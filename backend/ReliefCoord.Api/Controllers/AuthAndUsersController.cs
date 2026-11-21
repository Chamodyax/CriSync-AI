using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReliefCoord.Api.Security;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Services;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthAndUsersController : ControllerBase
{
    private static readonly object SettingsLock = new();
    private static SystemGovernanceConfigDto _governanceConfig = new(
        MinGlobalAdmins: 2,
        MaxGlobalAdmins: 3,
        MaxOrganizationAdmins: 3,
        RequireVolunteerCoordinatorVerification: true,
        RequirePartnerAdminApproval: true,
        HighValueDonationThresholdUsd: 50000m
    );
    private static ServiceIntegrationSettingsDto _serviceSettings = new(
        AiProviderName: "Azure OpenAI / Local Rule Fallback",
        AiModelName: "crisync-triage-v2",
        AiConfidenceThreshold: 0.72,
        SimulateExternalAiFailure: false,
        SmsProviderName: "Twilio Humanitarian Gateway",
        SmsSenderId: "CRISYNC",
        SmsIdempotencyWindowMinutes: 60,
        SimulateExternalNotificationFailure: false,
        EmailSmtpProvider: "SendGrid Relief Relay",
        EmailSenderAddress: "alerts@crisync.org",
        EmailAlertsEnabled: true,
        MappingProviderName: "OpenStreetMap / OSRM Routing",
        GeocodingProviderName: "Nominatim Disaster Geocode",
        SimulateExternalMappingFailure: false
    );

    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtTokenGenerator _jwtGenerator;
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public AuthAndUsersController(
        IUnitOfWork unitOfWork,
        JwtTokenGenerator jwtGenerator,
        IServiceResilienceMonitor resilienceMonitor)
    {
        _unitOfWork = unitOfWork;
        _jwtGenerator = jwtGenerator;
        _resilienceMonitor = resilienceMonitor;
    }

    private async Task<string> ResolveOrganisationNameAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (user.OrganisationId.HasValue)
        {
            var org = await _unitOfWork.Organisations.GetByIdAsync(user.OrganisationId.Value, cancellationToken);
            if (org != null)
            {
                return org.Name;
            }
        }
        return user.Role switch
        {
            UserRole.Citizen => "Citizen / Household Portal",
            UserRole.Donor => "Public Humanitarian Donor",
            UserRole.Volunteer => "Community Volunteer Roster",
            _ => "Independent Responder"
        };
    }

    private AdminUserSummaryDto BuildUserSummary(AppUser u, string organisationName) =>
        new(
            u.Id,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.Role.ToString(),
            u.PreferredLanguage,
            u.OrganisationId,
            organisationName,
            u.IsActive,
            u.CreatedAtUtc,
            u.AccountStatus,
            u.InvitedByUserName,
            u.InvitationCode);

    private async Task<(bool Allowed, string ErrorMessage)> ValidateAdminCountPolicyAsync(
        UserRole targetRole,
        bool targetIsActive,
        UserRole? previousRole,
        bool previousIsActive,
        CancellationToken cancellationToken)
    {
        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        int activeGlobalAdmins = allUsers.Count(u => u.Role == UserRole.SystemAdmin && u.IsActive);
        int activeOrgAdmins = allUsers.Count(u => u.Role == UserRole.OrganizationAdmin && u.IsActive);

        bool wasActiveGlobal = previousRole == UserRole.SystemAdmin && previousIsActive;
        bool willBeActiveGlobal = targetRole == UserRole.SystemAdmin && targetIsActive;
        int projectedGlobal = activeGlobalAdmins + (willBeActiveGlobal ? 1 : 0) - (wasActiveGlobal ? 1 : 0);

        bool wasActiveOrg = previousRole == UserRole.OrganizationAdmin && previousIsActive;
        bool willBeActiveOrg = targetRole == UserRole.OrganizationAdmin && targetIsActive;
        int projectedOrg = activeOrgAdmins + (willBeActiveOrg ? 1 : 0) - (wasActiveOrg ? 1 : 0);

        SystemGovernanceConfigDto cfg;
        lock (SettingsLock)
        {
            cfg = _governanceConfig;
        }

        if (willBeActiveGlobal && projectedGlobal > activeGlobalAdmins && projectedGlobal > cfg.MaxGlobalAdmins)
        {
            return (false, $"Small Admin Count Policy: Maximum limit of {cfg.MaxGlobalAdmins} active Global System Admins reached. Too many admins increases security risk.");
        }

        if (wasActiveGlobal && !willBeActiveGlobal && projectedGlobal < cfg.MinGlobalAdmins)
        {
            return (false, $"Small Admin Count Policy: At least {cfg.MinGlobalAdmins} active Global System Admins must remain active so system control is never lost if one admin is unavailable.");
        }

        if (willBeActiveOrg && projectedOrg > activeOrgAdmins && projectedOrg > cfg.MaxOrganizationAdmins)
        {
            return (false, $"Small Admin Count Policy: Maximum limit of {cfg.MaxOrganizationAdmins} active Organization Admins reached.");
        }

        return (true, string.Empty);
    }

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var users = await _unitOfWork.Users.FindAsync(
            u => u.Email.ToLower() == dto.Email.Trim().ToLower(),
            cancellationToken);

        var user = users.FirstOrDefault();
        if (user == null || !HumanitarianOperationsService.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Invalid email or password credentials." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "This account has been deactivated by an administrator." });
        }

        string orgName = await ResolveOrganisationNameAsync(user, cancellationToken);
        string token = _jwtGenerator.GenerateToken(user);
        return Ok(new AuthResponseDto(
            token,
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.PreferredLanguage,
            user.OrganisationId,
            orgName,
            user.AccountStatus,
            user.PhoneNumber,
            user.ProfilePhotoUrl
        ));
    }

    [HttpPost("auth/register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterUserRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto.Role == UserRole.DonorOfficer)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Donor / Funding Officer handles financial transactions and can only be invited and appointed exclusively by the Global System Admin."
            });
        }

        if (dto.Role is UserRole.CaseManager or UserRole.ShelterAdmin or UserRole.LogisticsManager or UserRole.VolunteerCoordinator or UserRole.EmergencyCoordinator)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = $"Role '{dto.Role}' is an invitation-based operational role. A System Admin or Emergency Coordinator must invite you."
            });
        }

        if (dto.Role is UserRole.SystemAdmin or UserRole.OrganizationAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Administrator accounts cannot be publicly self-registered."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "FullName, Email, and Password are required." });
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.FindAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (existing.Any())
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        Guid? assignedOrgId = dto.Role == UserRole.PartnerOfficer ? dto.OrganisationId : null;

        if (dto.Role == UserRole.PartnerOfficer && !string.IsNullOrWhiteSpace(dto.OrganisationName))
        {
            var newOrg = new Organisation
            {
                Name = dto.OrganisationName.Trim(),
                OrganisationType = string.IsNullOrWhiteSpace(dto.OrganisationType) ? "Partner NGO" : dto.OrganisationType.Trim(),
                Country = string.IsNullOrWhiteSpace(dto.Country) ? "International" : dto.Country.Trim(),
                ContactEmail = normalizedEmail,
                ContactPhone = dto.PhoneNumber?.Trim() ?? string.Empty,
                IsVerified = false,
                PartnershipStatus = "Pending",
                PartnershipProposal = string.IsNullOrWhiteSpace(dto.PartnershipProposal)
                    ? "Incoming partner organization registration & collaboration request."
                    : dto.PartnershipProposal.Trim()
            };
            await _unitOfWork.Organisations.AddAsync(newOrg, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            assignedOrgId = newOrg.Id;
        }

        string accountStatus = dto.Role switch
        {
            UserRole.Volunteer => "Pending",
            UserRole.PartnerOfficer => "PendingPartnershipApproval",
            _ => "Active"
        };

        var (hash, salt) = HumanitarianOperationsService.HashPassword(dto.Password);
        var user = new AppUser
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = dto.Role,
            PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? "en" : dto.PreferredLanguage,
            OrganisationId = assignedOrgId,
            IsActive = true,
            AccountStatus = accountStatus
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);

        if (dto.Role == UserRole.Volunteer)
        {
            var volunteerProfile = new VolunteerProfile
            {
                UserId = user.Id,
                FullName = user.FullName,
                SkillsCsv = string.IsNullOrWhiteSpace(dto.SkillsCsv) ? "General Emergency Relief" : dto.SkillsCsv.Trim(),
                LanguagesCsv = string.IsNullOrWhiteSpace(dto.LanguagesCsv) ? user.PreferredLanguage : dto.LanguagesCsv.Trim(),
                AvailabilitySchedule = string.IsNullOrWhiteSpace(dto.AvailabilitySchedule) ? "Available On-Call" : dto.AvailabilitySchedule.Trim(),
                IsVerified = false,
                IsAvailable = true,
                CurrentLatitude = 6.9271,
                CurrentLongitude = 79.8612,
                CompletedTasksCount = 0
            };
            await _unitOfWork.Volunteers.AddAsync(volunteerProfile, cancellationToken);
        }

        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Registration",
            ActionName = $"SelfRegistered_{dto.Role}_{accountStatus}",
            EntityType = nameof(AppUser),
            EntityId = user.Id.ToString(),
            ActorUserId = user.Id,
            ActorUserName = user.FullName,
            ActorRole = user.Role.ToString(),
            DetailsJson = JsonSerializer.Serialize(new { user.Email, user.AccountStatus, user.OrganisationId })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string organisationName = await ResolveOrganisationNameAsync(user, cancellationToken);
        string token = _jwtGenerator.GenerateToken(user);
        return Ok(new AuthResponseDto(
            token,
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.PreferredLanguage,
            user.OrganisationId,
            organisationName,
            user.AccountStatus,
            user.PhoneNumber,
            user.ProfilePhotoUrl
        ));
    }

    [HttpGet("auth/demo-accounts")]
    [AllowAnonymous]
    public ActionResult GetDemoAccounts()
    {
        return Ok(Array.Empty<object>());
    }

    [HttpGet("organisations")]
    [AllowAnonymous]
    public async Task<ActionResult> GetOrganisations(CancellationToken cancellationToken)
    {
        var orgs = await _unitOfWork.Organisations.GetAllAsync(cancellationToken);
        return Ok(orgs);
    }

    [HttpGet("users/me")]
    [Authorize]
    public async Task<ActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        string organisationName = await ResolveOrganisationNameAsync(user, cancellationToken);
        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            Role = user.Role.ToString(),
            user.PreferredLanguage,
            user.OrganisationId,
            OrganisationName = organisationName,
            user.AccountStatus,
            user.ProfilePhotoUrl
        });
    }

    [HttpPut("users/me")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> UpdateMyProfile(
        [FromBody] UpdateMyProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Full name is required." });
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        bool passwordChanged = false;
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            {
                return BadRequest(new { message = "Please enter your current password before setting a new password." });
            }

            if (!HumanitarianOperationsService.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

            if (dto.NewPassword.Trim().Length < 6)
            {
                return BadRequest(new { message = "New password must be at least 6 characters long." });
            }

            var (newHash, newSalt) = HumanitarianOperationsService.HashPassword(dto.NewPassword.Trim());
            user.PasswordHash = newHash;
            user.PasswordSalt = newSalt;
            passwordChanged = true;
        }

        if (dto.ProfilePhotoUrl != null)
        {
            var trimmedPhoto = dto.ProfilePhotoUrl.Trim();
            if (trimmedPhoto.Length > 350000)
            {
                return BadRequest(new { message = "Profile photo size is too large. Please choose a smaller image." });
            }
            user.ProfilePhotoUrl = trimmedPhoto;
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(dto.PreferredLanguage))
        {
            user.PreferredLanguage = dto.PreferredLanguage.Trim();
        }
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        var volunteers = await _unitOfWork.Volunteers.FindAsync(v => v.UserId == userId, cancellationToken);
        foreach (var v in volunteers)
        {
            v.FullName = user.FullName;
            v.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.Volunteers.UpdateAsync(v, cancellationToken);
        }

        var households = await _unitOfWork.Households.FindAsync(h => h.UserId == userId, cancellationToken);
        foreach (var h in households)
        {
            h.HouseholdHeadName = user.FullName;
            h.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.Households.UpdateAsync(h, cancellationToken);
        }

        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "ProfileManagement",
            ActionName = "UserProfileUpdated",
            EntityType = nameof(AppUser),
            EntityId = user.Id.ToString(),
            ActorUserId = user.Id,
            ActorUserName = user.FullName,
            ActorRole = user.Role.ToString(),
            DetailsJson = JsonSerializer.Serialize(new
            {
                user.FullName,
                user.PhoneNumber,
                user.PreferredLanguage,
                PasswordChanged = passwordChanged,
                HasProfilePhoto = !string.IsNullOrEmpty(user.ProfilePhotoUrl)
            })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string organisationName = await ResolveOrganisationNameAsync(user, cancellationToken);
        string token = _jwtGenerator.GenerateToken(user);
        return Ok(new AuthResponseDto(
            token,
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.PreferredLanguage,
            user.OrganisationId,
            organisationName,
            user.AccountStatus,
            user.PhoneNumber,
            user.ProfilePhotoUrl
        ));
    }

    [HttpGet("users/households/me")]
    [Authorize]
    public async Task<ActionResult> GetMyHousehold(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var profiles = await _unitOfWork.Households.FindAsync(h => h.UserId == userId, cancellationToken);
        return Ok(profiles.FirstOrDefault());
    }

    [HttpPost("users/households")]
    [Authorize]
    public async Task<ActionResult> UpsertHousehold([FromBody] UpsertHouseholdDto dto, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var existingList = await _unitOfWork.Households.FindAsync(h => h.UserId == userId, cancellationToken);
        var profile = existingList.FirstOrDefault();

        if (profile == null)
        {
            profile = new HouseholdProfile
            {
                UserId = userId,
                HouseholdHeadName = dto.HouseholdHeadName,
                TotalMembers = Math.Max(1, dto.TotalMembers),
                ChildrenCount = dto.ChildrenCount,
                ElderlyCount = dto.ElderlyCount,
                HasMobilityNeeds = dto.HasMobilityNeeds,
                HasChronicMedicalNeeds = dto.HasChronicMedicalNeeds,
                MedicalNotes = dto.MedicalNotes,
                PrimaryAddress = dto.PrimaryAddress,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };
            await _unitOfWork.Households.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.HouseholdHeadName = dto.HouseholdHeadName;
            profile.TotalMembers = Math.Max(1, dto.TotalMembers);
            profile.ChildrenCount = dto.ChildrenCount;
            profile.ElderlyCount = dto.ElderlyCount;
            profile.HasMobilityNeeds = dto.HasMobilityNeeds;
            profile.HasChronicMedicalNeeds = dto.HasChronicMedicalNeeds;
            profile.MedicalNotes = dto.MedicalNotes;
            profile.PrimaryAddress = dto.PrimaryAddress;
            profile.Latitude = dto.Latitude;
            profile.Longitude = dto.Longitude;
            await _unitOfWork.Households.UpdateAsync(profile, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(profile);
    }

    [HttpPost("users/register-citizen-behalf")]
    [Authorize(Roles = "CaseManager,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<AdminUserSummaryDto>> RegisterCitizenOnBehalf(
        [FromBody] RegisterCitizenOnBehalfDto dto,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Citizen full name, email/identifier, and password are required." });
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (existing.Any())
        {
            return Conflict(new { message = "A citizen account with this email already exists." });
        }

        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Case Manager";
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        var (hash, salt) = HumanitarianOperationsService.HashPassword(dto.Password);

        var citizenUser = new AppUser
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Citizen,
            PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? "si" : dto.PreferredLanguage.Trim(),
            IsActive = true,
            AccountStatus = "Active",
            InvitedByUserName = $"Registered on behalf by {actorName}"
        };

        await _unitOfWork.Users.AddAsync(citizenUser, cancellationToken);

        var household = new HouseholdProfile
        {
            UserId = citizenUser.Id,
            HouseholdHeadName = citizenUser.FullName,
            TotalMembers = Math.Max(1, dto.TotalMembers),
            ChildrenCount = Math.Max(0, dto.ChildrenCount),
            ElderlyCount = Math.Max(0, dto.ElderlyCount),
            HasMobilityNeeds = dto.HasMobilityNeeds,
            HasChronicMedicalNeeds = dto.HasChronicMedicalNeeds,
            MedicalNotes = dto.MedicalNotes?.Trim() ?? string.Empty,
            PrimaryAddress = dto.PrimaryAddress?.Trim() ?? string.Empty,
            Latitude = 6.9271,
            Longitude = 79.8612
        };

        await _unitOfWork.Households.AddAsync(household, cancellationToken);
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "CitizenOnboarding",
            ActionName = "CaseManager_RegisteredCitizenOnBehalf",
            EntityType = nameof(AppUser),
            EntityId = citizenUser.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = actorName,
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "CaseManager",
            DetailsJson = JsonSerializer.Serialize(new { citizenUser.FullName, citizenUser.Email, household.TotalMembers })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(BuildUserSummary(citizenUser, "Citizen / Household Portal"));
    }

    [HttpPost("invitations/invite-staff")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin,EmergencyCoordinator")]
    public async Task<ActionResult<AdminUserSummaryDto>> InviteOperationalRole(
        [FromBody] InviteStaffRequestDto dto,
        CancellationToken cancellationToken)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? callerRole;
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;

        if (dto.Role == UserRole.DonorOfficer && callerRole != nameof(UserRole.SystemAdmin))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only the Global System Admin can invite or appoint a Donor / Funding Officer because this role handles financial transactions."
            });
        }

        if ((dto.Role == UserRole.SystemAdmin || dto.Role == UserRole.OrganizationAdmin) && callerRole != nameof(UserRole.SystemAdmin))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only a Global System Admin can appoint System Admin or Organization Admin accounts."
            });
        }

        if (dto.Role is not (UserRole.CaseManager or UserRole.ShelterAdmin or UserRole.LogisticsManager or UserRole.VolunteerCoordinator or UserRole.DonorOfficer or UserRole.EmergencyCoordinator or UserRole.OrganizationAdmin or UserRole.SystemAdmin))
        {
            return BadRequest(new { message = $"Role '{dto.Role}' uses self-registration or citizen-on-behalf registration rather than staff invitation." });
        }

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.TemporaryPassword))
        {
            return BadRequest(new { message = "Full name, email, and temporary password are required." });
        }

        if (dto.TemporaryPassword.Trim().Length < 6)
        {
            return BadRequest(new { message = "Temporary password must be at least 6 characters." });
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.FindAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (existing.Any())
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var (allowed, policyError) = await ValidateAdminCountPolicyAsync(dto.Role, true, null, false, cancellationToken);
        if (!allowed)
        {
            return BadRequest(new { message = policyError });
        }

        string inviteCode = $"INV-{dto.Role.ToString().ToUpperInvariant()[..Math.Min(4, dto.Role.ToString().Length)]}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var (hash, salt) = HumanitarianOperationsService.HashPassword(dto.TemporaryPassword);

        var staffUser = new AppUser
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = dto.Role,
            PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? "en" : dto.PreferredLanguage.Trim(),
            OrganisationId = dto.OrganisationId,
            IsActive = true,
            AccountStatus = "Active",
            InvitedByUserName = actorName,
            InvitationCode = inviteCode
        };

        await _unitOfWork.Users.AddAsync(staffUser, cancellationToken);
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "StaffInvitation",
            ActionName = $"Invited_{dto.Role}",
            EntityType = nameof(AppUser),
            EntityId = staffUser.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = actorName,
            ActorRole = callerRole,
            DetailsJson = JsonSerializer.Serialize(new { staffUser.FullName, staffUser.Email, Role = staffUser.Role.ToString(), inviteCode })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        string orgName = await ResolveOrganisationNameAsync(staffUser, cancellationToken);
        return Ok(BuildUserSummary(staffUser, orgName));
    }

    [HttpGet("admin/users")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<IEnumerable<AdminUserSummaryDto>>> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        var orgs = await _unitOfWork.Organisations.GetAllAsync(cancellationToken);
        var orgMap = orgs.ToDictionary(o => o.Id, o => o.Name);

        var result = users
            .OrderByDescending(u => u.Role == UserRole.SystemAdmin)
            .ThenByDescending(u => u.Role == UserRole.OrganizationAdmin)
            .ThenBy(u => u.FullName)
            .Select(u => BuildUserSummary(
                u,
                u.OrganisationId.HasValue && orgMap.TryGetValue(u.OrganisationId.Value, out var orgName)
                    ? orgName
                    : u.Role switch
                    {
                        UserRole.Citizen => "Citizen / Household Portal",
                        UserRole.Donor => "Public Humanitarian Donor",
                        UserRole.Volunteer => "Community Volunteer Roster",
                        _ => "Independent Responder"
                    }))
            .ToList();

        return Ok(result);
    }

    [HttpPost("admin/users")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<AdminUserSummaryDto>> CreateUserByAdmin([FromBody] AdminCreateUserDto dto, CancellationToken cancellationToken)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (dto.Role == UserRole.DonorOfficer && callerRole != nameof(UserRole.SystemAdmin))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only the Global System Admin can appoint a Donor / Funding Officer."
            });
        }

        if ((dto.Role == UserRole.SystemAdmin || dto.Role == UserRole.OrganizationAdmin) && callerRole != nameof(UserRole.SystemAdmin))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only a Global System Admin can appoint System Admin or Organization Admin accounts."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { message = "Full name, email, and password are required." });
        }

        if (dto.Password.Trim().Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters long." });
        }

        var (allowed, policyError) = await ValidateAdminCountPolicyAsync(dto.Role, true, null, false, cancellationToken);
        if (!allowed)
        {
            return BadRequest(new { message = policyError });
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var existing = await _unitOfWork.Users.FindAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (existing.Any())
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? callerRole;
        var (hash, salt) = HumanitarianOperationsService.HashPassword(dto.Password);
        var user = new AppUser
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = dto.Role,
            PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? "en" : dto.PreferredLanguage.Trim(),
            OrganisationId = dto.Role is UserRole.Citizen or UserRole.Donor or UserRole.Volunteer ? null : dto.OrganisationId,
            IsActive = true,
            AccountStatus = "Active",
            InvitedByUserName = actorName
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "UserManagement",
            ActionName = "Admin_CreatedUser",
            EntityType = nameof(AppUser),
            EntityId = user.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = actorName,
            ActorRole = callerRole,
            DetailsJson = JsonSerializer.Serialize(new
            {
                user.FullName,
                user.Email,
                Role = user.Role.ToString()
            })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string organisationName = await ResolveOrganisationNameAsync(user, cancellationToken);
        return Ok(BuildUserSummary(user, organisationName));
    }

    [HttpPut("admin/users/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<AdminUserSummaryDto>> UpdateUser(Guid id, [FromBody] AdminUpdateUserDto dto, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (callerRole != nameof(UserRole.SystemAdmin) &&
            (user.Role == UserRole.SystemAdmin || dto.Role == UserRole.SystemAdmin || dto.Role == UserRole.DonorOfficer))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only a Global System Admin can modify Global System Admin or Donor / Funding Officer accounts."
            });
        }

        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Full name and email are required." });
        }

        string normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        var conflicts = await _unitOfWork.Users.FindAsync(
            u => u.Email.ToLower() == normalizedEmail && u.Id != id,
            cancellationToken);

        if (conflicts.Any())
        {
            return Conflict(new { message = "Another account already uses this email address." });
        }

        bool isDefaultAdmin = user.Email.Equals("admin@crisync.org", StringComparison.OrdinalIgnoreCase);
        var nextRole = isDefaultAdmin ? UserRole.SystemAdmin : dto.Role;
        var nextActive = isDefaultAdmin || dto.IsActive;

        var (allowed, policyError) = await ValidateAdminCountPolicyAsync(nextRole, nextActive, user.Role, user.IsActive, cancellationToken);
        if (!allowed)
        {
            return BadRequest(new { message = policyError });
        }

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
        user.OrganisationId = nextRole is UserRole.Citizen or UserRole.Donor or UserRole.Volunteer ? null : dto.OrganisationId;

        if (!isDefaultAdmin)
        {
            user.Email = normalizedEmail;
            user.Role = nextRole;
            user.IsActive = nextActive;
            if (user.IsActive && user.AccountStatus == "Suspended")
            {
                user.AccountStatus = "Active";
            }
            else if (!user.IsActive)
            {
                user.AccountStatus = "Suspended";
            }
        }
        else
        {
            user.Role = UserRole.SystemAdmin;
            user.IsActive = true;
            user.AccountStatus = "Active";
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var (hash, salt) = HumanitarianOperationsService.HashPassword(dto.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        await _unitOfWork.Users.UpdateAsync(user, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "UserManagement",
            ActionName = "Admin_UpdatedUser",
            EntityType = nameof(AppUser),
            EntityId = user.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = callerRole,
            DetailsJson = JsonSerializer.Serialize(new
            {
                user.FullName,
                user.Email,
                Role = user.Role.ToString(),
                user.IsActive,
                PasswordReset = !string.IsNullOrWhiteSpace(dto.NewPassword)
            })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string organisationName = await ResolveOrganisationNameAsync(user, cancellationToken);
        return Ok(BuildUserSummary(user, organisationName));
    }

    [HttpDelete("admin/users/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (callerRole != nameof(UserRole.SystemAdmin) && (user.Role == UserRole.SystemAdmin || user.Role == UserRole.DonorOfficer))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only a Global System Admin can remove System Admin or Donor Officer accounts."
            });
        }

        var currentActorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorGuid) ? actorGuid : Guid.Empty;

        if (user.Email.Equals("admin@crisync.org", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "The primary default System Administrator account cannot be deleted." });
        }

        if (user.Id == currentActorId)
        {
            return BadRequest(new { message = "You cannot delete the currently signed-in administrator account." });
        }

        var (allowed, policyError) = await ValidateAdminCountPolicyAsync(user.Role, false, user.Role, user.IsActive, cancellationToken);
        if (!allowed)
        {
            return BadRequest(new { message = policyError });
        }

        var households = await _unitOfWork.Households.FindAsync(h => h.UserId == id, cancellationToken);
        foreach (var h in households)
        {
            await _unitOfWork.Households.DeleteAsync(h, cancellationToken);
        }

        var volunteers = await _unitOfWork.Volunteers.FindAsync(v => v.UserId == id, cancellationToken);
        foreach (var v in volunteers)
        {
            var assignments = await _unitOfWork.VolunteerAssignments.FindAsync(a => a.VolunteerProfileId == v.Id, cancellationToken);
            foreach (var a in assignments)
            {
                await _unitOfWork.VolunteerAssignments.DeleteAsync(a, cancellationToken);
            }
            await _unitOfWork.Volunteers.DeleteAsync(v, cancellationToken);
        }

        var shelterAllocs = await _unitOfWork.ShelterAllocations.FindAsync(s => s.CitizenUserId == id, cancellationToken);
        foreach (var s in shelterAllocs)
        {
            await _unitOfWork.ShelterAllocations.DeleteAsync(s, cancellationToken);
        }

        var userRequests = await _unitOfWork.AssistanceRequests.FindAsync(r => r.CitizenUserId == id, cancellationToken);
        foreach (var req in userRequests)
        {
            var recs = await _unitOfWork.AiRecommendations.FindAsync(a => a.AssistanceRequestId == req.Id, cancellationToken);
            foreach (var rec in recs)
            {
                var decisions = await _unitOfWork.GovernanceDecisions.FindAsync(d => d.AiRecommendationId == rec.Id, cancellationToken);
                foreach (var dec in decisions)
                {
                    await _unitOfWork.GovernanceDecisions.DeleteAsync(dec, cancellationToken);
                }
                await _unitOfWork.AiRecommendations.DeleteAsync(rec, cancellationToken);
            }

            var histories = await _unitOfWork.RequestHistories.FindAsync(h => h.AssistanceRequestId == req.Id, cancellationToken);
            foreach (var history in histories)
            {
                await _unitOfWork.RequestHistories.DeleteAsync(history, cancellationToken);
            }

            var reqFeedbacks = await _unitOfWork.RequestFeedbacks.FindAsync(f => f.AssistanceRequestId == req.Id, cancellationToken);
            foreach (var fb in reqFeedbacks)
            {
                await _unitOfWork.RequestFeedbacks.DeleteAsync(fb, cancellationToken);
            }

            var resAllocs = await _unitOfWork.ResourceAllocations.FindAsync(a => a.AssistanceRequestId == req.Id, cancellationToken);
            var allocIds = resAllocs.Select(a => a.Id).ToHashSet();
            if (allocIds.Count > 0)
            {
                var dispatches = await _unitOfWork.DispatchRecords.FindAsync(d => allocIds.Contains(d.ResourceAllocationId), cancellationToken);
                foreach (var disp in dispatches)
                {
                    await _unitOfWork.DispatchRecords.DeleteAsync(disp, cancellationToken);
                }
            }
            foreach (var alloc in resAllocs)
            {
                await _unitOfWork.ResourceAllocations.DeleteAsync(alloc, cancellationToken);
            }

            var linkedShelterAllocs = await _unitOfWork.ShelterAllocations.FindAsync(s => s.AssistanceRequestId == req.Id, cancellationToken);
            foreach (var sAlloc in linkedShelterAllocs)
            {
                sAlloc.AssistanceRequestId = null;
                await _unitOfWork.ShelterAllocations.UpdateAsync(sAlloc, cancellationToken);
            }

            var linkedVolAssigns = await _unitOfWork.VolunteerAssignments.FindAsync(v => v.AssistanceRequestId == req.Id, cancellationToken);
            foreach (var vAssign in linkedVolAssigns)
            {
                vAssign.AssistanceRequestId = null;
                await _unitOfWork.VolunteerAssignments.UpdateAsync(vAssign, cancellationToken);
            }

            await _unitOfWork.AssistanceRequests.DeleteAsync(req, cancellationToken);
        }

        var orphanFeedbacks = await _unitOfWork.RequestFeedbacks.FindAsync(f => f.CitizenUserId == id, cancellationToken);
        foreach (var fb in orphanFeedbacks)
        {
            await _unitOfWork.RequestFeedbacks.DeleteAsync(fb, cancellationToken);
        }

        await _unitOfWork.Users.DeleteAsync(user, cancellationToken);

        var actorId = currentActorId == Guid.Empty ? (Guid?)null : currentActorId;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "UserManagement",
            ActionName = "Admin_DeletedUser",
            EntityType = nameof(AppUser),
            EntityId = user.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { user.FullName, user.Email, Role = user.Role.ToString() })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedUserId = id, message = $"Removed user {user.FullName} ({user.Email})." });
    }

    [HttpGet("admin/partnerships")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> GetPartnerships(CancellationToken cancellationToken)
    {
        var orgs = await _unitOfWork.Organisations.GetAllAsync(cancellationToken);
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        var partnerUsersByOrg = users
            .Where(u => u.OrganisationId.HasValue && u.Role == UserRole.PartnerOfficer)
            .GroupBy(u => u.OrganisationId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(u => new { u.Id, u.FullName, u.Email, u.AccountStatus, u.IsActive }).ToList());

        var result = orgs
            .OrderByDescending(o => o.PartnershipStatus == "Pending")
            .ThenBy(o => o.Name)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.OrganisationType,
                o.Country,
                o.ContactEmail,
                o.ContactPhone,
                o.IsVerified,
                o.PartnershipStatus,
                o.PartnershipProposal,
                o.CreatedAtUtc,
                Officers = partnerUsersByOrg.TryGetValue(o.Id, out var list) ? list : new()
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost("admin/partnerships/{organisationId:guid}/approve")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> ApprovePartnership(Guid organisationId, CancellationToken cancellationToken)
    {
        var org = await _unitOfWork.Organisations.GetByIdAsync(organisationId, cancellationToken);
        if (org == null)
        {
            return NotFound(new { message = "Partner organisation not found." });
        }

        org.IsVerified = true;
        org.PartnershipStatus = "Approved";
        await _unitOfWork.Organisations.UpdateAsync(org, cancellationToken);

        var officers = await _unitOfWork.Users.FindAsync(
            u => u.OrganisationId == organisationId && u.Role == UserRole.PartnerOfficer,
            cancellationToken);
        foreach (var officer in officers)
        {
            officer.AccountStatus = "Active";
            officer.IsActive = true;
            await _unitOfWork.Users.UpdateAsync(officer, cancellationToken);
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "PartnershipApproval",
            ActionName = "Admin_ApprovedPartnership",
            EntityType = nameof(Organisation),
            EntityId = org.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { org.Name, OfficersActivated = officers.Count })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(org);
    }

    [HttpPost("admin/partnerships/{organisationId:guid}/reject")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> RejectPartnership(Guid organisationId, CancellationToken cancellationToken)
    {
        var org = await _unitOfWork.Organisations.GetByIdAsync(organisationId, cancellationToken);
        if (org == null)
        {
            return NotFound(new { message = "Partner organisation not found." });
        }

        org.IsVerified = false;
        org.PartnershipStatus = "Rejected";
        await _unitOfWork.Organisations.UpdateAsync(org, cancellationToken);

        var officers = await _unitOfWork.Users.FindAsync(
            u => u.OrganisationId == organisationId && u.Role == UserRole.PartnerOfficer,
            cancellationToken);
        foreach (var officer in officers)
        {
            officer.AccountStatus = "PartnershipRejected";
            await _unitOfWork.Users.UpdateAsync(officer, cancellationToken);
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "PartnershipApproval",
            ActionName = "Admin_RejectedPartnership",
            EntityType = nameof(Organisation),
            EntityId = org.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { org.Name })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(org);
    }

    [HttpGet("admin/role-permissions")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> GetRolePermissions(CancellationToken cancellationToken)
    {
        var permissions = await _unitOfWork.RolePermissions.GetAllAsync(cancellationToken);
        return Ok(permissions.OrderBy(p => p.Role.ToString()).ThenBy(p => p.PermissionCode));
    }

    [HttpPost("admin/role-permissions")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> AddRolePermission([FromBody] RolePermissionDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.PermissionCode))
        {
            return BadRequest(new { message = "PermissionCode is required." });
        }

        string code = dto.PermissionCode.Trim().ToUpperInvariant();
        var existing = await _unitOfWork.RolePermissions.FindAsync(
            p => p.Role == dto.Role && p.PermissionCode == code,
            cancellationToken);

        if (existing.Any())
        {
            return Conflict(new { message = $"Permission '{code}' is already assigned to role '{dto.Role}'." });
        }

        var perm = new RolePermission
        {
            Role = dto.Role,
            PermissionCode = code,
            Description = dto.Description?.Trim() ?? string.Empty
        };

        await _unitOfWork.RolePermissions.AddAsync(perm, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "RolePermissions",
            ActionName = "Admin_AddedRolePermission",
            EntityType = nameof(RolePermission),
            EntityId = perm.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { Role = dto.Role.ToString(), code })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(perm);
    }

    [HttpDelete("admin/role-permissions/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteRolePermission(Guid id, CancellationToken cancellationToken)
    {
        var perm = await _unitOfWork.RolePermissions.GetByIdAsync(id, cancellationToken);
        if (perm == null)
        {
            return NotFound();
        }

        await _unitOfWork.RolePermissions.DeleteAsync(perm, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "RolePermissions",
            ActionName = "Admin_RemovedRolePermission",
            EntityType = nameof(RolePermission),
            EntityId = perm.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { Role = perm.Role.ToString(), perm.PermissionCode })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedPermissionId = id });
    }

    [HttpGet("admin/system-config")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public ActionResult<SystemGovernanceConfigDto> GetSystemConfig()
    {
        lock (SettingsLock)
        {
            return Ok(_governanceConfig);
        }
    }

    [HttpPut("admin/system-config")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<SystemGovernanceConfigDto>> UpdateSystemConfig([FromBody] SystemGovernanceConfigDto dto, CancellationToken cancellationToken)
    {
        int clampedMin = Math.Clamp(dto.MinGlobalAdmins, 2, 3);
        int clampedMax = Math.Clamp(dto.MaxGlobalAdmins, clampedMin, 4);
        int clampedOrgMax = Math.Clamp(dto.MaxOrganizationAdmins, 1, 5);

        SystemGovernanceConfigDto updated = new(
            clampedMin,
            clampedMax,
            clampedOrgMax,
            dto.RequireVolunteerCoordinatorVerification,
            dto.RequirePartnerAdminApproval,
            Math.Max(1000m, dto.HighValueDonationThresholdUsd)
        );

        lock (SettingsLock)
        {
            _governanceConfig = updated;
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "SystemConfiguration",
            ActionName = "Admin_UpdatedSystemConfig",
            EntityType = "SystemGovernanceConfig",
            EntityId = "GLOBAL",
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(updated)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(updated);
    }

    [HttpGet("admin/service-settings")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public ActionResult<ServiceIntegrationSettingsDto> GetServiceSettings()
    {
        lock (SettingsLock)
        {
            _serviceSettings = _serviceSettings with
            {
                SimulateExternalAiFailure = _resilienceMonitor.SimulateExternalAiFailure,
                SimulateExternalNotificationFailure = _resilienceMonitor.SimulateExternalNotificationFailure,
                SimulateExternalMappingFailure = _resilienceMonitor.SimulateExternalMappingFailure
            };
            return Ok(_serviceSettings);
        }
    }

    [HttpPut("admin/service-settings")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ServiceIntegrationSettingsDto>> UpdateServiceSettings([FromBody] ServiceIntegrationSettingsDto dto, CancellationToken cancellationToken)
    {
        _resilienceMonitor.SimulateExternalAiFailure = dto.SimulateExternalAiFailure;
        _resilienceMonitor.SimulateExternalNotificationFailure = dto.SimulateExternalNotificationFailure;
        _resilienceMonitor.SimulateExternalMappingFailure = dto.SimulateExternalMappingFailure;

        var sanitized = dto with
        {
            AiConfidenceThreshold = Math.Clamp(dto.AiConfidenceThreshold, 0.1, 0.99),
            SmsIdempotencyWindowMinutes = Math.Clamp(dto.SmsIdempotencyWindowMinutes, 1, 1440)
        };

        lock (SettingsLock)
        {
            _serviceSettings = sanitized;
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "IntegrationSettings",
            ActionName = "Admin_UpdatedServiceIntegrationSettings",
            EntityType = "ExternalServiceSettings",
            EntityId = "AI_SMS_EMAIL_MAPS",
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(sanitized)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(sanitized);
    }
}

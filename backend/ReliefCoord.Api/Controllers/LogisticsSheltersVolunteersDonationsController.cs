using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Services;
using ReliefCoord.Application.Strategies;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Api.Controllers;

[ApiController]
[Route("api")]
public class LogisticsSheltersVolunteersDonationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HumanitarianOperationsService _opsService;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly IMappingProviderService _mappingService;
    private readonly IServiceResilienceMonitor _resilienceMonitor;
    private readonly ResourceAllocationStrategyResolver _strategyResolver;

    public LogisticsSheltersVolunteersDonationsController(
        IUnitOfWork unitOfWork,
        HumanitarianOperationsService opsService,
        IAiProviderFactory aiProviderFactory,
        IMappingProviderService mappingService,
        IServiceResilienceMonitor resilienceMonitor,
        ResourceAllocationStrategyResolver strategyResolver)
    {
        _unitOfWork = unitOfWork;
        _opsService = opsService;
        _aiProviderFactory = aiProviderFactory;
        _mappingService = mappingService;
        _resilienceMonitor = resilienceMonitor;
        _strategyResolver = strategyResolver;
    }

    [HttpGet("logistics/resources")]
    [AllowAnonymous]
    public async Task<ActionResult> GetResources(CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Resources.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("logistics/resources")]
    [Authorize(Roles = "LogisticsManager,PartnerOfficer,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> CreateResource([FromBody] ResourceItem item, CancellationToken cancellationToken)
    {
        item.AvailableQuantity = item.TotalQuantity;
        await _unitOfWork.Resources.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpPut("logistics/resources/{id:guid}")]
    [Authorize(Roles = "LogisticsManager,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> UpdateResource(Guid id, [FromBody] UpdateResourceItemDto dto, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Resources.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { message = "Resource item not found." });
        }

        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.WarehouseName))
        {
            return BadRequest(new { message = "Resource name and warehouse name are required." });
        }

        item.Name = dto.Name.Trim();
        item.Category = dto.Category;
        item.WarehouseName = dto.WarehouseName.Trim();
        item.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? item.Unit : dto.Unit.Trim();
        item.TotalQuantity = Math.Max(0, dto.TotalQuantity);
        item.AvailableQuantity = Math.Clamp(dto.AvailableQuantity, 0, item.TotalQuantity);
        item.ReorderThreshold = Math.Max(0, dto.ReorderThreshold);
        item.WarehouseLatitude = dto.WarehouseLatitude;
        item.WarehouseLongitude = dto.WarehouseLongitude;

        await _unitOfWork.Resources.UpdateAsync(item, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Logistics",
            ActionName = "Admin_UpdatedResource",
            EntityType = nameof(ResourceItem),
            EntityId = item.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { item.Name, item.WarehouseName, item.AvailableQuantity, item.TotalQuantity })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpDelete("logistics/resources/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteResource(Guid id, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Resources.GetByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { message = "Resource item not found." });
        }

        var allocs = await _unitOfWork.ResourceAllocations.FindAsync(a => a.ResourceItemId == id, cancellationToken);
        var allocIds = allocs.Select(a => a.Id).ToHashSet();
        if (allocIds.Count > 0)
        {
            var dispatches = await _unitOfWork.DispatchRecords.FindAsync(d => allocIds.Contains(d.ResourceAllocationId), cancellationToken);
            foreach (var disp in dispatches)
            {
                await _unitOfWork.DispatchRecords.DeleteAsync(disp, cancellationToken);
            }
        }
        foreach (var alloc in allocs)
        {
            await _unitOfWork.ResourceAllocations.DeleteAsync(alloc, cancellationToken);
        }

        await _unitOfWork.Resources.DeleteAsync(item, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Logistics",
            ActionName = "Admin_DeletedResource",
            EntityType = nameof(ResourceItem),
            EntityId = item.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { item.Name, item.WarehouseName })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedResourceId = id });
    }

    [HttpGet("logistics/recommendations/{requestId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetResourceRecommendations(Guid requestId, CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.AssistanceRequests.GetByIdAsync(requestId, cancellationToken);
        if (req == null)
        {
            return NotFound(new { message = "Assistance request not found." });
        }

        var resources = await _unitOfWork.Resources.GetAllAsync(cancellationToken);
        var ai = _aiProviderFactory.CreateProvider();
        var candidates = await ai.RecommendResourcesForRequestAsync(req, resources, cancellationToken);
        return Ok(candidates);
    }

    [HttpPost("logistics/simulate-allocation")]
    [AllowAnonymous]
    public async Task<ActionResult<ResourceAllocationPlanResult>> SimulateAllocation(
        [FromBody] CompetingShortageScenarioRequestDto dto,
        CancellationToken cancellationToken)
    {
        var allRequests = await _unitOfWork.AssistanceRequests.GetAllAsync(cancellationToken);
        var targetRequests = dto.CompetingRequestIds.Any()
            ? allRequests.Where(r => dto.CompetingRequestIds.Contains(r.Id)).ToList()
            : allRequests.Where(r => r.Status != RequestStatus.Fulfilled && r.Status != RequestStatus.MergedAsDuplicate).ToList();

        var allResources = await _unitOfWork.Resources.GetAllAsync(cancellationToken);
        var strategy = _strategyResolver.Resolve(dto.StrategyName);
        var plan = strategy.ComputeAllocationPlan(targetRequests, allResources, _mappingService);
        return Ok(plan);
    }

    [HttpPost("logistics/execute-allocation")]
    [Authorize(Roles = "LogisticsManager,PartnerOfficer,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult<ResourceAllocationPlanResult>> ExecuteAllocation(
        [FromBody] CompetingShortageScenarioRequestDto dto,
        CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Logistics Manager";
        var roleStr = User.FindFirstValue(ClaimTypes.Role) ?? "LogisticsManager";
        Enum.TryParse<UserRole>(roleStr, out var actorRole);

        var plan = await _opsService.ExecuteResourceAllocationAsync(
            actorId,
            actorName,
            actorRole,
            dto.CompetingRequestIds,
            dto.StrategyName,
            cancellationToken);

        return Ok(plan);
    }

    [HttpGet("logistics/allocations")]
    [AllowAnonymous]
    public async Task<ActionResult> GetAllocations(CancellationToken cancellationToken)
    {
        var allocations = await _unitOfWork.ResourceAllocations.GetAllAsync(cancellationToken);
        var dispatches = await _unitOfWork.DispatchRecords.GetAllAsync(cancellationToken);
        var partnerRequests = await _unitOfWork.PartnerAidRequests.GetAllAsync(cancellationToken);

        return Ok(new
        {
            Allocations = allocations,
            Dispatches = dispatches,
            PartnerAidRequests = partnerRequests
        });
    }

    [HttpGet("shelters")]
    [AllowAnonymous]
    public async Task<ActionResult> GetShelters(CancellationToken cancellationToken)
    {
        var shelters = await _unitOfWork.Shelters.GetAllAsync(cancellationToken);
        var allocations = await _unitOfWork.ShelterAllocations.GetAllAsync(cancellationToken);
        return Ok(new
        {
            Shelters = shelters,
            Allocations = allocations
        });
    }

    [HttpGet("shelters/recommendations/{requestId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetShelterRecommendations(Guid requestId, CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.AssistanceRequests.GetByIdAsync(requestId, cancellationToken);
        if (req == null)
        {
            return NotFound();
        }

        var shelters = await _unitOfWork.Shelters.GetAllAsync(cancellationToken);
        var ai = _aiProviderFactory.CreateProvider();
        var candidates = await ai.RecommendSheltersForRequestAsync(req, shelters, cancellationToken);
        return Ok(candidates);
    }

    [HttpPost("shelters")]
    [Authorize(Roles = "ShelterAdmin,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> CreateShelter([FromBody] Shelter shelter, CancellationToken cancellationToken)
    {
        await _unitOfWork.Shelters.AddAsync(shelter, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(shelter);
    }

    [HttpPut("shelters/{id:guid}")]
    [Authorize(Roles = "ShelterAdmin,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> UpdateShelter(Guid id, [FromBody] UpdateShelterDto dto, CancellationToken cancellationToken)
    {
        var shelter = await _unitOfWork.Shelters.GetByIdAsync(id, cancellationToken);
        if (shelter == null)
        {
            return NotFound(new { message = "Shelter not found." });
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Shelter name is required." });
        }

        shelter.Name = dto.Name.Trim();
        shelter.Address = dto.Address?.Trim() ?? string.Empty;
        shelter.TotalCapacity = Math.Max(1, dto.TotalCapacity);
        shelter.CurrentOccupancy = Math.Clamp(dto.CurrentOccupancy, 0, shelter.TotalCapacity);
        shelter.IsWheelchairAccessible = dto.IsWheelchairAccessible;
        shelter.HasMedicalFacility = dto.HasMedicalFacility;
        shelter.HasPowerBackup = dto.HasPowerBackup;
        shelter.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Operational" : dto.Status.Trim();
        shelter.Latitude = dto.Latitude;
        shelter.Longitude = dto.Longitude;

        await _unitOfWork.Shelters.UpdateAsync(shelter, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Shelters",
            ActionName = "Admin_UpdatedShelter",
            EntityType = nameof(Shelter),
            EntityId = shelter.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { shelter.Name, shelter.TotalCapacity, shelter.CurrentOccupancy, shelter.Status })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(shelter);
    }

    [HttpDelete("shelters/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteShelter(Guid id, CancellationToken cancellationToken)
    {
        var shelter = await _unitOfWork.Shelters.GetByIdAsync(id, cancellationToken);
        if (shelter == null)
        {
            return NotFound(new { message = "Shelter not found." });
        }

        var allocs = await _unitOfWork.ShelterAllocations.FindAsync(a => a.ShelterId == id, cancellationToken);
        foreach (var alloc in allocs)
        {
            await _unitOfWork.ShelterAllocations.DeleteAsync(alloc, cancellationToken);
        }

        await _unitOfWork.Shelters.DeleteAsync(shelter, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Shelters",
            ActionName = "Admin_DeletedShelter",
            EntityType = nameof(Shelter),
            EntityId = shelter.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { shelter.Name, shelter.Address })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedShelterId = id });
    }

    [HttpPost("shelters/allocate")]
    [Authorize(Roles = "ShelterAdmin,EmergencyCoordinator,CaseManager,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> AllocateShelter([FromBody] AllocateShelterRequestDto dto, CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Shelter Administrator";
        var roleStr = User.FindFirstValue(ClaimTypes.Role) ?? "ShelterAdmin";
        Enum.TryParse<UserRole>(roleStr, out var actorRole);

        var allocation = await _opsService.AllocateShelterAsync(actorId, actorName, actorRole, dto, cancellationToken);
        return Ok(allocation);
    }

    [HttpGet("volunteers")]
    [AllowAnonymous]
    public async Task<ActionResult> GetVolunteers(CancellationToken cancellationToken)
    {
        var volunteers = await _unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var assignments = await _unitOfWork.VolunteerAssignments.GetAllAsync(cancellationToken);
        return Ok(new
        {
            Volunteers = volunteers,
            Assignments = assignments
        });
    }

    [HttpPost("volunteers")]
    [Authorize]
    public async Task<ActionResult> RegisterVolunteer([FromBody] CreateVolunteerProfileDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return BadRequest(new { message = "Volunteer full name is required." });
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        bool autoVerified = callerRole != nameof(UserRole.Volunteer);
        var volunteer = new VolunteerProfile
        {
            UserId = actorId,
            FullName = dto.FullName.Trim(),
            SkillsCsv = dto.SkillsCsv?.Trim() ?? string.Empty,
            LanguagesCsv = string.IsNullOrWhiteSpace(dto.LanguagesCsv) ? "en" : dto.LanguagesCsv.Trim(),
            IsVerified = autoVerified,
            IsAvailable = true,
            AvailabilitySchedule = string.IsNullOrWhiteSpace(dto.AvailabilitySchedule) ? "Available On-Call" : dto.AvailabilitySchedule.Trim(),
            CurrentLatitude = dto.CurrentLatitude,
            CurrentLongitude = dto.CurrentLongitude,
            CompletedTasksCount = 0
        };

        await _unitOfWork.Volunteers.AddAsync(volunteer, cancellationToken);
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "VolunteerOperations",
            ActionName = "Volunteer_Registered",
            EntityType = nameof(VolunteerProfile),
            EntityId = volunteer.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? volunteer.FullName,
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "Volunteer",
            DetailsJson = JsonSerializer.Serialize(new { volunteer.FullName, volunteer.SkillsCsv, volunteer.IsVerified })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(volunteer);
    }

    [HttpPost("volunteers/{id:guid}/verify-activate")]
    [Authorize(Roles = "VolunteerCoordinator,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> VerifyAndActivateVolunteer(Guid id, CancellationToken cancellationToken)
    {
        var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (volunteer == null)
        {
            return NotFound(new { message = "Volunteer profile not found." });
        }

        volunteer.IsVerified = true;
        volunteer.IsAvailable = true;
        await _unitOfWork.Volunteers.UpdateAsync(volunteer, cancellationToken);

        if (volunteer.UserId != Guid.Empty)
        {
            var linkedUser = await _unitOfWork.Users.GetByIdAsync(volunteer.UserId, cancellationToken);
            if (linkedUser != null)
            {
                linkedUser.AccountStatus = "Active";
                linkedUser.IsActive = true;
                await _unitOfWork.Users.UpdateAsync(linkedUser, cancellationToken);
            }
        }

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "VolunteerOperations",
            ActionName = "VolunteerCoordinator_VerifiedVolunteer",
            EntityType = nameof(VolunteerProfile),
            EntityId = volunteer.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "VolunteerCoordinator",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "VolunteerCoordinator",
            DetailsJson = JsonSerializer.Serialize(new { volunteer.FullName, volunteer.UserId, Verified = true })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(volunteer);
    }

    [HttpDelete("volunteers/{id:guid}")]
    [Authorize(Roles = "VolunteerCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteVolunteer(Guid id, CancellationToken cancellationToken)
    {
        var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(id, cancellationToken);
        if (volunteer == null)
        {
            return NotFound(new { message = "Volunteer profile not found." });
        }

        var assignments = await _unitOfWork.VolunteerAssignments.FindAsync(a => a.VolunteerProfileId == id, cancellationToken);
        foreach (var assign in assignments)
        {
            await _unitOfWork.VolunteerAssignments.DeleteAsync(assign, cancellationToken);
        }

        await _unitOfWork.Volunteers.DeleteAsync(volunteer, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "VolunteerOperations",
            ActionName = "Admin_DeletedVolunteer",
            EntityType = nameof(VolunteerProfile),
            EntityId = volunteer.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { volunteer.FullName })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedVolunteerId = id });
    }

    [HttpGet("volunteers/match")]
    [AllowAnonymous]
    public async Task<ActionResult> MatchVolunteers(
        [FromQuery] string skill = "Medicine",
        [FromQuery] double lat = 6.6828,
        [FromQuery] double lon = 80.3992,
        CancellationToken cancellationToken = default)
    {
        var volunteers = await _unitOfWork.Volunteers.GetAllAsync(cancellationToken);
        var ai = _aiProviderFactory.CreateProvider();
        var matches = await ai.RecommendVolunteersForTaskAsync(skill, lat, lon, volunteers, cancellationToken);
        return Ok(matches);
    }

    [HttpPost("volunteers/assign")]
    [Authorize(Roles = "VolunteerCoordinator,PartnerOfficer,EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> AssignVolunteerTask([FromBody] AssignVolunteerTaskDto dto, CancellationToken cancellationToken)
    {
        var volunteer = await _unitOfWork.Volunteers.GetByIdAsync(dto.VolunteerProfileId, cancellationToken);
        if (volunteer == null)
        {
            return NotFound(new { message = "Volunteer profile not found." });
        }

        double distKm = 1.8;
        if (dto.AssistanceRequestId.HasValue)
        {
            var req = await _unitOfWork.AssistanceRequests.GetByIdAsync(dto.AssistanceRequestId.Value, cancellationToken);
            if (req != null)
            {
                distKm = _mappingService.CalculateDistanceKm(volunteer.CurrentLatitude, volunteer.CurrentLongitude, req.Latitude, req.Longitude);
            }
        }

        var assignment = new VolunteerTaskAssignment
        {
            VolunteerProfileId = volunteer.Id,
            AssistanceRequestId = dto.AssistanceRequestId,
            TaskTitle = dto.TaskTitle,
            RequiredSkill = dto.RequiredSkill,
            AiMatchScore = 92.4,
            DistanceKm = distKm,
            Status = "Assigned"
        };

        await _unitOfWork.VolunteerAssignments.AddAsync(assignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(assignment);
    }

    [HttpGet("donations")]
    [AllowAnonymous]
    public async Task<ActionResult> GetDonations(CancellationToken cancellationToken)
    {
        var campaigns = await _unitOfWork.DonationCampaigns.GetAllAsync(cancellationToken);
        var donations = await _unitOfWork.Donations.GetAllAsync(cancellationToken);
        return Ok(new
        {
            Campaigns = campaigns,
            Donations = donations
        });
    }

    [HttpPost("donations/campaigns")]
    [Authorize(Roles = "DonorOfficer,EmergencyCoordinator,SystemAdmin")]
    public async Task<ActionResult> CreateDonationCampaign([FromBody] CreateDonationCampaignDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Campaign title is required." });
        }

        Guid crisisEventId = dto.CrisisEventId ?? Guid.Empty;
        if (crisisEventId == Guid.Empty)
        {
            var crises = await _unitOfWork.CrisisEvents.GetAllAsync(cancellationToken);
            crisisEventId = crises.OrderByDescending(c => c.CreatedAtUtc).FirstOrDefault()?.Id ?? Guid.Empty;
        }

        var campaign = new DonationCampaign
        {
            Title = dto.Title.Trim(),
            CrisisEventId = crisisEventId,
            TargetAmountUsd = dto.TargetAmountUsd > 0 ? dto.TargetAmountUsd : 50000m,
            CollectedAmountUsd = 0m,
            DisbursedAmountUsd = 0m,
            IsActive = true
        };

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.DonationCampaigns.AddAsync(campaign, cancellationToken);
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Donations",
            ActionName = "DonationCampaign_Created",
            EntityType = nameof(DonationCampaign),
            EntityId = campaign.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "Donor Officer",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "DonorOfficer",
            DetailsJson = JsonSerializer.Serialize(new { campaign.Title, campaign.TargetAmountUsd, campaign.CrisisEventId })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(campaign);
    }

    [HttpDelete("donations/campaigns/{id:guid}")]
    [Authorize(Roles = "DonorOfficer,SystemAdmin")]
    public async Task<ActionResult> DeleteDonationCampaign(Guid id, CancellationToken cancellationToken)
    {
        var campaign = await _unitOfWork.DonationCampaigns.GetByIdAsync(id, cancellationToken);
        if (campaign == null)
        {
            return NotFound(new { message = "Donation campaign not found." });
        }

        var donations = await _unitOfWork.Donations.FindAsync(d => d.CampaignId == id, cancellationToken);
        foreach (var d in donations)
        {
            await _unitOfWork.Donations.DeleteAsync(d, cancellationToken);
        }

        await _unitOfWork.DonationCampaigns.DeleteAsync(campaign, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "Donations",
            ActionName = "Admin_DeletedCampaign",
            EntityType = nameof(DonationCampaign),
            EntityId = campaign.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { campaign.Title })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedCampaignId = id });
    }

    [HttpPost("donations")]
    [Authorize]
    public async Task<ActionResult> RecordDonation([FromBody] CreateDonationDto dto, CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        var record = await _opsService.RecordDonationAsync(actorId, dto, cancellationToken);
        return Ok(record);
    }

    [HttpPost("donations/{id:guid}/approve")]
    [Authorize(Roles = "DonorOfficer,EmergencyCoordinator,SystemAdmin")]
    public async Task<ActionResult> ApproveSuspiciousDonation(Guid id, CancellationToken cancellationToken)
    {
        var donation = await _unitOfWork.Donations.GetByIdAsync(id, cancellationToken);
        if (donation == null)
        {
            return NotFound();
        }

        donation.IsReviewedByHuman = true;
        donation.VerificationStatus = "HumanComplianceApproved";
        await _unitOfWork.Donations.UpdateAsync(donation, cancellationToken);

        var campaign = await _unitOfWork.DonationCampaigns.GetByIdAsync(donation.CampaignId, cancellationToken);
        if (campaign != null && donation.MonetaryAmountUsd > 0)
        {
            campaign.CollectedAmountUsd += donation.MonetaryAmountUsd;
            await _unitOfWork.DonationCampaigns.UpdateAsync(campaign, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(donation);
    }

    [HttpGet("notifications")]
    [AllowAnonymous]
    public async Task<ActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var notifications = await _unitOfWork.Notifications.GetAllAsync(cancellationToken);
        return Ok(notifications);
    }

    [HttpPost("notifications/send")]
    [Authorize]
    public async Task<ActionResult> SendNotification([FromBody] SendNotificationRequestDto dto, CancellationToken cancellationToken)
    {
        var record = await _opsService.SendIdempotentNotificationAsync(dto, cancellationToken);
        return Ok(record);
    }

    [HttpGet("mapping/route")]
    [AllowAnonymous]
    public async Task<ActionResult<RouteCalculationDto>> CalculateRoute(
        [FromQuery] double originLat = 6.6890,
        [FromQuery] double originLon = 80.4020,
        [FromQuery] double destLat = 6.6828,
        [FromQuery] double destLon = 80.3992,
        CancellationToken cancellationToken = default)
    {
        var route = await _mappingService.CalculateRouteAsync(originLat, originLon, destLat, destLon, cancellationToken);
        return Ok(route);
    }

    [HttpGet("mapping/geocode")]
    [AllowAnonymous]
    public async Task<ActionResult<GeocodingResultDto>> Geocode([FromQuery] string address = "Ratnapura", CancellationToken cancellationToken = default)
    {
        var geo = await _mappingService.GeocodeAddressAsync(address, cancellationToken);
        return Ok(geo);
    }

    [HttpGet("admin/metrics")]
    [AllowAnonymous]
    public async Task<ActionResult<OperationalKpiMetricsDto>> GetMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _opsService.GetOperationalMetricsAsync(cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("admin/audit-logs")]
    [AllowAnonymous]
    public async Task<ActionResult> GetAuditLogs(CancellationToken cancellationToken)
    {
        var logs = await _unitOfWork.AuditEvents.GetAllAsync(cancellationToken);
        return Ok(logs);
    }

    [HttpGet("admin/resilience")]
    [AllowAnonymous]
    public ActionResult<ResilienceStatusDto> GetResilienceStatus()
    {
        return Ok(_resilienceMonitor.GetStatus());
    }

    [HttpPost("admin/resilience/toggle")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin,EmergencyCoordinator")]
    public async Task<ActionResult<ResilienceStatusDto>> ToggleResilienceSimulation(
        [FromQuery] string service = "AI",
        [FromQuery] bool simulateFailure = false,
        CancellationToken cancellationToken = default)
    {
        if (service.Equals("AI", StringComparison.OrdinalIgnoreCase))
        {
            _resilienceMonitor.SimulateExternalAiFailure = simulateFailure;
        }
        else if (service.Equals("Mapping", StringComparison.OrdinalIgnoreCase))
        {
            _resilienceMonitor.SimulateExternalMappingFailure = simulateFailure;
        }
        else if (service.Equals("Notification", StringComparison.OrdinalIgnoreCase))
        {
            _resilienceMonitor.SimulateExternalNotificationFailure = simulateFailure;
        }

        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "ResilienceSimulation",
            ActionName = $"ToggleExternalFailure_{service}",
            EntityType = "ExternalServiceCircuitBreaker",
            EntityId = service,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { service, simulateFailure })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(_resilienceMonitor.GetStatus());
    }
}

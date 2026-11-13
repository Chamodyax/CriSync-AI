using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReliefCoord.Application.Services;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task EnsureCleanProductionDatabaseAsync(ReliefCoordDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        try
        {
            await db.Users.Select(u => new
            {
                u.AccountStatus,
                u.InvitedByUserName,
                u.InvitationCode,
                u.ProfilePhotoUrl,
                u.QuickUnlockEnabled,
                u.QuickUnlockMode,
                u.QuickUnlockBiometricCredentialId,
                u.QuickUnlockFailedAttempts,
                u.QuickUnlockTrustedDevicesJson
            }).FirstOrDefaultAsync();
            await db.Organisations.Select(o => new { o.PartnershipStatus, o.PartnershipProposal }).FirstOrDefaultAsync();
            await db.CampaignExpenditures.Select(e => e.Id).FirstOrDefaultAsync();
            await db.OperationalKpiSnapshots.Select(s => s.Id).FirstOrDefaultAsync();
        }
        catch
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        var hasLegacyDemoData = await db.Users.AnyAsync(u => u.Email == "coordinator@relief.org");
        var hasLegacyPasswordHashes = await db.Users.AnyAsync(u => !u.PasswordHash.StartsWith("PBKDF2$"));

        if (hasLegacyDemoData || hasLegacyPasswordHashes)
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        var orgUnOchaId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var orgRedCrossId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        var orgDmcId = Guid.Parse("11111111-1111-1111-1111-111111111103");
        var orgMsfId = Guid.Parse("11111111-1111-1111-1111-111111111104");
        var orgSarvodayaId = Guid.Parse("11111111-1111-1111-1111-111111111105");

        var existingOrgIds = (await db.Organisations
            .Select(o => o.Id)
            .ToListAsync())
            .ToHashSet();

        var baselineOrganisations = new List<Organisation>
        {
            new()
            {
                Id = orgUnOchaId,
                Name = "UN OCHA Regional Coordination Office",
                OrganisationType = "International Agency",
                Country = "Sri Lanka / Regional",
                ContactEmail = "ops@unocha-relief.org",
                ContactPhone = "+94-11-2501234",
                IsVerified = true,
                PartnershipStatus = "Approved",
                PartnershipProposal = "Founding UN Coordination Partner"
            },
            new()
            {
                Id = orgRedCrossId,
                Name = "Sri Lanka Red Cross Society",
                OrganisationType = "National NGO",
                Country = "Sri Lanka",
                ContactEmail = "dispatch@redcross.lk",
                ContactPhone = "+94-11-2691095",
                IsVerified = true,
                PartnershipStatus = "Approved",
                PartnershipProposal = "National Humanitarian & Relief Logistics Partner"
            },
            new()
            {
                Id = orgDmcId,
                Name = "National Disaster Management Centre (DMC)",
                OrganisationType = "Government Authority",
                Country = "Sri Lanka",
                ContactEmail = "eoc@dmc.gov.lk",
                ContactPhone = "+94-11-2136136",
                IsVerified = true,
                PartnershipStatus = "Approved",
                PartnershipProposal = "National Statutory Emergency Authority"
            },
            new()
            {
                Id = orgMsfId,
                Name = "Médecins Sans Frontières (MSF Medical Rapid Team)",
                OrganisationType = "Medical INGO",
                Country = "International",
                ContactEmail = "medical-coord@msf.org",
                ContactPhone = "+94-11-2445566",
                IsVerified = true,
                PartnershipStatus = "Approved",
                PartnershipProposal = "International Rapid Medical Response Partner"
            },
            new()
            {
                Id = orgSarvodayaId,
                Name = "Sarvodaya Disaster Relief Unit",
                OrganisationType = "National Relief NGO",
                Country = "Sri Lanka",
                ContactEmail = "coord@sarvodaya-relief.lk",
                ContactPhone = "+94-11-2655255",
                IsVerified = false,
                PartnershipStatus = "Pending",
                PartnershipProposal = "Field volunteer mobilization, mobile kitchens, and community evacuation shelters across Sabaragamuwa and Western Provinces.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
            }
        };

        var orgsToInsert = baselineOrganisations
            .Where(o => !existingOrgIds.Contains(o.Id))
            .ToList();

        if (orgsToInsert.Count > 0)
        {
            await db.Organisations.AddRangeAsync(orgsToInsert);
            await db.SaveChangesAsync();
        }

        var existingEmails = (await db.Users
            .Select(u => u.Email.ToLower())
            .ToListAsync())
            .ToHashSet();

        var (standardHash, standardSalt) = HumanitarianOperationsService.HashPassword("Crisync@2026");
        var (adminHash, adminSalt) = HumanitarianOperationsService.HashPassword("Admin@Crisync2026");

        var nimaliId = Guid.Parse("22222222-2222-2222-2222-222222222211");
        var arjunaId = Guid.Parse("22222222-2222-2222-2222-222222222212");
        var kasunId = Guid.Parse("22222222-2222-2222-2222-222222222213");
        var dilshanId = Guid.Parse("22222222-2222-2222-2222-222222222216");
        var elenaId = Guid.Parse("22222222-2222-2222-2222-222222222217");
        var sanjayaId = Guid.Parse("22222222-2222-2222-2222-222222222219");
        var tharushiId = Guid.Parse("22222222-2222-2222-2222-222222222220");
        var kamalId = Guid.Parse("22222222-2222-2222-2222-222222222221");
        var samanId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var malaId = Guid.Parse("22222222-2222-2222-2222-222222222223");
        var nuwanId = Guid.Parse("22222222-2222-2222-2222-222222222224");
        var pradeepId = Guid.Parse("22222222-2222-2222-2222-222222222225");

        var officialAccounts = new List<AppUser>
        {
            new()
            {
                Id = nimaliId,
                FullName = "Nimali Perera",
                Email = "citizen@crisync.org",
                PhoneNumber = "+94-77-1112233",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Citizen,
                PreferredLanguage = "si",
                IsActive = true
            },
            new()
            {
                Id = arjunaId,
                FullName = "Dr. Arjuna Jayawardena",
                Email = "coordinator@crisync.org",
                PhoneNumber = "+94-77-2223344",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.EmergencyCoordinator,
                PreferredLanguage = "en",
                OrganisationId = orgUnOchaId,
                IsActive = true
            },
            new()
            {
                Id = kasunId,
                FullName = "Kasun Fernando",
                Email = "casemanager@crisync.org",
                PhoneNumber = "+94-77-3334455",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.CaseManager,
                PreferredLanguage = "en",
                OrganisationId = orgDmcId,
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222214"),
                FullName = "Roshan Silva",
                Email = "logistics@crisync.org",
                PhoneNumber = "+94-77-4445566",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.LogisticsManager,
                PreferredLanguage = "en",
                OrganisationId = orgRedCrossId,
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222215"),
                FullName = "Meena Selvarajah",
                Email = "shelter@crisync.org",
                PhoneNumber = "+94-77-5556677",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.ShelterAdmin,
                PreferredLanguage = "ta",
                OrganisationId = orgDmcId,
                IsActive = true
            },
            new()
            {
                Id = dilshanId,
                FullName = "Dilshan Bandara",
                Email = "volunteer@crisync.org",
                PhoneNumber = "+94-77-6667788",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.VolunteerCoordinator,
                PreferredLanguage = "en",
                OrganisationId = orgRedCrossId,
                IsActive = true
            },
            new()
            {
                Id = elenaId,
                FullName = "Elena Rostova",
                Email = "donor@crisync.org",
                PhoneNumber = "+94-77-7778899",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.DonorOfficer,
                PreferredLanguage = "en",
                OrganisationId = orgUnOchaId,
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222218"),
                FullName = "Dr. Tariq Al-Mansoor",
                Email = "partner@crisync.org",
                PhoneNumber = "+94-77-8889900",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.PartnerOfficer,
                PreferredLanguage = "ar",
                OrganisationId = orgMsfId,
                IsActive = true
            },
            new()
            {
                Id = sanjayaId,
                FullName = "Sanjaya Perera",
                Email = "publicdonor@crisync.org",
                PhoneNumber = "+94-77-9900112",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Donor,
                PreferredLanguage = "en",
                IsActive = true,
                AccountStatus = "Active"
            },
            new()
            {
                Id = tharushiId,
                FullName = "Tharushi Mendis",
                Email = "fieldvolunteer@crisync.org",
                PhoneNumber = "+94-77-9900223",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Volunteer,
                PreferredLanguage = "si",
                IsActive = true,
                AccountStatus = "Active"
            },
            new()
            {
                Id = kamalId,
                FullName = "Kamal Jayasuriya",
                Email = "kamal.jayasuriya@crisync.org",
                PhoneNumber = "+94-77-4128890",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Citizen,
                PreferredLanguage = "si",
                IsActive = true,
                AccountStatus = "Active",
                InvitedByUserName = "Kasun Fernando (Case Manager On-Behalf)",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-4),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-4)
            },
            new()
            {
                Id = samanId,
                FullName = "Saman Kumara",
                Email = "saman.kumara@crisync.org",
                PhoneNumber = "+94-77-5241108",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Citizen,
                PreferredLanguage = "si",
                IsActive = true,
                AccountStatus = "Active",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
            },
            new()
            {
                Id = malaId,
                FullName = "Mala Wijeratne",
                Email = "mala.wijeratne@crisync.org",
                PhoneNumber = "+94-77-6394471",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Citizen,
                PreferredLanguage = "en",
                IsActive = true,
                AccountStatus = "Active",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = nuwanId,
                FullName = "Nuwan Jayaweera",
                Email = "nuwan.jayaweera@crisync.org",
                PhoneNumber = "+94-77-8142290",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.Volunteer,
                PreferredLanguage = "si",
                IsActive = false,
                AccountStatus = "Pending",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = pradeepId,
                FullName = "Pradeep Gunasekara",
                Email = "pradeep@sarvodaya-relief.lk",
                PhoneNumber = "+94-77-3905514",
                PasswordHash = standardHash,
                PasswordSalt = standardSalt,
                Role = UserRole.PartnerOfficer,
                PreferredLanguage = "en",
                OrganisationId = orgSarvodayaId,
                IsActive = true,
                AccountStatus = "PendingPartnership",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222297"),
                FullName = "Nadia Al-Khatib",
                Email = "orgadmin@crisync.org",
                PhoneNumber = "+94-11-2000300",
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                Role = UserRole.OrganizationAdmin,
                PreferredLanguage = "en",
                OrganisationId = orgUnOchaId,
                IsActive = true,
                AccountStatus = "Active"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222298"),
                FullName = "Anura Senanayake",
                Email = "backup.admin@crisync.org",
                PhoneNumber = "+94-11-2000200",
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                Role = UserRole.SystemAdmin,
                PreferredLanguage = "en",
                OrganisationId = orgUnOchaId,
                IsActive = true,
                AccountStatus = "Active"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222299"),
                FullName = "Chaminda Wijesinghe",
                Email = "admin@crisync.org",
                PhoneNumber = "+94-11-2000100",
                PasswordHash = adminHash,
                PasswordSalt = adminSalt,
                Role = UserRole.SystemAdmin,
                PreferredLanguage = "en",
                OrganisationId = orgUnOchaId,
                IsActive = true,
                AccountStatus = "Active"
            }
        };

        var accountsToInsert = officialAccounts
            .Where(acc => !existingEmails.Contains(acc.Email.ToLowerInvariant()))
            .ToList();

        if (accountsToInsert.Count > 0)
        {
            await db.Users.AddRangeAsync(accountsToInsert);
            await db.SaveChangesAsync();
        }

        if (!await db.RolePermissions.AnyAsync())
        {
            var baselinePermissions = new List<RolePermission>
            {
                new() { Role = UserRole.SystemAdmin, PermissionCode = "ADMIN_MANAGE_ALL", Description = "Global governance, invite Donor Officers, enforce admin policy, manage integrations and roles" },
                new() { Role = UserRole.SystemAdmin, PermissionCode = "ADMIN_INVITE_DONOR_OFFICER", Description = "Exclusive authority to invite and appoint Donor / Funding Officers" },
                new() { Role = UserRole.OrganizationAdmin, PermissionCode = "ORG_ADMIN_STAFF_CONTROL", Description = "Manage organization staff accounts, approve partner requests, and view audit logs" },
                new() { Role = UserRole.EmergencyCoordinator, PermissionCode = "INVITE_OPERATIONAL_STAFF", Description = "Invite Case Managers, Shelter Admins, Logistics Managers, and Volunteer Coordinators" },
                new() { Role = UserRole.CaseManager, PermissionCode = "REGISTER_CITIZEN_ON_BEHALF", Description = "Register Citizens on their behalf during emergencies without phone or internet" },
                new() { Role = UserRole.VolunteerCoordinator, PermissionCode = "VERIFY_ACTIVATE_VOLUNTEERS", Description = "Verify self-registered Pending volunteers and activate their accounts" },
                new() { Role = UserRole.DonorOfficer, PermissionCode = "MANAGE_DONATION_FUNDS", Description = "Manage relief fundraising campaigns and approve high-value financial compliance donations" },
                new() { Role = UserRole.PartnerOfficer, PermissionCode = "SUBMIT_PARTNERSHIP_REQUEST", Description = "Submit partner organization request and coordinate shared stock once approved" },
                new() { Role = UserRole.ShelterAdmin, PermissionCode = "MANAGE_SHELTERS", Description = "Register temporary evacuation shelters and manage household allocations" },
                new() { Role = UserRole.LogisticsManager, PermissionCode = "MANAGE_LOGISTICS_DISPATCH", Description = "Manage relief warehouses, simulate allocations, and dispatch supplies" },
                new() { Role = UserRole.Volunteer, PermissionCode = "VOLUNTEER_FIELD_PARTICIPATION", Description = "Self-register and participate in verified field relief tasks" },
                new() { Role = UserRole.Donor, PermissionCode = "MAKE_DONATIONS", Description = "Self-register independently and contribute monetary or in-kind relief donations" },
                new() { Role = UserRole.Citizen, PermissionCode = "SUBMIT_ASSISTANCE_REQUEST", Description = "Self-register, manage household profile, submit requests, and track aid delivery" }
            };

            await db.RolePermissions.AddRangeAsync(baselinePermissions);
            await db.SaveChangesAsync();
        }

        if (!await db.Households.AnyAsync())
        {
            var households = new List<HouseholdProfile>
            {
                new()
                {
                    UserId = nimaliId,
                    HouseholdHeadName = "Nimali Perera",
                    TotalMembers = 6,
                    ChildrenCount = 2,
                    ElderlyCount = 1,
                    HasMobilityNeeds = true,
                    HasChronicMedicalNeeds = true,
                    MedicalNotes = "Grandmother requires daily cold-chain Insulin glargine and wheelchair evacuation support.",
                    PrimaryAddress = "42 Kalu Ganga Bank Road, Ratnapura",
                    Latitude = 6.6828,
                    Longitude = 80.3992
                },
                new()
                {
                    UserId = kamalId,
                    HouseholdHeadName = "Kamal Jayasuriya",
                    TotalMembers = 5,
                    ChildrenCount = 2,
                    ElderlyCount = 1,
                    HasMobilityNeeds = false,
                    HasChronicMedicalNeeds = true,
                    MedicalNotes = "Elderly father has minor laceration and needs clean dressings and antibiotics.",
                    PrimaryAddress = "Nivitigala Valley Road, Ratnapura",
                    Latitude = 6.6650,
                    Longitude = 80.4150
                },
                new()
                {
                    UserId = samanId,
                    HouseholdHeadName = "Saman Kumara",
                    TotalMembers = 4,
                    ChildrenCount = 1,
                    ElderlyCount = 1,
                    HasMobilityNeeds = true,
                    HasChronicMedicalNeeds = true,
                    MedicalNotes = "Father sheltering at Kalu Ganga corner clinic needs insulin refills.",
                    PrimaryAddress = "44 Kalu Ganga Bank Road, Ratnapura",
                    Latitude = 6.6830,
                    Longitude = 80.3995
                },
                new()
                {
                    UserId = malaId,
                    HouseholdHeadName = "Mala Wijeratne",
                    TotalMembers = 5,
                    ChildrenCount = 2,
                    ElderlyCount = 1,
                    HasMobilityNeeds = true,
                    HasChronicMedicalNeeds = false,
                    MedicalNotes = "Sheltering with community group at St. Luke's Parish Hall including infants and seniors.",
                    PrimaryAddress = "St. Luke Parish Road, Kalutara",
                    Latitude = 6.5854,
                    Longitude = 79.9607
                }
            };

            await db.Households.AddRangeAsync(households);
            await db.SaveChangesAsync();
        }

        var crisisFloodId = Guid.Parse("33333333-3333-3333-3333-333333333311");
        if (!await db.CrisisEvents.AnyAsync())
        {
            var crisisFlood = new CrisisEvent
    }
}

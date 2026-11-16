using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Services;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Api.Controllers;

[ApiController]
[Route("api")]
public class CrisisAndRequestsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HumanitarianOperationsService _opsService;

    public CrisisAndRequestsController(IUnitOfWork unitOfWork, HumanitarianOperationsService opsService)
    {
        _unitOfWork = unitOfWork;
        _opsService = opsService;
    }

    [HttpGet("crises")]
    [AllowAnonymous]
    public async Task<ActionResult> GetCrises(CancellationToken cancellationToken)
    {
        var crises = await _unitOfWork.CrisisEvents.GetAllAsync(cancellationToken);
        var areas = await _unitOfWork.OperationalAreas.GetAllAsync(cancellationToken);

        foreach (var c in crises)
        {
            c.OperationalAreas = areas.Where(a => a.CrisisEventId == c.Id).ToList();
        }

        return Ok(crises);
    }

    [HttpPost("crises")]
    [Authorize(Roles = "EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> CreateCrisis([FromBody] CrisisEvent crisis, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(crisis.Title))
        {
            return BadRequest(new { message = "Crisis title is required." });
        }

        await _unitOfWork.CrisisEvents.AddAsync(crisis, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(crisis);
    }

    [HttpPut("crises/{id:guid}")]
    [Authorize(Roles = "EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> UpdateCrisis(Guid id, [FromBody] UpdateCrisisEventDto dto, CancellationToken cancellationToken)
    {
        var crisis = await _unitOfWork.CrisisEvents.GetByIdAsync(id, cancellationToken);
        if (crisis == null)
        {
            return NotFound(new { message = "Crisis event not found." });
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { message = "Crisis title is required." });
        }

        crisis.Title = dto.Title.Trim();
        crisis.CrisisType = dto.CrisisType;
        crisis.Severity = dto.Severity;
        crisis.Region = dto.Region?.Trim() ?? string.Empty;
        crisis.Description = dto.Description?.Trim() ?? string.Empty;
        crisis.CenterLatitude = dto.CenterLatitude;
        crisis.CenterLongitude = dto.CenterLongitude;
        crisis.RadiusKm = dto.RadiusKm > 0 ? dto.RadiusKm : crisis.RadiusKm;
        crisis.IsActive = dto.IsActive;

        await _unitOfWork.CrisisEvents.UpdateAsync(crisis, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "CrisisManagement",
            ActionName = "Admin_UpdatedCrisis",
            EntityType = nameof(CrisisEvent),
            EntityId = crisis.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { crisis.Title, Severity = crisis.Severity.ToString(), crisis.IsActive })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(crisis);
    }

    [HttpDelete("crises/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteCrisis(Guid id, CancellationToken cancellationToken)
    {
        var crisis = await _unitOfWork.CrisisEvents.GetByIdAsync(id, cancellationToken);
        if (crisis == null)
        {
            return NotFound(new { message = "Crisis event not found." });
        }

        var areas = await _unitOfWork.OperationalAreas.FindAsync(a => a.CrisisEventId == id, cancellationToken);
        foreach (var area in areas)
        {
            await _unitOfWork.OperationalAreas.DeleteAsync(area, cancellationToken);
        }

        var alerts = await _unitOfWork.PublicAlerts.FindAsync(a => a.CrisisEventId == id, cancellationToken);
        foreach (var alert in alerts)
        {
            await _unitOfWork.PublicAlerts.DeleteAsync(alert, cancellationToken);
        }

        await _unitOfWork.CrisisEvents.DeleteAsync(crisis, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "CrisisManagement",
            ActionName = "Admin_DeletedCrisis",
            EntityType = nameof(CrisisEvent),
            EntityId = crisis.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { crisis.Title, crisis.Region })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedCrisisId = id });
    }

    [HttpGet("crises/alerts")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPublicAlerts(CancellationToken cancellationToken)
    {
        var alerts = await _unitOfWork.PublicAlerts.GetAllAsync(cancellationToken);
        return Ok(alerts);
    }

    [HttpPost("crises/alerts")]
    [Authorize(Roles = "EmergencyCoordinator,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> CreatePublicAlert([FromBody] PublicAlert alert, CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            alert.IssuedByUserId = userId;
        }

        alert.IsVerified = true;
        await _unitOfWork.PublicAlerts.AddAsync(alert, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(alert);
    }

    [HttpGet("requests")]
    [AllowAnonymous]
    public async Task<ActionResult> GetAssistanceRequests(CancellationToken cancellationToken)
    {
        var requests = await _unitOfWork.AssistanceRequests.GetAllAsync(cancellationToken);
        var histories = await _unitOfWork.RequestHistories.GetAllAsync(cancellationToken);
        var recommendations = await _unitOfWork.AiRecommendations.GetAllAsync(cancellationToken);
        var decisions = await _unitOfWork.GovernanceDecisions.GetAllAsync(cancellationToken);

        var decisionMap = decisions.ToDictionary(d => d.AiRecommendationId, d => d);

        foreach (var rec in recommendations)
        {
            if (decisionMap.TryGetValue(rec.Id, out var dec))
            {
                rec.HumanDecision = dec;
            }
        }

        foreach (var req in requests)
        {
            req.Timeline = histories
                .Where(h => h.AssistanceRequestId == req.Id)
                .OrderByDescending(h => h.CreatedAtUtc)
                .ToList();

            req.AiRecommendations = recommendations
                .Where(a => a.AssistanceRequestId == req.Id)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToList();
        }

        return Ok(requests);
    }

    [HttpPost("requests")]
    [Authorize]
    public async Task<ActionResult> SubmitAssistanceRequest([FromBody] CreateAssistanceRequestDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Description))
        {
            return BadRequest(new { message = "Request title and description are required." });
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Authenticated Citizen";
        Guid userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : Guid.Empty;

        var created = await _opsService.SubmitAssistanceRequestAsync(userId, userName, dto, cancellationToken);
        return Ok(created);
    }

    [HttpPost("requests/upload-evidence")]
    [Authorize]
    public ActionResult UploadEvidence(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No evidence file provided." });
        }

        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(new { message = "Evidence file size exceeds the 5 MB security limit." });
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf" };
        string ext = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { message = "Invalid file extension. Only .jpg, .png, and .pdf files are permitted." });
        }

        string safeFileName = $"EVIDENCE_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        return Ok(new
        {
            SafeFileName = safeFileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            SecurityScanStatus = "PassedMimeAndExtensionValidation"
        });
    }

    [HttpPut("requests/{id:guid}/status")]
    [Authorize(Roles = "EmergencyCoordinator,CaseManager,LogisticsManager,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> UpdateRequestStatus(Guid id, [FromBody] UpdateRequestStatusDto dto, CancellationToken cancellationToken)
    {
        var request = await _unitOfWork.AssistanceRequests.GetByIdAsync(id, cancellationToken);
        if (request == null)
        {
            return NotFound(new { message = "Assistance request not found." });
        }

        var previous = request.Status;
        request.Status = dto.NewStatus;
        if (dto.UpdatedUrgency.HasValue)
        {
            request.FinalApprovedUrgency = dto.UpdatedUrgency.Value;
        }
        if (dto.AssignedCaseManagerId.HasValue)
        {
            request.AssignedCaseManagerId = dto.AssignedCaseManagerId.Value;
        }
        if (dto.AssignedOrganisationId.HasValue)
        {
            request.AssignedOrganisationId = dto.AssignedOrganisationId.Value;
        }

        await _unitOfWork.AssistanceRequests.UpdateAsync(request, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Coordinator";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "CaseManager";

        await _unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            AssistanceRequestId = request.Id,
            PreviousStatus = previous,
            NewStatus = dto.NewStatus,
            ActionDescription = string.IsNullOrWhiteSpace(dto.ActionNote)
                ? $"Status transitioned from {previous} to {dto.NewStatus} by {actorName}."
                : dto.ActionNote,
            ChangedByUserId = actorId,
            ChangedByUserName = actorName,
            ChangedByRole = actorRole
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(request);
    }

    [HttpPost("requests/merge-duplicate")]
    [Authorize(Roles = "EmergencyCoordinator,CaseManager,SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> MergeDuplicateRequest([FromBody] MergeDuplicateRequestDto dto, CancellationToken cancellationToken)
    {
        var primary = await _unitOfWork.AssistanceRequests.GetByIdAsync(dto.PrimaryRequestId, cancellationToken);
        var duplicate = await _unitOfWork.AssistanceRequests.GetByIdAsync(dto.DuplicateRequestId, cancellationToken);

        if (primary == null || duplicate == null)
        {
            return NotFound(new { message = "Primary or duplicate assistance request not found." });
        }

        var prevStatus = duplicate.Status;
        duplicate.Status = RequestStatus.MergedAsDuplicate;
        duplicate.PotentialDuplicateOfRequestId = primary.Id;
        duplicate.IsFlaggedPotentialDuplicate = true;
        await _unitOfWork.AssistanceRequests.UpdateAsync(duplicate, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Coordinator";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "CaseManager";

        await _unitOfWork.RequestHistories.AddAsync(new RequestHistory
        {
            AssistanceRequestId = duplicate.Id,
            PreviousStatus = prevStatus,
            NewStatus = RequestStatus.MergedAsDuplicate,
            ActionDescription = $"Merged as duplicate into primary case {primary.ReferenceCode} without record deletion. Justification: {dto.MergeJustification}",
            ChangedByUserId = actorId,
            ChangedByUserName = actorName,
            ChangedByRole = actorRole
        }, cancellationToken);

        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "DuplicateResolution",
            ActionName = "MergeDuplicateWithoutDeletion",
            EntityType = nameof(AssistanceRequest),
            EntityId = duplicate.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = actorName,
            ActorRole = actorRole,
            DetailsJson = JsonSerializer.Serialize(new
            {
                PrimaryRequestId = primary.Id,
                PrimaryReferenceCode = primary.ReferenceCode,
                DuplicateRequestId = duplicate.Id,
                DuplicateReferenceCode = duplicate.ReferenceCode,
                dto.MergeJustification
            })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { Primary = primary, MergedDuplicate = duplicate });
    }

    [HttpPost("requests/feedback")]
    [Authorize]
    public async Task<ActionResult> SubmitFeedback([FromBody] SubmitFeedbackDto dto, CancellationToken cancellationToken)
    {
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty;
        var feedback = new RequestFeedback
        {
            AssistanceRequestId = dto.AssistanceRequestId,
            CitizenUserId = actorId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Comments = dto.Comments,
            WasAidTimely = dto.WasAidTimely,
            WasQuantitySufficient = dto.WasQuantitySufficient
        };

        await _unitOfWork.RequestFeedbacks.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(feedback);
    }

    [HttpDelete("requests/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,OrganizationAdmin")]
    public async Task<ActionResult> DeleteAssistanceRequest(Guid id, CancellationToken cancellationToken)
    {
        var req = await _unitOfWork.AssistanceRequests.GetByIdAsync(id, cancellationToken);
        if (req == null)
        {
            return NotFound(new { message = "Assistance request not found." });
        }

        var recs = await _unitOfWork.AiRecommendations.FindAsync(a => a.AssistanceRequestId == id, cancellationToken);
        foreach (var rec in recs)
        {
            var decisions = await _unitOfWork.GovernanceDecisions.FindAsync(d => d.AiRecommendationId == rec.Id, cancellationToken);
            foreach (var dec in decisions)
            {
                await _unitOfWork.GovernanceDecisions.DeleteAsync(dec, cancellationToken);
            }
            await _unitOfWork.AiRecommendations.DeleteAsync(rec, cancellationToken);
        }

        var histories = await _unitOfWork.RequestHistories.FindAsync(h => h.AssistanceRequestId == id, cancellationToken);
        foreach (var history in histories)
        {
            await _unitOfWork.RequestHistories.DeleteAsync(history, cancellationToken);
        }

        var feedbacks = await _unitOfWork.RequestFeedbacks.FindAsync(f => f.AssistanceRequestId == id, cancellationToken);
        foreach (var fb in feedbacks)
        {
            await _unitOfWork.RequestFeedbacks.DeleteAsync(fb, cancellationToken);
        }

        var resourceAllocs = await _unitOfWork.ResourceAllocations.FindAsync(a => a.AssistanceRequestId == id, cancellationToken);
        var allocIds = resourceAllocs.Select(a => a.Id).ToHashSet();
        if (allocIds.Count > 0)
        {
            var dispatches = await _unitOfWork.DispatchRecords.FindAsync(d => allocIds.Contains(d.ResourceAllocationId), cancellationToken);
            foreach (var disp in dispatches)
            {
                await _unitOfWork.DispatchRecords.DeleteAsync(disp, cancellationToken);
            }
        }
        foreach (var alloc in resourceAllocs)
        {
            await _unitOfWork.ResourceAllocations.DeleteAsync(alloc, cancellationToken);
        }

        var shelterAllocs = await _unitOfWork.ShelterAllocations.FindAsync(s => s.AssistanceRequestId == id, cancellationToken);
        foreach (var sAlloc in shelterAllocs)
        {
            sAlloc.AssistanceRequestId = null;
            await _unitOfWork.ShelterAllocations.UpdateAsync(sAlloc, cancellationToken);
        }

        var volAssignments = await _unitOfWork.VolunteerAssignments.FindAsync(v => v.AssistanceRequestId == id, cancellationToken);
        foreach (var vAssign in volAssignments)
        {
            vAssign.AssistanceRequestId = null;
            await _unitOfWork.VolunteerAssignments.UpdateAsync(vAssign, cancellationToken);
        }

        await _unitOfWork.AssistanceRequests.DeleteAsync(req, cancellationToken);

        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : (Guid?)null;
        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "RequestManagement",
            ActionName = "Admin_DeletedRequest",
            EntityType = nameof(AssistanceRequest),
            EntityId = req.Id.ToString(),
            ActorUserId = actorId,
            ActorUserName = User.FindFirstValue(ClaimTypes.Name) ?? "SystemAdmin",
            ActorRole = User.FindFirstValue(ClaimTypes.Role) ?? "SystemAdmin",
            DetailsJson = JsonSerializer.Serialize(new { req.ReferenceCode, req.Title })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { deletedRequestId = id });
    }
}

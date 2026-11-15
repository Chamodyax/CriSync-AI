using System.Security.Cryptography;
using System.Text.Json;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Strategies;
using ReliefCoord.Domain.Entities;
using ReliefCoord.Domain.Enums;
using ReliefCoord.Domain.Events;

namespace ReliefCoord.Application.Services;

public class HumanitarianOperationsService
{
    private static readonly SemaphoreSlim ShelterAllocationLock = new(1, 1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiProviderFactory _aiProviderFactory;
    private readonly IMappingProviderService _mappingService;
    private readonly INotificationChannelFactory _notificationFactory;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ResourceAllocationStrategyResolver _strategyResolver;
    private readonly IGovernanceConfigProvider? _governanceConfigProvider;

    public HumanitarianOperationsService(
        IUnitOfWork unitOfWork,
        IAiProviderFactory aiProviderFactory,
        IMappingProviderService mappingService,
        INotificationChannelFactory notificationFactory,
        IDomainEventPublisher eventPublisher,
        ResourceAllocationStrategyResolver strategyResolver,
        IGovernanceConfigProvider? governanceConfigProvider = null)
    {
        _unitOfWork = unitOfWork;
        _aiProviderFactory = aiProviderFactory;
        _mappingService = mappingService;
        _notificationFactory = notificationFactory;
        _eventPublisher = eventPublisher;
        _strategyResolver = strategyResolver;
        _governanceConfigProvider = governanceConfigProvider;
    }

    public static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            100_000,
            HashAlgorithmName.SHA256,
            32);
        string hash = Convert.ToBase64String(keyBytes);
        string salt = Convert.ToBase64String(saltBytes);
        return (hash, salt);
    }

    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
        {
            return false;
        }

        if (storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            string[] parts = storedHash.Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int iterations))
            {
                return false;
            }

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] expectedBytes = Convert.FromBase64String(parts[2]);
                byte[] computedBytes = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    saltBytes,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedBytes.Length);
                return CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
            }
            catch
            {
                return false;
            }
        }

        try
        {
            byte[] combinedBytes = System.Text.Encoding.UTF8.GetBytes(password + storedSalt);
            byte[] computedLegacyBytes = SHA256.HashData(combinedBytes);
            byte[] expectedLegacyBytes = Convert.FromBase64String(storedHash);
            return CryptographicOperations.FixedTimeEquals(computedLegacyBytes, expectedLegacyBytes);
        }
        catch
        {
            return false;
        }
    }

    public async Task<AssistanceRequest> SubmitAssistanceRequestAsync(
        Guid citizenUserId,
        string citizenName,
        CreateAssistanceRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existingByIdempotency = await _unitOfWork.AssistanceRequests.FindAsync(
                r => r.IdempotencyKey == dto.IdempotencyKey && r.CitizenUserId == citizenUserId,
                cancellationToken);
            if (existingByIdempotency.Any())
            {
                return existingByIdempotency.First();
            }
        }

        var existingOpenRequests = await _unitOfWork.AssistanceRequests.FindAsync(
            r => r.CrisisEventId == dto.CrisisEventId &&
                 r.Status != RequestStatus.Closed &&
                 r.Status != RequestStatus.Rejected,
            cancellationToken);

        var request = new AssistanceRequest
        {
            ReferenceCode = $"REQ-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            CitizenUserId = citizenUserId,
            CrisisEventId = dto.CrisisEventId,
            Category = dto.Category,
            SelfReportedUrgency = dto.SelfReportedUrgency,
            FinalApprovedUrgency = dto.SelfReportedUrgency,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            AffectedPeopleCount = Math.Max(1, dto.AffectedPeopleCount),
            RequestedQuantity = Math.Max(1, dto.RequestedQuantity),
            RequiresAccessibilitySupport = dto.RequiresAccessibilitySupport,
            LocationAddress = dto.LocationAddress,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            SupportingEvidenceFileName = dto.SupportingEvidenceFileName,
            SupportingEvidenceMimeType = dto.SupportingEvidenceMimeType,
            Status = RequestStatus.PendingAiReview,
            PreferredLanguage = string.IsNullOrWhiteSpace(dto.PreferredLanguage) ? "en" : dto.PreferredLanguage,
            IdempotencyKey = string.IsNullOrWhiteSpace(dto.IdempotencyKey) ? Guid.NewGuid().ToString("N") : dto.IdempotencyKey
        };

        var aiProvider = _aiProviderFactory.CreateProvider();
        var evaluation = await aiProvider.EvaluateAssistanceRequestAsync(request, existingOpenRequests, cancellationToken);

        request.PriorityScore = evaluation.PriorityScore;
        request.IsFlaggedPotentialDuplicate = evaluation.IsPotentialDuplicate;
        request.PotentialDuplicateOfRequestId = evaluation.DuplicateOfRequestId;
        request.DuplicateSimilarityScore = evaluation.DuplicateSimilarityScore;
        request.IsFlaggedSuspicious = evaluation.IsSuspicious;
        request.SuspiciousReason = evaluation.SuspiciousReason;

        var recommendation = new AiRecommendation
        {
            AssistanceRequestId = request.Id,
            CrisisEventId = request.CrisisEventId,
            RecommendationType = AiRecommendationType.UrgencyScoring,
            ModelIdentifier = evaluation.ProviderUsed,
            RecommendedCategory = evaluation.SuggestedCategory.ToString(),
            RecommendedUrgency = evaluation.SuggestedUrgency,
            RecommendedPriorityScore = evaluation.PriorityScore,
            ConfidenceScore = evaluation.ConfidenceScore,
            Rationale = evaluation.Rationale,
            ExplainableFactorsJson = JsonSerializer.Serialize(evaluation.FeatureWeights),
            SuggestedPayloadJson = JsonSerializer.Serialize(new
            {
                evaluation.SuggestedCategory,
                evaluation.SuggestedUrgency,
                evaluation.IsPotentialDuplicate,
                evaluation.DuplicateOfRequestId,
                evaluation.DuplicateSimilarityScore,
                evaluation.IsSuspicious,
                evaluation.SuspiciousReason
            }),
            GovernanceStatus = GovernanceDecisionStatus.PendingReview
        };

        var history = new RequestHistory
        {
            AssistanceRequestId = request.Id,
            PreviousStatus = RequestStatus.Submitted,
            NewStatus = RequestStatus.PendingAiReview,
            ActionDescription = $"Request submitted by {citizenName}. AI scored priority {evaluation.PriorityScore:F1} (Confidence {evaluation.ConfidenceScore * 100:F0}%). Waiting for authorised human coordinator review.",
            ChangedByUserId = citizenUserId,
            ChangedByUserName = citizenName,
            ChangedByRole = UserRole.Citizen.ToString()
        };

        await _unitOfWork.AssistanceRequests.AddAsync(request, cancellationToken);
        await _unitOfWork.AiRecommendations.AddAsync(recommendation, cancellationToken);
        await _unitOfWork.RequestHistories.AddAsync(history, cancellationToken);

        await _unitOfWork.AuditEvents.AddAsync(new AuditEventRecord
        {
            EventCategory = "AssistanceRequest",
            ActionName = "RequestSubmittedWithAiAssessment",
            EntityType = nameof(AssistanceRequest),
            EntityId = request.Id.ToString(),
            ActorUserId = citizenUserId,
            ActorUserName = citizenName,
            ActorRole = UserRole.Citizen.ToString(),
            DetailsJson = JsonSerializer.Serialize(new
            {
                request.ReferenceCode,
                evaluation.PriorityScore,
                evaluation.ConfidenceScore,
                evaluation.IsPotentialDuplicate,
                evaluation.DuplicateSimilarityScore,
                evaluation.ProviderUsed
            })
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new AssistanceRequestSubmittedEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            request.Id,
            citizenUserId,
            request.ReferenceCode,
            request.PreferredLanguage
        ), cancellationToken);

        request.Timeline = new List<RequestHistory> { history };
        request.AiRecommendations = new List<AiRecommendation> { recommendation };
        return request;
    }

    public async Task<HumanGovernanceDecision> ReviewAiRecommendationAsync(
        Guid reviewerUserId,
        string reviewerName,
        UserRole reviewerRole,
        ReviewAiRecommendationDto dto,
        CancellationToken cancellationToken = default)
    {
        if (reviewerRole != UserRole.EmergencyCoordinator &&
            reviewerRole != UserRole.CaseManager &&
            reviewerRole != UserRole.SystemAdmin &&
            reviewerRole != UserRole.OrganizationAdmin)
        {
            throw new UnauthorizedAccessException("Critical humanitarian AI decisions require EmergencyCoordinator, CaseManager, SystemAdmin, or OrganizationAdmin authority.");
        }

        var recommendation = await _unitOfWork.AiRecommendations.GetByIdAsync(dto.RecommendationId, cancellationToken)
            ?? throw new KeyNotFoundException("AI Recommendation not found.");

        recommendation.GovernanceStatus = dto.DecisionStatus;
        recommendation.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.AiRecommendations.UpdateAsync(recommendation, cancellationToken);

        string originalAiSummary = $"Category={recommendation.RecommendedCategory}, Urgency={recommendation.RecommendedUrgency}, PriorityScore={recommendation.RecommendedPriorityScore:F1}";
        UrgencyLevel finalUrgency = dto.OverriddenUrgency ?? recommendation.RecommendedUrgency;
        string finalCategory = dto.OverriddenCategory?.ToString() ?? recommendation.RecommendedCategory;
        string finalHumanSummary = $"Action={dto.DecisionStatus}, FinalCategory={finalCategory}, FinalUrgency={finalUrgency}";

        var decision = new HumanGovernanceDecision
        {
            AiRecommendationId = recommendation.Id,
            ActionTaken = dto.DecisionStatus,
            OriginalAiRecommendationSummary = originalAiSummary,
            OriginalConfidence = recommendation.ConfidenceScore,
            FinalHumanDecisionSummary = finalHumanSummary,
            OverrideJustification = dto.Justification,
            ResponsibleUserId = reviewerUserId,
            ResponsibleUserName = reviewerName,
            ResponsibleUserRole = reviewerRole.ToString(),
            DecisionTimestampUtc = DateTime.UtcNow
        };

        await _unitOfWork.GovernanceDecisions.AddAsync(decision, cancellationToken);

        if (recommendation.AssistanceRequestId.HasValue)
        {
            var request = await _unitOfWork.AssistanceRequests.GetByIdAsync(recommendation.AssistanceRequestId.Value, cancellationToken);
            if (request != null)
            {
                var prevStatus = request.Status;
                if (dto.DecisionStatus == GovernanceDecisionStatus.Approved || dto.DecisionStatus == GovernanceDecisionStatus.Modified)
                {
                    request.FinalApprovedUrgency = finalUrgency;
                    if (dto.OverriddenCategory.HasValue)
                    {
                        request.Category = dto.OverriddenCategory.Value;
                    }
                    request.Status = finalUrgency >= UrgencyLevel.Critical ? RequestStatus.Escalated : RequestStatus.Prioritised;
                    if (dto.DecisionStatus == GovernanceDecisionStatus.Modified)
                    {
                        request.PriorityScore = Math.Min(100.0, (int)finalUrgency * 19.5);
}

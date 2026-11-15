using System.ComponentModel.DataAnnotations;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Application.DTOs;

public record LoginRequestDto(string Email, string Password);

public record RegisterUserRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    UserRole Role,
    string PreferredLanguage,
    Guid? OrganisationId,
    string? OrganisationName = null,
    string? OrganisationType = null,
    string? Country = null,
    string? PartnershipProposal = null,
    string? SkillsCsv = null,
    string? LanguagesCsv = null,
    string? AvailabilitySchedule = null
);

public record AuthResponseDto(
    string Token,
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    string PreferredLanguage,
    Guid? OrganisationId,
    string OrganisationName,
    string AccountStatus = "Active",
    string PhoneNumber = "",
    string ProfilePhotoUrl = ""
);

public record UpdateMyProfileDto(
    string FullName,
    string PhoneNumber,
    string PreferredLanguage,
    string? ProfilePhotoUrl,
    string? CurrentPassword,
    string? NewPassword
);

public record UpsertHouseholdDto(
    string HouseholdHeadName,
    int TotalMembers,
    int ChildrenCount,
    int ElderlyCount,
    bool HasMobilityNeeds,
    bool HasChronicMedicalNeeds,
    string MedicalNotes,
    string PrimaryAddress,
    double Latitude,
    double Longitude
);

public record CreateAssistanceRequestDto(
    Guid CrisisEventId,
    RequestCategory Category,
    UrgencyLevel SelfReportedUrgency,
    string Title,
    string Description,
    int AffectedPeopleCount,
    int RequestedQuantity,
    bool RequiresAccessibilitySupport,
    string LocationAddress,
    double Latitude,
    double Longitude,
    string SupportingEvidenceFileName,
    string SupportingEvidenceMimeType,
    string PreferredLanguage,
    string IdempotencyKey
);

public record UpdateRequestStatusDto(
    RequestStatus NewStatus,
    UrgencyLevel? UpdatedUrgency,
    Guid? AssignedCaseManagerId,
    Guid? AssignedOrganisationId,
    string ActionNote
);

public record MergeDuplicateRequestDto(
    Guid PrimaryRequestId,
    Guid DuplicateRequestId,
    string MergeJustification
);

public record SubmitFeedbackDto(
    Guid AssistanceRequestId,
    int Rating,
    string Comments,
    bool WasAidTimely,
    bool WasQuantitySufficient
);

public record AiRequestEvaluationResult(
    RequestCategory SuggestedCategory,
    UrgencyLevel SuggestedUrgency,
    double PriorityScore,
    double ConfidenceScore,
    string Rationale,
    Dictionary<string, double> FeatureWeights,
    bool IsPotentialDuplicate,
    Guid? DuplicateOfRequestId,
    double DuplicateSimilarityScore,
    bool IsSuspicious,
    string SuspiciousReason,
    string ProviderUsed
);

public record ReviewAiRecommendationDto(
    Guid RecommendationId,
    GovernanceDecisionStatus DecisionStatus,
    UrgencyLevel? OverriddenUrgency,
    RequestCategory? OverriddenCategory,
    string Justification
);

public record AiResourceCandidateDto(
    Guid ResourceItemId,
    string ResourceName,
    string WarehouseName,
    int AvailableQuantity,
    double DistanceKm,
    double MultiConstraintScore,
    string ScoreBreakdown,
    bool SupportsColdChain,
    int DaysUntilExpiry
);

public record AiShelterCandidateDto(
    Guid ShelterId,
    string ShelterName,
    string Address,
    int AvailableSpots,
    double DistanceKm,
    double SuitabilityScore,
    bool IsWheelchairAccessible,
    bool HasMedicalFacility,
    string RecommendationReason
);

public record AiVolunteerCandidateDto(
    Guid VolunteerProfileId,
    string FullName,
    string SkillsCsv,
    double DistanceKm,
    double MatchScore,
    string MatchExplanation
);

public record DemandForecastItemDto(
    string CategoryName,
    int HistoricalRequestsCount,
    int CurrentRequestedUnits,
    int PredictedNext48HoursDemand,
    int CurrentAvailableStock,
    int ProjectedDeficit,
    string RiskStatus,
    double Confidence
);

public record MultilingualBriefingDto(
    Guid CrisisEventId,
    string LanguageCode,
    string LanguageName,
    string Headline,
    string OperationalSummary,
    string CitizenSafetyAdvisory,
    string GeneratedByProvider
);

public record AllocateResourceRequestDto(
    Guid AssistanceRequestId,
    Guid? PreferredResourceItemId,
    string StrategyName
);

public record CompetingShortageScenarioRequestDto(
    Guid ResourceItemId,
    List<Guid> CompetingRequestIds,
    string StrategyName
);

public record PlannedAllocationItemDto(
    Guid AssistanceRequestId,
    string RequestReferenceCode,
    string RequestTitle,
    UrgencyLevel Urgency,
    double PriorityScore,
    Guid SelectedResourceItemId,
    string SelectedResourceName,
    string WarehouseName,
    int RequestedQuantity,
    int AllocatedQuantity,
    bool IsPartialShortage,
    double ConstraintScore,
    double DistanceKm,
    string AlternativeRecommendation
);

public record ResourceAllocationPlanResult(
    string StrategyUsed,
    bool ShortageEncountered,
    int TotalRequestedAcrossCases,
    int TotalAllocatedAcrossCases,
    int TotalUnmetShortfall,
    List<PlannedAllocationItemDto> Allocations,
    string ArchitecturalExplanation
);


namespace ReliefCoord.Domain.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventType { get; }
}

public record AssistanceRequestSubmittedEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RequestId,
    Guid CitizenUserId,
    string ReferenceCode,
    string PreferredLanguage
) : IDomainEvent
{
    public string EventType => nameof(AssistanceRequestSubmittedEvent);
}

public record AiRecommendationReviewedEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid RecommendationId,
    Guid? RequestId,
    string ActionTaken,
    Guid ReviewerUserId,
    string ReviewerName
) : IDomainEvent
{
    public string EventType => nameof(AiRecommendationReviewedEvent);
}

public record ResourceAllocatedEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid AllocationId,
    Guid RequestId,
    Guid CitizenUserId,
    string ResourceName,
    int AllocatedQuantity,
    bool IsPartialShortage
) : IDomainEvent
{
    public string EventType => nameof(ResourceAllocatedEvent);
}

public record ShelterAllocatedEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid ShelterId,
    string ShelterName,
    Guid CitizenUserId,
    int AllocatedMembersCount
) : IDomainEvent
{

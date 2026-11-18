using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Application.Services;
using ReliefCoord.Domain.Enums;
using ReliefCoord.Domain.Events;

namespace ReliefCoord.Infrastructure.Messaging;

public class InMemoryDomainEventQueue : IDomainEventPublisher
{
    private readonly Channel<IDomainEvent> _channel = Channel.CreateUnbounded<IDomainEvent>();

    public ChannelReader<IDomainEvent> Reader => _channel.Reader;

    public async ValueTask PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(domainEvent, cancellationToken);
    }
}

public class AsynchronousDomainEventWorker : BackgroundService
{
    private readonly InMemoryDomainEventQueue _eventQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AsynchronousDomainEventWorker> _logger;

    public AsynchronousDomainEventWorker(
        InMemoryDomainEventQueue eventQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<AsynchronousDomainEventWorker> logger)
    {
        _eventQueue = eventQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var domainEvent in _eventQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var opsService = scope.ServiceProvider.GetRequiredService<HumanitarianOperationsService>();

                if (domainEvent is AssistanceRequestSubmittedEvent submitted)
                {
                    await opsService.SendIdempotentNotificationAsync(new SendNotificationRequestDto(
                        submitted.CitizenUserId,
                        "citizen-alert@relief.org",
                        NotificationChannel.Sms,
                        submitted.PreferredLanguage,
                        $"Assistance Request {submitted.ReferenceCode} Received",
                        $"Your emergency request ({submitted.ReferenceCode}) has been received and prioritised for human coordinator review.",
                        $"EVT-SUBMIT-{submitted.RequestId}"
                    ), stoppingToken);
                }
                else if (domainEvent is ResourceAllocatedEvent allocated)
                {
                    await opsService.SendIdempotentNotificationAsync(new SendNotificationRequestDto(
                        allocated.CitizenUserId,
                        "citizen-alert@relief.org",
                        NotificationChannel.Sms,
                        "en",
                        $"Aid Dispatched: {allocated.ResourceName}",
                        $"Allocated {allocated.AllocatedQuantity} units of {allocated.ResourceName} to your request.",
                        $"EVT-ALLOC-{allocated.AllocationId}"
                    ), stoppingToken);
                }
                else if (domainEvent is ShelterAllocatedEvent shelterEvt)
                {
                    await opsService.SendIdempotentNotificationAsync(new SendNotificationRequestDto(
                        shelterEvt.CitizenUserId,
                        "citizen-alert@relief.org",
                        NotificationChannel.Sms,
                        "en",
                        $"Shelter Confirmed: {shelterEvt.ShelterName}",
                        $"Your household ({shelterEvt.AllocatedMembersCount} members) has been allocated space at {shelterEvt.ShelterName}.",
                        $"EVT-SHELTER-{shelterEvt.ShelterId}-{shelterEvt.CitizenUserId}"
                    ), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing domain event {EventType}", domainEvent.EventType);
            }
        }
    }
}

using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;

namespace ReliefCoord.Infrastructure.ExternalServices;

public class ServiceResilienceMonitor : IServiceResilienceMonitor
{
    public bool SimulateExternalAiFailure { get; set; }
    public bool SimulateExternalMappingFailure { get; set; }
    public bool SimulateExternalNotificationFailure { get; set; }

    public int AiCircuitTripCount { get; private set; }
    public int MappingCircuitTripCount { get; private set; }
    public int NotificationCircuitTripCount { get; private set; }

    public void RecordTrip(string serviceName)
    {
        if (serviceName == "AI")
        {
            AiCircuitTripCount = AiCircuitTripCount + 1;
        }
        else if (serviceName == "Mapping")
        {
            MappingCircuitTripCount = MappingCircuitTripCount + 1;
        }
        else if (serviceName == "Notification")
        {
            NotificationCircuitTripCount = NotificationCircuitTripCount + 1;
        }
    }

    public ResilienceStatusDto GetStatus()
    {
        string aiMode = SimulateExternalAiFailure ? "CircuitOpen_LocalHeuristicFallback" : "Healthy_PrimaryCloudEnsemble";
        string mappingMode = SimulateExternalMappingFailure ? "CircuitOpen_HaversineOfflineFallback" : "Healthy_OpenStreetMapRouting";
        string notificationMode = SimulateExternalNotificationFailure ? "CircuitOpen_AsyncRetryQueue" : "Healthy_MultiChannelGateway";

        return new ResilienceStatusDto(
            SimulateExternalAiFailure,
            SimulateExternalMappingFailure,
            SimulateExternalNotificationFailure,
            AiCircuitTripCount,
            MappingCircuitTripCount,
            NotificationCircuitTripCount,
            aiMode,
            mappingMode,
            notificationMode
        );
    }
}

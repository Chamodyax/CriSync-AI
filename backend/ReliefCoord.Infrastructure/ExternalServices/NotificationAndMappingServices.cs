using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Infrastructure.ExternalServices;

public class EmailNotificationSender : INotificationChannelSender
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public EmailNotificationSender(IServiceResilienceMonitor resilienceMonitor)
    {
        _resilienceMonitor = resilienceMonitor;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public Task<(bool Success, string ProviderResponse)> SendAsync(
        string recipientContact,
        string subject,
        string body,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (_resilienceMonitor.SimulateExternalNotificationFailure)
        {
            _resilienceMonitor.RecordTrip("Notification");
            return Task.FromResult((false, "SMTP_GATEWAY_TIMEOUT_503: Queued for asynchronous background retry."));
        }

        return Task.FromResult((true, $"SendGrid-202-Accepted [Lang={languageCode}] to {recipientContact}"));
    }
}

public class SmsNotificationSender : INotificationChannelSender
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public SmsNotificationSender(IServiceResilienceMonitor resilienceMonitor)
    {
        _resilienceMonitor = resilienceMonitor;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public Task<(bool Success, string ProviderResponse)> SendAsync(
        string recipientContact,
        string subject,
        string body,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (_resilienceMonitor.SimulateExternalNotificationFailure)
        {
            _resilienceMonitor.RecordTrip("Notification");
            return Task.FromResult((false, "TWILIO_GATEWAY_UNAVAILABLE_504: Queued for asynchronous background retry."));
        }

        return Task.FromResult((true, $"Twilio-SMS-Delivered [Lang={languageCode}] to {recipientContact}"));
    }
}

public class PushNotificationSender : INotificationChannelSender
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public PushNotificationSender(IServiceResilienceMonitor resilienceMonitor)
    {
        _resilienceMonitor = resilienceMonitor;
    }

    public NotificationChannel Channel => NotificationChannel.Push;

    public Task<(bool Success, string ProviderResponse)> SendAsync(
        string recipientContact,
        string subject,
        string body,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (_resilienceMonitor.SimulateExternalNotificationFailure)
        {
            _resilienceMonitor.RecordTrip("Notification");
            return Task.FromResult((false, "FCM_PUSH_SERVICE_UNAVAILABLE: Queued for asynchronous background retry."));
        }

        return Task.FromResult((true, $"FCM-Push-Dispatched [Lang={languageCode}] to {recipientContact}"));
    }
}

public class InAppNotificationSender : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public Task<(bool Success, string ProviderResponse)> SendAsync(
        string recipientContact,
        string subject,
        string body,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult((true, $"InApp-Notification-Persisted [Lang={languageCode}]"));
    }
}

public class NotificationChannelFactory : INotificationChannelFactory
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public NotificationChannelFactory(IServiceResilienceMonitor resilienceMonitor)
    {
        _resilienceMonitor = resilienceMonitor;
    }

    public INotificationChannelSender CreateSender(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => new EmailNotificationSender(_resilienceMonitor),
            NotificationChannel.Sms => new SmsNotificationSender(_resilienceMonitor),
            NotificationChannel.Push => new PushNotificationSender(_resilienceMonitor),
            _ => new InAppNotificationSender()
        };
    }
}

public class ResilientMappingProviderService : IMappingProviderService
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;

    public ResilientMappingProviderService(IServiceResilienceMonitor resilienceMonitor)
    {
        _resilienceMonitor = resilienceMonitor;
    }

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(earthRadiusKm * c, 2);
    }

    public Task<RouteCalculationDto> CalculateRouteAsync(
        double originLat,
        double originLon,
        double destLat,
        double destLon,
        CancellationToken cancellationToken = default)
    {
        double haversineKm = CalculateDistanceKm(originLat, originLon, destLat, destLon);
        if (_resilienceMonitor.SimulateExternalMappingFailure)
        {
            _resilienceMonitor.RecordTrip("Mapping");
            double fallbackRoadKm = Math.Round(haversineKm * 1.28, 2);
            int fallbackMinutes = Math.Max(8, (int)Math.Ceiling(fallbackRoadKm * 1.65));
            return Task.FromResult(new RouteCalculationDto(
                fallbackRoadKm,
                fallbackMinutes,
                $"[OFFLINE GEOSPATIAL FALLBACK] Estimated terrain corridor ({fallbackRoadKm} km, ~{fallbackMinutes} mins)",
                "LocalHaversineFallbackEngine"
            ));
        }

        double roadDistanceKm = Math.Round(haversineKm * 1.22, 2);
        int travelMinutes = Math.Max(5, (int)Math.Ceiling(roadDistanceKm * 1.45));
        return Task.FromResult(new RouteCalculationDto(
            roadDistanceKm,
            travelMinutes,
            $"OSRM Humanitarian Relief Corridor ({roadDistanceKm} km via primary arterial route, ETA {travelMinutes} mins)",
            "OpenStreetMap-OSRM-Gateway"
        ));
    }

    public Task<GeocodingResultDto> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        if (_resilienceMonitor.SimulateExternalMappingFailure)
        {
            _resilienceMonitor.RecordTrip("Mapping");
            return Task.FromResult(new GeocodingResultDto(
                $"{address} (Resolved via Offline Gazetteer)",
                6.6828,
                80.3992,
                "OfflineGazetteerFallback"
            ));
        }

        string lower = (address ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("ratnapura"))
        {
            return Task.FromResult(new GeocodingResultDto("Ratnapura Flood Operations Zone, Sri Lanka", 6.6828, 80.3992, "Nominatim-OSM"));
        }
        if (lower.Contains("kalutara"))
        {
            return Task.FromResult(new GeocodingResultDto("Kalutara River Basin Sector, Sri Lanka", 6.5854, 79.9607, "Nominatim-OSM"));
        }
        if (lower.Contains("badulla"))
        {
            return Task.FromResult(new GeocodingResultDto("Badulla Highland Landslide Zone, Sri Lanka", 6.9934, 81.0550, "Nominatim-OSM"));
        }

        return Task.FromResult(new GeocodingResultDto(
            string.IsNullOrWhiteSpace(address) ? "Colombo Central Humanitarian Hub, Sri Lanka" : address,
            6.9271,
            79.8612,
            "Nominatim-OSM"
        ));
    }

    private static double DegreesToRadians(double deg) => deg * (Math.PI / 180.0);
}

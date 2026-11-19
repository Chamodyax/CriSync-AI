using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ReliefCoord.Application.DTOs;
using ReliefCoord.Application.Interfaces;
using ReliefCoord.Domain.Enums;

namespace ReliefCoord.Infrastructure.ExternalServices;

public class EmailNotificationSender : INotificationChannelSender
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;
    private readonly IConfiguration? _configuration;

    public EmailNotificationSender(IServiceResilienceMonitor resilienceMonitor, IConfiguration? configuration = null)
    {
        _resilienceMonitor = resilienceMonitor;
        _configuration = configuration;
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

        string? smtpHost = _configuration?["ExternalServices:SmtpHost"];
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            return Task.FromResult((true, $"SMTP-Relay-Ready ({smtpHost}) [Lang={languageCode}] to {recipientContact}"));
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
    private readonly IConfiguration? _configuration;

    public NotificationChannelFactory(IServiceResilienceMonitor resilienceMonitor, IConfiguration? configuration = null)
    {
        _resilienceMonitor = resilienceMonitor;
        _configuration = configuration;
    }

    public INotificationChannelSender CreateSender(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => new EmailNotificationSender(_resilienceMonitor, _configuration),
            NotificationChannel.Sms => new SmsNotificationSender(_resilienceMonitor),
            NotificationChannel.Push => new PushNotificationSender(_resilienceMonitor),
            _ => new InAppNotificationSender()
        };
    }
}

public class ResilientMappingProviderService : IMappingProviderService
{
    private readonly IServiceResilienceMonitor _resilienceMonitor;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IConfiguration? _configuration;

    public ResilientMappingProviderService(
        IServiceResilienceMonitor resilienceMonitor,
        IHttpClientFactory? httpClientFactory = null,
        IConfiguration? configuration = null)
    {
        _resilienceMonitor = resilienceMonitor;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    private bool IsLiveOsmEnabled() =>
}

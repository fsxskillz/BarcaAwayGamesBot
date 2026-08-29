using BarcaAwayTickets.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BarcaAwayTickets.Notifications;

public sealed class NotifierFactory(IServiceProvider serviceProvider) : INotifierFactory
{
    public INotifier Create(NotificationOptions options)
    {
        var provider = options.Provider?.Trim();
        if (string.Equals(provider, "Telegram", StringComparison.OrdinalIgnoreCase))
            return serviceProvider.GetRequiredService<TelegramNotifier>();
        if (string.Equals(provider, "WhatsApp", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("The WhatsApp notifier is configured but has not been implemented yet. Set Notification:Provider to Telegram.");
        throw new NotSupportedException($"Unsupported notification provider '{options.Provider}'. Supported values: Telegram, WhatsApp.");
    }
}

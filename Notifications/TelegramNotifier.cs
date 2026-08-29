using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.Notifications;

public sealed class TelegramNotifier(HttpClient httpClient) : INotifier
{
    public async Task NotifyAsync(MatchInfo match, CancellationToken cancellationToken = default)
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        var chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
            throw new InvalidOperationException("TELEGRAM_BOT_TOKEN or TELEGRAM_CHAT_ID is missing.");
        var date = string.IsNullOrWhiteSpace(match.Date) ? "Date not available" : match.Date;
        var closingForm = string.IsNullOrWhiteSpace(match.ClosingForm)
            ? string.Empty
            : $"\n\n⏳ Applications close: {match.ClosingForm}";
        var message = $"🚨 NEW BARÇA AWAY MATCH\n\n{match.Name}\n\n📅 {date}{closingForm}\n\n🎟️ Tickets:\n{match.TicketUrl}";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["chat_id"] = chatId, ["text"] = message, ["disable_web_page_preview"] = "true"
        });
        using var response = await httpClient.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Telegram returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) for {match.Name}.");
        Console.WriteLine($"Telegram notification sent: {match.Name}");
    }
}

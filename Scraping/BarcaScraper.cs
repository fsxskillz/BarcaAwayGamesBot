using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using BarcaAwayTickets.Models;

namespace BarcaAwayTickets.Scraping;

public sealed class BarcaScraper(HttpClient httpClient) : IBarcaScraper
{
    private const string PageUrl = "https://www.fcbarcelona.com/en/club/members/tickets-for-away-matches";

    public async Task<IReadOnlyList<MatchInfo>> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Downloading {PageUrl}");
        ConfigureHttpClient();
        using var response = await httpClient.GetAsync(PageUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"FC Barcelona returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");

        return ParseMatches(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    internal static List<MatchInfo> ParseMatches(string html)
    {
        var document = new HtmlParser().ParseDocument(html);
        var marker = document.QuerySelectorAll("p")
            .FirstOrDefault(p => Normalize(p.TextContent).Equals("AVAILABLE MATCHES", StringComparison.OrdinalIgnoreCase));
        if (marker is null)
            throw new InvalidOperationException("The 'AVAILABLE MATCHES' section was not found. Refusing to treat this as an empty result.");

        var matches = new List<MatchInfo>();
        string? pendingName = null;
        string? pendingDate = null;
        string? pendingClosingForm = null;
        for (var node = marker.NextElementSibling; node is not null; node = node.NextElementSibling)
        {
            var text = Normalize(node.TextContent);
            if (IsEndOfAvailableMatches(text)) break;
            var ticketLink = node.QuerySelectorAll("a").FirstOrDefault(IsTicketLink);
            if (ticketLink is not null)
            {
                AddMatch(matches, pendingName, pendingDate, pendingClosingForm, ticketLink.GetAttribute("href"), ticketLink.TextContent);
                pendingName = null;
                pendingDate = null;
                pendingClosingForm = null;
            }
            else if (node.LocalName.Equals("p", StringComparison.OrdinalIgnoreCase) && node.QuerySelector("strong") is not null)
                pendingName = text.TrimEnd('*').Trim();
            else
            {
                pendingDate ??= TryExtractMatchDate(text);
                pendingClosingForm ??= TryExtractClosingForm(text);
            }
        }
        return matches;
    }

    private void ConfigureHttpClient()
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BarcaAwayTickets", "1.0"));
        if (!httpClient.DefaultRequestHeaders.AcceptLanguage.Any())
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    private static void AddMatch(List<MatchInfo> matches, string? pendingName, string? pendingDate, string? pendingClosingForm, string? href, string linkText)
    {
        if (string.IsNullOrWhiteSpace(href)) return;
        var url = new Uri(new Uri(PageUrl), href).AbsoluteUri;
        var name = string.IsNullOrWhiteSpace(pendingName)
            ? Normalize(linkText).Replace(" TICKETS", "", StringComparison.OrdinalIgnoreCase)
            : pendingName;
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"A ticket link was found without a match name: {url}");
        if (matches.All(match => !match.Id.Equals(url, StringComparison.Ordinal)))
            matches.Add(new MatchInfo(url, name, pendingDate, pendingClosingForm, url, DateTimeOffset.UtcNow));
    }

    private static bool IsTicketLink(IElement link) => !string.IsNullOrWhiteSpace(link.GetAttribute("href")) &&
        (link.ClassList.Contains("button") || Normalize(link.TextContent).Contains("TICKETS", StringComparison.OrdinalIgnoreCase));
    private static bool IsEndOfAvailableMatches(string text) => text.StartsWith("*The match schedule", StringComparison.OrdinalIgnoreCase) || text.StartsWith("No changes, returns", StringComparison.OrdinalIgnoreCase);
    private static string? TryExtractMatchDate(string text) => text.Contains("closing", StringComparison.OrdinalIgnoreCase)
        ? null : text.Contains("date", StringComparison.OrdinalIgnoreCase) || text.Contains("kick-off", StringComparison.OrdinalIgnoreCase) || text.Contains("match schedule", StringComparison.OrdinalIgnoreCase) ? text : null;
    private static string? TryExtractClosingForm(string text)
    {
        const string prefix = "Closing form:";
        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? text[prefix.Length..].Trim()
            : null;
    }
    private static string Normalize(string value) => Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
}

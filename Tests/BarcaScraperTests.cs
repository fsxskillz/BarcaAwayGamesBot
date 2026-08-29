using System.Net;
using System.Text;
using BarcaAwayTickets.Scraping;
using Xunit;

namespace BarcaAwayTickets.Tests;

public sealed class BarcaScraperTests
{
    [Fact]
    public async Task ScrapeAsync_ParsesTicketLinksAndClosingForm()
    {
        const string html = """
            <p><strong>AVAILABLE MATCHES</strong></p>
            <p><strong>VALENCIA - FC BARCELONA*</strong></p>
            <p>Closing form: 25 August, 11.59 PM</p>
            <div><a class="button button--primary" href="/webapp/en/desplacaments/213/">VALENCIA - FC BARCELONA TICKETS</a></div>
            <p><strong>SEVILLA - FC BARCELONA</strong></p>
            <p>Date: 12 September 2026</p>
            <div><a class="button" href="https://tickets.example.test/214">SEVILLA TICKETS</a></div>
            <p>*The match schedule is subject to possible changes</p>
            <p><a class="button" href="https://should-not-be-read.example">Unrelated tickets</a></p>
            """;
        using var client = new HttpClient(new StubHandler(html));
        var matches = await new BarcaScraper(client).ScrapeAsync();

        Assert.Collection(matches,
            first =>
            {
                Assert.Equal("VALENCIA - FC BARCELONA", first.Name);
                Assert.Null(first.Date);
                Assert.Equal("25 August, 11.59 PM", first.ClosingForm);
                Assert.Equal("https://www.fcbarcelona.com/webapp/en/desplacaments/213/", first.Id);
            },
            second =>
            {
                Assert.Equal("SEVILLA - FC BARCELONA", second.Name);
                Assert.Equal("Date: 12 September 2026", second.Date);
                Assert.Equal("https://tickets.example.test/214", second.Id);
            });
    }

    [Fact]
    public async Task ScrapeAsync_ThrowsWhenAvailableMatchesMarkerIsMissing()
    {
        using var client = new HttpClient(new StubHandler("<p>Temporarily unavailable</p>"));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new BarcaScraper(client).ScrapeAsync());
        Assert.Contains("AVAILABLE MATCHES", exception.Message);
    }

    private sealed class StubHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html, Encoding.UTF8, "text/html") });
    }
}

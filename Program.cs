using BarcaAwayTickets.Configuration;
using BarcaAwayTickets.Notifications;
using BarcaAwayTickets.Scraping;
using BarcaAwayTickets.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var notificationOptions = new NotificationOptions
{
    Provider = configuration["Notification:Provider"] ?? "Telegram"
};

var services = new ServiceCollection();
services.AddSingleton(new HttpClient());
services.AddSingleton<IBarcaScraper, BarcaScraper>();
services.AddSingleton<IStateStore>(_ => new StateStore("state.json"));
services.AddSingleton<TelegramNotifier>();
services.AddSingleton<INotifierFactory, NotifierFactory>();

using var serviceProvider = services.BuildServiceProvider();

try
{
    var scraper = serviceProvider.GetRequiredService<IBarcaScraper>();
    var stateStore = serviceProvider.GetRequiredService<IStateStore>();
    var notifier = serviceProvider.GetRequiredService<INotifierFactory>().Create(notificationOptions);

    var currentMatches = await scraper.ScrapeAsync();
    Console.WriteLine($"Found {currentMatches.Count} available match(es).");

    var knownMatches = await stateStore.LoadAsync();
    var newMatches = stateStore.GetNewMatches(currentMatches, knownMatches);
    var updatedMatches = stateStore.MergeMatches(currentMatches, knownMatches);
    var stateChanged = !knownMatches.SequenceEqual(updatedMatches);
    if (newMatches.Count == 0)
    {
        if (stateChanged)
        {
            await stateStore.SaveAsync(updatedMatches);
            Console.WriteLine("No new match. Existing match details were refreshed in state.json.");
        }
        else
            Console.WriteLine("No new match. State file is unchanged.");
        return;
    }

    Console.WriteLine($"{newMatches.Count} new match(es) detected.");
    foreach (var match in newMatches)
        await notifier.NotifyAsync(match);

    await stateStore.SaveAsync(updatedMatches);
    Console.WriteLine("Notifications sent and state.json updated.");
}
catch (Exception exception)
{
    Console.Error.WriteLine($"ERROR: {exception.Message}");
    Environment.ExitCode = 1;
}

# Barça Away Tickets monitor

Small .NET 10 scraper that checks the official FC Barcelona away-ticket page once per day and sends a notification only for a newly seen ticket URL.

## HTML inspected on 29 August 2026

The official page currently represents the relevant content as adjacent elements inside the article body:

```html
<p><strong>AVAILABLE MATCHES</strong></p>
<p><strong>VALENCIA- FC BARCELONA*</strong></p>
<p>Closing form: 25 August, 11.59 PM</p>
<div class="articleWidget center">
  <div class="embeddable-button">
    <a href="https://taquilla.fcbarcelona.cat/webapp/en/desplacaments/213/"
       class="button button--primary">VALENCIA– FC BARCELONA TICKETS</a>
  </div>
</div>
<p>*The match schedule is subject to possible changes</p>
```

The scraper starts at the exact `AVAILABLE MATCHES` marker and stops at the schedule footnote. It uses the ticket URL (for example `/desplacaments/213/`) as the stable ID, so reordering and cosmetic HTML changes do not generate duplicates. The current page exposes an application closing deadline, not the fixture date; it is stored separately as `closingForm` and included in Telegram alerts.

## Project tree

```text
.
├── .github/workflows/monitor.yml
├── .gitignore
├── BarcaAwayTickets.csproj
├── Configuration/NotificationOptions.cs
├── Models/MatchInfo.cs
├── Notifications/
│   ├── INotifier.cs
│   ├── INotifierFactory.cs
│   ├── NotifierFactory.cs
│   └── TelegramNotifier.cs
├── Program.cs
├── README.md
├── Scraping/
│   ├── BarcaScraper.cs
│   └── IBarcaScraper.cs
├── State/
│   ├── IStateStore.cs
│   └── StateStore.cs
├── Tests/
│   ├── BarcaAwayTickets.Tests.csproj
│   ├── BarcaScraperTests.cs
│   └── StateStoreTests.cs
├── appsettings.json
└── state.json
```

## Responsibilities

- `Program.cs` composes the small application, runs the scrape/compare/notify/save flow, and contains no parsing, persistence, or provider-specific notification code.
- `Models/MatchInfo.cs` is the fixture data model; its ticket URL is the stable ID.
- `Scraping/BarcaScraper.cs` owns the FC Barcelona HTTP request, timeout, HTML parsing, and extraction safeguards. `IBarcaScraper.cs` makes it replaceable and testable.
- `State/StateStore.cs` reads and writes `state.json`, and compares fixture IDs. `IStateStore.cs` is the persistence boundary.
- `Notifications/TelegramNotifier.cs` sends Telegram messages and reads its token and chat ID only from environment variables. `INotifier.cs` is the provider-independent application boundary.
- `Notifications/NotifierFactory.cs` selects the configured notifier case-insensitively. WhatsApp is recognized but deliberately fails with a clear message until its notifier is implemented.
- `Configuration/NotificationOptions.cs` holds only non-sensitive configuration.
- `Tests/` contains isolated tests for the live-page parsing pattern and stable-ID state comparison.

## Notification provider configuration

`appsettings.json` contains only the selected provider, never secrets:

```json
{
  "Notification": {
    "Provider": "Telegram"
  }
}
```

Provider names are case-insensitive. To prepare a future provider, change only this setting:

```json
{
  "Notification": {
    "Provider": "WhatsApp"
  }
}
```

`WhatsApp` is intentionally not implemented yet, so that configuration currently produces a clear error rather than silently falling back to Telegram. Adding `WhatsAppNotifier : INotifier` and registering it in `NotifierFactory` will not require changing the application flow.

## Local run

```bash
export TELEGRAM_BOT_TOKEN="123456:ABC..."
export TELEGRAM_CHAT_ID="123456789"
dotnet run --configuration Release
```

Run the tests with:

```bash
dotnet test Tests/BarcaAwayTickets.Tests.csproj --configuration Release
```

The first successful execution considers the current available ticket URLs new and sends an alert for each. Afterwards, `state.json` is only changed when a notification has been sent successfully. If page retrieval or parsing fails, the program exits with an error, sends no notification, and does not alter the state.

## Create the Telegram bot

1. In Telegram, open **@BotFather** and send `/newbot`.
2. Follow the prompts for its display name and username (the username must end in `bot`).
3. Copy the token supplied by BotFather: it is `TELEGRAM_BOT_TOKEN`.
4. Open a conversation with the new bot and send it `/start`.
5. In a browser, visit `https://api.telegram.org/bot<TOKEN>/getUpdates` after replacing `<TOKEN>`. In the returned JSON, copy `message.chat.id`; that integer is `TELEGRAM_CHAT_ID`. For a group, add the bot to the group, post a message in it, then use that group's `chat.id` (often negative).

Never commit the bot token or chat ID to this repository.

## Configure GitHub Secrets

1. On GitHub, open the repository.
2. Go to **Settings** → **Secrets and variables** → **Actions**.
3. Click **New repository secret**.
4. Create `TELEGRAM_BOT_TOKEN` with the BotFather token as its value.
5. Create `TELEGRAM_CHAT_ID` with the value obtained from `getUpdates`.

## First push and manual test

1. Create an empty GitHub repository and add it as `origin`:

   ```bash
   git init
   git add .
   git commit -m "Initial Barça away ticket monitor"
   git branch -M main
   git remote add origin https://github.com/<YOUR_ACCOUNT>/<YOUR_REPOSITORY>.git
   git push -u origin main
   ```

2. Configure the two repository secrets above.
3. In the GitHub repository, open **Actions** → **Monitor Barça away tickets** → **Run workflow**, choose `main`, then click **Run workflow**.
4. Open the resulting run. The first run should send alerts for the ticket URLs currently present and commit the populated `state.json`.
5. Run it a second time: it should log `No new match` and create no commit or Telegram message.

The scheduled cron is 07:17 UTC daily. Scheduled workflows run from the default branch, so keep `monitor.yml` and `state.json` on that branch.

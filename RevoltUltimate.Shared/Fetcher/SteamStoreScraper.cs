using HtmlAgilityPack;
using RevoltUltimate.API.Objects;
using System.Net.Http;

namespace RevoltUltimate.API.Fetcher
{
    public class SteamStoreScraper
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<Game> ScrapeGameDataAsync(int appId)
        {
            try
            {
                var storeUrl = $"https://store.steampowered.com/app/{appId}/";
                var storePageHtml = await _httpClient.GetStringAsync(storeUrl);
                var storeDoc = new HtmlDocument();
                storeDoc.LoadHtml(storePageHtml);

                var gameName = storeDoc.DocumentNode.SelectSingleNode("//div[@id='appHubAppName']")?.InnerText.Trim() ?? $"Unknown Game {appId}";
                var gameDescription = storeDoc.DocumentNode.SelectSingleNode("//div[@class='game_description_snippet']")?.InnerText.Trim() ?? "";

                var achievements = await ScrapeAchievementsAsync(appId);

                return new Game(gameName, "PC", null, gameDescription, "Emulator", achievements, appId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to scrape game data for AppID {appId}: {ex.Message}");
                return null;
            }
        }

        private async Task<List<Achievement>> ScrapeAchievementsAsync(int appId)
        {
            var achievements = new List<Achievement>();
            try
            {
                int count = 0;
                var achievementsUrl = $"https://steamcommunity.com/stats/{appId}/achievements/";
                var achievementsPageHtml = await _httpClient.GetStringAsync(achievementsUrl);
                var achievementsDoc = new HtmlDocument();
                achievementsDoc.LoadHtml(achievementsPageHtml);

                var achievementNodes = achievementsDoc.DocumentNode.SelectNodes("//div[contains(@class, 'achieveRow')]");
                if (achievementNodes == null)
                {
                    Console.WriteLine($"No achievement rows found for AppID {appId}. The page layout may have changed.");
                    return achievements;
                }

                foreach (var node in achievementNodes)
                {
                    var nameNode = node.SelectSingleNode(".//h3");
                    var descriptionNode = node.SelectSingleNode(".//h5");
                    var iconNode = node.SelectSingleNode(".//img");
                    var globalPercentNode = node.SelectSingleNode(".//div[contains(@class, 'achievePercent')]");

                    if (nameNode != null && descriptionNode != null && iconNode != null)
                    {
                        var name = nameNode.InnerText.Trim();
                        if (string.IsNullOrEmpty(name)) continue; // Skip if there's no name

                        var description = descriptionNode.InnerText.Trim();
                        var iconUrl = iconNode.GetAttributeValue("src", "");

                        float.TryParse(globalPercentNode?.InnerText.Trim().Replace("%", ""), out var globalPercent);

                        achievements.Add(new Achievement(
                            Name: name,
                            Description: description,
                            ImageUrl: iconUrl,
                            Hidden: false,
                            Id: 0,
                            Unlocked: false,
                            DateTimeUnlocked: null,
                            Difficulty: 0,
                            apiName: count.ToString(),
                            progress: false,
                            currentProgress: 0,
                            maxProgress: 0,
                            getglobalpercentage: globalPercent
                        ));
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to scrape achievements for AppID {appId}: {ex.Message}");
            }
            return achievements;
        }
    }
}

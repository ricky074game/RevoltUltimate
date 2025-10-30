using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Objects;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace RevoltUltimate.API.Searcher
{
    public class SteamScrape
    {
        private static SteamScrape _instance;
        public static SteamScrape Instance => _instance ??= new SteamScrape();

        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private bool _isLoggedIn;
        private string _steamId64;
        private string _username;

        public Func<Tuple<string, string>> ShowLoginWindow { get; set; }

        public SteamScrape()
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = _cookieContainer };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<bool> TryRefreshSessionAsync()
        {
            if (!_isLoggedIn || string.IsNullOrEmpty(_username))
            {
                System.Diagnostics.Debug.WriteLine("SteamScrape: Cannot refresh session, user not logged in.");
                return false;
            }

            try
            {
                var response = await _httpClient.GetAsync("https://steamcommunity.com/my/home");
                response.EnsureSuccessStatusCode();

                if (response.RequestMessage.RequestUri.ToString().Contains("login"))
                {
                    System.Diagnostics.Debug.WriteLine("SteamScrape: Session is invalid, clearing credentials.");
                    Disconnect();
                    return false;
                }

                var cookies = _cookieContainer.GetCookies(new Uri("https://steamcommunity.com")).Cast<Cookie>();
                var newSessionId = cookies.FirstOrDefault(c => c.Name == "sessionid")?.Value;
                var newSteamLoginSecure = cookies.FirstOrDefault(c => c.Name == "steamLoginSecure")?.Value;

                if (!string.IsNullOrEmpty(newSessionId) && !string.IsNullOrEmpty(newSteamLoginSecure))
                {
                    AccountManager.SaveSteamSession(_username, newSessionId, newSteamLoginSecure);
                    System.Diagnostics.Debug.WriteLine("SteamScrape: Session refreshed and saved successfully.");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SteamScrape: Error refreshing session: {ex.Message}");
                Disconnect();
                return false;
            }
        }



        public void SetSessionCookies(string steamLoginSecure, string sessionId, string username)
        {
            if (string.IsNullOrEmpty(steamLoginSecure) || string.IsNullOrEmpty(sessionId))
            {
                _isLoggedIn = false;
                return;
            }

            _cookieContainer.Add(new Cookie("steamLoginSecure", steamLoginSecure, "/", ".steamcommunity.com"));
            _cookieContainer.Add(new Cookie("sessionid", sessionId, "/", ".steamcommunity.com"));
            _username = username;
            _isLoggedIn = true;
        }

        private async Task<bool> FetchAndSetProfileInfoAsync()
        {
            if (!_isLoggedIn)
            {
                System.Diagnostics.Debug.WriteLine("SteamScrape: Cannot get profile info, user is not logged in.");
                return false;
            }

            try
            {
                var response = await _httpClient.GetStringAsync("https://steamcommunity.com/my/");

                var steamIdMatch = Regex.Match(response, @"g_steamID\s*=\s*""(\d+)""");
                if (steamIdMatch.Success)
                {
                    _steamId64 = steamIdMatch.Groups[1].Value;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SteamScrape: Could not find SteamID on profile page.");
                    return false;
                }

                var personaNameMatch = Regex.Match(response, @"""personaname""\s*:\s*""(.+?)""");
                if (personaNameMatch.Success)
                {
                    _username = personaNameMatch.Groups[1].Value;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SteamScrape: Could not find Persona Name on profile page.");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"SteamScrape: Found SteamID: {_steamId64}, Username: {_username}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SteamScrape: Error fetching profile page to get info: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Game>> GetOwnedGamesAsync()
        {
            if (string.IsNullOrEmpty(_steamId64) && !(await FetchAndSetProfileInfoAsync().ConfigureAwait(false)))
            {
                System.Diagnostics.Debug.WriteLine("SteamScrape: Not logged in or profile info not found, cannot get owned games.");
                return new List<Game>();
            }

            var games = new List<Game>();
            var url = $"https://steamcommunity.com/profiles/{_steamId64}/games/?tab=all";

            try
            {
                var response = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var gamesListNode = doc.DocumentNode.SelectSingleNode("//template[@id='gameslist_config']");
                var jsonData = gamesListNode?.GetAttributeValue("data-profile-gameslist", null);

                if (!string.IsNullOrEmpty(jsonData))
                {
                    var decodedJsonData = WebUtility.HtmlDecode(jsonData);
                    var gamesListJson = JObject.Parse(decodedJsonData);
                    var ownedGames = gamesListJson["rgGames"];

                    if (ownedGames is JArray gamesArray)
                    {
                        foreach (var gameToken in gamesArray)
                        {
                            var name = gameToken["name"]?.ToString();
                            var appId = gameToken["appid"]?.ToString();
                            var iconHash = gameToken["img_icon_url"]?.ToString();
                            var logoUrl = !string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(iconHash)
                                ? $"https://cdn.akamai.steamstatic.com/steamcommunity/public/images/apps/{appId}/{iconHash}.jpg"
                                : null;

                            if (!string.IsNullOrEmpty(name))
                            {
                                var game = new Game(name, "Steam", logoUrl, $"Steam App {appId}", "Steam Local", int.Parse(appId));
                                games.Add(game);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine($"SteamScrape: Successfully parsed {games.Count} games from profile page.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SteamScrape: 'rgGames' JSON array not found or is not an array.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SteamScrape: Could not find gameslist_config JSON on the games page.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SteamScrape: Error fetching or parsing owned games: {ex.Message}");
            }
            
            // Fetch achievements for all games in parallel
            var achievementTasks = games.Select(async game =>
            {
                var appIdMatch = Regex.Match(game.description, @"\d+");
                if (appIdMatch.Success && uint.TryParse(appIdMatch.Value, out uint appId))
                {
                    var achievements = await GetPlayerAchievementsAsync(appId).ConfigureAwait(false);
                    game.AddAchievements(achievements);
                }
                return game;
            }).ToList();
            
            await Task.WhenAll(achievementTasks).ConfigureAwait(false);
            
            return games;
        }

        public async Task<List<Achievement>> GetPlayerAchievementsAsync(uint appId)
        {
            if (string.IsNullOrEmpty(_steamId64) && !(await FetchAndSetProfileInfoAsync()))
            {
                System.Diagnostics.Debug.WriteLine("SteamScrape: Not logged in or profile info not found, cannot get achievements.");
                return new List<Achievement>();
            }

            var achievements = new List<Achievement>();
            var url = $"https://steamcommunity.com/profiles/{_steamId64}/stats/{appId}/?tab=achievements";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var achievementNodes = doc.DocumentNode.SelectNodes("//div[@class='achieveRow']");
                if (achievementNodes == null)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamScrape: No achievement rows found for AppID {appId}. The game might not have achievements or the profile is private.");
                    return achievements;
                }

                int id = 0;
                foreach (var node in achievementNodes)
                {
                    var name = node.SelectSingleNode(".//h3")?.InnerText.Trim();
                    var description = node.SelectSingleNode(".//h5")?.InnerText.Trim();
                    var imageUrl = node.SelectSingleNode(".//img")?.GetAttributeValue("src", "");
                    var unlocked = node.SelectSingleNode(".//div[@class='achieveUnlockTime']") != null;
                    var unlockTimeNode = node.SelectSingleNode(".//div[@class='achieveUnlockTime']");
                    var unlockedTime = unlockTimeNode != null ? unlockTimeNode.InnerText.Trim() : string.Empty;

                    var progressNode = node.SelectSingleNode(".//div[contains(@class, 'achievementProgressBar')]");
                    bool hasProgress = progressNode != null;
                    int currentProgress = 0;
                    int maxProgress = 0;

                    if (hasProgress)
                    {
                        var progressTextNode = progressNode.SelectSingleNode(".//div[contains(@class, 'progressText')]");
                        if (progressTextNode != null)
                        {
                            var progressText = progressTextNode.InnerText.Trim().Replace(",", "");
                            var progressParts = progressText.Split('/');
                            if (progressParts.Length == 2)
                            {
                                int.TryParse(progressParts[0].Trim(), out currentProgress);
                                int.TryParse(progressParts[1].Trim(), out maxProgress);
                            }
                        }
                    }

                    // Scrape global percentage
                    var globalPercentageNode = node.SelectSingleNode(".//div[contains(@class, 'achievePercent')]");
                    float globalPercentage = 0;
                    if (globalPercentageNode != null)
                    {
                        var percentageText = globalPercentageNode.InnerText.Trim().Replace("%", "");
                        float.TryParse(percentageText, out globalPercentage);
                    }

                    bool hidden = description.Contains("revealed once unlocked");
                    if (!string.IsNullOrEmpty(name))
                    {
                        var achievement = new Achievement(
                            Name: name,
                            Description: description,
                            ImageUrl: imageUrl,
                            Hidden: hidden,
                            Id: id++,
                            Unlocked: unlocked,
                            DateTimeUnlocked: unlockedTime,
                            Difficulty: 1,
                            apiName: "", // Not available via scraping
                            progress: hasProgress,
                            currentProgress: currentProgress,
                            maxProgress: maxProgress,
                            getglobalpercentage: globalPercentage
                        );
                        achievements.Add(achievement);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"SteamScrape: Successfully parsed {achievements.Count} achievements for AppID {appId}.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SteamScrape: Error fetching or parsing achievements for AppID {appId}: {ex.Message}");
            }

            return achievements;
        }

        public void Disconnect()
        {
            if (!string.IsNullOrEmpty(_username))
            {
                AccountManager.ClearSteamSession(_username);
            }

            _isLoggedIn = false;
            _steamId64 = null;
            _username = null;
            _cookieContainer.Add(new Cookie("steamLoginSecure", DateTime.Now.AddDays(-1).ToString(), "/", ".steamcommunity.com"));
            _cookieContainer.Add(new Cookie("sessionid", DateTime.Now.AddDays(-1).ToString(), "/", ".steamcommunity.com"));
            System.Diagnostics.Debug.WriteLine("SteamScrape: User logged out, cookies cleared and stored session deleted.");
        }
    }
}
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RevoltUltimate.API.Accounts;
using RevoltUltimate.API.Objects;
using System.Diagnostics;
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
        public SteamScrape()
        {
            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };
            _httpClient = new HttpClient(handler);
        }

        public async Task<bool> TryRefreshSessionAsync()
        {
            if (!_isLoggedIn || string.IsNullOrEmpty(_username))
            {
                Trace.WriteLine("SteamScrape: Cannot refresh session, user not logged in.");
                return false;
            }

            try
            {
                var communityResponse = await _httpClient.GetAsync("https://steamcommunity.com/my/home");
                communityResponse.EnsureSuccessStatusCode();

                if (communityResponse.RequestMessage.RequestUri.ToString().Contains("login"))
                {
                    Trace.WriteLine("SteamScrape: Community session is invalid, clearing credentials.");
                    Disconnect();
                    return false;
                }
                var communityUri = new Uri("https://steamcommunity.com/");
                var storeUri = new Uri("https://store.steampowered.com/");

                var allCookies = new List<Cookie>();
                allCookies.AddRange(_cookieContainer.GetCookies(communityUri).Cast<Cookie>());
                allCookies.AddRange(_cookieContainer.GetCookies(storeUri).Cast<Cookie>());

                var distinctCookies = allCookies
                    .GroupBy(c => new { c.Name, c.Domain })
                    .Select(g => g.First())
                    .ToList();

                if (distinctCookies.Any(c => c.Name == "steamLoginSecure"))
                {
                    var serializableCookies = distinctCookies.Select(c => new SerializableCookie
                    {
                        Name = c.Name,
                        Value = c.Value,
                        Domain = c.Domain,
                        Path = c.Path
                    }).ToList();

                    AccountManager.SaveSteamAccount(_username, serializableCookies);
                    Trace.WriteLine("SteamScrape: Session refreshed and all cookies saved successfully.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"SteamScrape: Error refreshing session: {ex.Message}");
                Disconnect();
                return false;
            }
        }


        public void SetSessionCookies(List<SerializableCookie> cookies, string username)
        {
            if (cookies == null || !cookies.Any())
            {
                _isLoggedIn = false;
                return;
            }

            foreach (var cookie in cookies)
            {
                try
                {
                    _cookieContainer.Add(new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
                }
                catch (CookieException ex)
                {
                    Trace.WriteLine($"Skipping invalid cookie '{cookie.Name}' during session load: {ex.Message}");
                }
            }

            _username = username;
            _isLoggedIn = true;
        }

        private async Task<bool> FetchAndSetProfileInfoAsync()
        {

            if (!_isLoggedIn)
            {
                Trace.WriteLine("SteamScrape: Cannot get profile info, user is not logged in.");
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
                    Trace.WriteLine("SteamScrape: Could not find SteamID on profile page.");
                    return false;
                }

                var personaNameMatch = Regex.Match(response, @"""personaname""\s*:\s*""(.+?)""");
                if (personaNameMatch.Success)
                {
                    _username = personaNameMatch.Groups[1].Value;
                }
                else
                {
                    Trace.WriteLine("SteamScrape: Could not find Persona Name on profile page.");
                    return false;
                }

                Trace.WriteLine($"SteamScrape: Found SteamID: {_steamId64}, Username: {_username}");
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"SteamScrape: Error fetching profile page to get info: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Game>> GetOwnedGamesAsync()
        {
            if (string.IsNullOrEmpty(_steamId64) && !(await FetchAndSetProfileInfoAsync()))
            {
                Trace.WriteLine("SteamScrape: Not logged in or profile info not found, cannot get owned games.");
                return new List<Game>();
            }

            var games = new List<Game>();
            var url = $"https://steamcommunity.com/profiles/{_steamId64}/games/?tab=all";

            try
            {
                var response = await _httpClient.GetStringAsync(url);

                string searchMarker = "window.SSR.renderContext=JSON.parse(\"";
                int startIndex = response.IndexOf(searchMarker);
                if (startIndex != -1)
                {
                    startIndex += searchMarker.Length;
                    int endIndex = response.IndexOf("\");", startIndex);
                    if (endIndex != -1)
                    {
                        var jsonString = response.Substring(startIndex, endIndex - startIndex);
                        jsonString = Regex.Unescape(jsonString);
                        var renderContext = JObject.Parse(jsonString);
                        var queryDataString = renderContext["queryData"]?.ToString();
                        if (!string.IsNullOrEmpty(queryDataString))
                        {
                            var queryDataObject = JObject.Parse(queryDataString);
                            var queries = queryDataObject["queries"];

                            if (queries is JArray queriesArray)
                            {
                                var ownedGamesQuery = queriesArray.FirstOrDefault(q =>
                                    q["queryKey"] is JArray key &&
                                    key.Count > 0 &&
                                    key[0].ToString() == "OwnedGames");

                                if (ownedGamesQuery != null)
                                {
                                    var gamesList = ownedGamesQuery["state"]?["data"];
                                    if (gamesList is JArray gamesArray)
                                    {
                                        foreach (var gameToken in gamesArray)
                                        {
                                            var name = gameToken["name"]?.ToString();
                                            var appId = gameToken["appid"]?.ToString();
                                            var iconHash = gameToken["img_icon_url"]?.ToString();

                                            var logoUrl = !string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(iconHash)
                                                ? $"https://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{iconHash}.jpg"
                                                : null;

                                            if (!string.IsNullOrEmpty(name) && int.TryParse(appId, out int appIdInt))
                                            {
                                                var game = new Game(name, "Steam", logoUrl, $"Steam App {appId}", "Steam Local", appIdInt);
                                                games.Add(game);
                                            }
                                        }
                                        Trace.WriteLine($"SteamScrape: Successfully parsed {games.Count} games from SSR data.");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Trace.WriteLine("SteamScrape: Could not find window.SSR.renderContext in page source.");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"SteamScrape: Error fetching or parsing owned games: {ex.Message}");
            }

            foreach (var game in games)
            {
                var match = Regex.Match(game.description, @"\d+");
                if (match.Success)
                {
                    var achievements = await GetPlayerAchievementsAsync(uint.Parse(match.Value));
                    game.AddAchievements(achievements);
                }
            }

            return games;
        }

        public async Task<List<Achievement>> GetPlayerAchievementsAsync(uint appId)
        {
            if (string.IsNullOrEmpty(_steamId64) && !(await FetchAndSetProfileInfoAsync()))
            {
                Trace.WriteLine("SteamScrape: Not logged in or profile info not found, cannot get achievements.");
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
                    Trace.WriteLine($"SteamScrape: No achievement rows found for AppID {appId}. The game might not have achievements or the profile is private.");
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
                            Difficulty: 0,
                            apiName: "",
                            progress: hasProgress,
                            currentProgress: currentProgress,
                            maxProgress: maxProgress,
                            getglobalpercentage: globalPercentage
                        );
                        achievements.Add(achievement);
                    }
                }
                Trace.WriteLine($"SteamScrape: Successfully parsed {achievements.Count} achievements for AppID {appId}.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"SteamScrape: Error fetching or parsing achievements for AppID {appId}: {ex.Message}");
            }

            return achievements;
        }

        public void Disconnect()
        {
            if (!string.IsNullOrEmpty(_username))
            {
                AccountManager.DeleteAccount(_username);
            }

            _isLoggedIn = false;
            _steamId64 = null;
            _username = null;

            const string steamCommunityDomain = ".steamcommunity.com";
            const string steamStoreDomain = ".store.steampowered.com";
            var expiredCookieDate = DateTime.Now.AddDays(-1);

            _cookieContainer.Add(new Cookie("steamLoginSecure", "") { Domain = steamCommunityDomain, Expires = expiredCookieDate });
            _cookieContainer.Add(new Cookie("sessionid", "") { Domain = steamCommunityDomain, Expires = expiredCookieDate });
            _cookieContainer.Add(new Cookie("steamLoginSecure", "") { Domain = steamStoreDomain, Expires = expiredCookieDate });
            _cookieContainer.Add(new Cookie("sessionid", "") { Domain = steamStoreDomain, Expires = expiredCookieDate });

            Trace.WriteLine("SteamScrape: User logged out, cookies cleared and stored session deleted.");
        }
    }
}
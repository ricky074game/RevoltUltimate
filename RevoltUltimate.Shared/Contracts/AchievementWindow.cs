using RevoltUltimate.API.Objects;
using System.IO;
using System.Reflection;

namespace RevoltUltimate.API.Contracts
{
    public class AchievementWindow
    {
        private static IAchievementNotifier? _cachedNotifier;
        private static string? _lastLoadedDllPath;

        public static void ShowNotification(Achievement achievement, string? customNotifierDllPath)
        {
            if (achievement == null)
            {
                Console.WriteLine("Achievement object cannot be null.");
                return;
            }

            if (string.IsNullOrEmpty(customNotifierDllPath))
            {
                Console.WriteLine("Custom achievement notifier DLL path is not configured in settings.");
                return;
            }

            try
            {
                IAchievementNotifier notifier;

                if (_cachedNotifier != null && _lastLoadedDllPath == customNotifierDllPath)
                {
                    notifier = _cachedNotifier;
                }
                else
                {
                    if (!File.Exists(customNotifierDllPath))
                    {
                        _cachedNotifier = null;
                        _lastLoadedDllPath = null;
                        return;
                    }

                    Assembly notifierAssembly = Assembly.LoadFrom(customNotifierDllPath);
                    Type? notifierType = notifierAssembly.GetTypes()
                                                         .FirstOrDefault(t => typeof(IAchievementNotifier).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    if (notifierType == null)
                    {
                        _cachedNotifier = null;
                        _lastLoadedDllPath = null;
                        return;
                    }

                    notifier = (IAchievementNotifier)Activator.CreateInstance(notifierType);
                    _cachedNotifier = notifier;
                    _lastLoadedDllPath = customNotifierDllPath;
                }
                notifier.ShowAchievement(achievement);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing achievement notification via DLL: {ex}");
                _cachedNotifier = null;
                _lastLoadedDllPath = null;
            }
        }
    }
}

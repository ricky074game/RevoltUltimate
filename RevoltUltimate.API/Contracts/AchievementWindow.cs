using RevoltUltimate.API.Objects;
using System.Diagnostics;
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
            if (string.IsNullOrEmpty(customNotifierDllPath))
            {
                Trace.WriteLine("Custom achievement notifier DLL path is not configured in settings.");
                return;
            }

            try
            {
                IAchievementNotifier notifier;

                string absoluteDllPath = Path.IsPathRooted(customNotifierDllPath)
                    ? customNotifierDllPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, customNotifierDllPath);

                if (_cachedNotifier != null && _lastLoadedDllPath == absoluteDllPath)
                {
                    notifier = _cachedNotifier;
                }
                else
                {
                    if (!File.Exists(absoluteDllPath))
                    {
                        Trace.WriteLine($"Could not find Mod Entry DLL at: {absoluteDllPath}");
                        _cachedNotifier = null;
                        _lastLoadedDllPath = null;
                        return;
                    }

                    Assembly notifierAssembly = Assembly.LoadFrom(absoluteDllPath);
                    Type? notifierType = notifierAssembly.GetTypes()
                        .FirstOrDefault(t => typeof(IAchievementNotifier).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    if (notifierType == null)
                    {
                        Trace.WriteLine($"The DLL at {absoluteDllPath} does not contain an IAchievementNotifier implementation.");
                        _cachedNotifier = null;
                        _lastLoadedDllPath = null;
                        return;
                    }

                    notifier = (IAchievementNotifier)Activator.CreateInstance(notifierType);
                    _cachedNotifier = notifier;
                    _lastLoadedDllPath = absoluteDllPath;
                }

                notifier.ShowAchievement(achievement);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error showing achievement notification via DLL: {ex}");
                _cachedNotifier = null;
                _lastLoadedDllPath = null;
            }
        }
    }
}
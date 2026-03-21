using Microsoft.Win32;
using System.Diagnostics;

namespace RevoltUltimate.Desktop
{
    public static class FileAssociationHelper
    {
        public static void EnsureRevoltAssociation()
        {
            try
            {
                string extension = ".revolt";
                string progId = "RevoltUltimate.RevoltFile";
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                if (string.IsNullOrEmpty(exePath)) return;

                using var extKey = Registry.ClassesRoot.CreateSubKey(extension);
                extKey.SetValue("", progId);

                using var progKey = Registry.ClassesRoot.CreateSubKey(progId);
                progKey.SetValue("", "Revolt Ultimate Custom Theme");

                using var iconKey = progKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"\"{exePath}\",0");

                using var cmdKey = progKey.CreateSubKey(@"shell\open\command");
                // Launch app bypassing arguments to %1
                cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
            catch
            {
                // If we fail to set the registry keys, we can silently ignore it. The app will still work, just without file association.
            }
        }
    }
}
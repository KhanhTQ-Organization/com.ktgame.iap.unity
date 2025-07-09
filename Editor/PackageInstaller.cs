using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;

namespace com.ktgame.iap.unity.editor
{
    internal static class PackageInstaller
    {
        private const string PackageName = "com.ktgame.iap.unity";

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Events.registeringPackages += RegisteringPackagesEventHandler;
            Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        private static void RegisteringPackagesEventHandler(PackageRegistrationEventArgs args)
        {
            var removedPackage = args.removed.FirstOrDefault(package => package.name.Equals(PackageName));
            if (removedPackage != null) { }
        }

        private static void RegisteredPackagesEventHandler(PackageRegistrationEventArgs args)
        {
            var addedPackage = args.added.FirstOrDefault(package => package.name.Equals(PackageName));
            if (addedPackage != null) { }
        }

        private static void AddScriptingDefineSymbol(string define)
        {
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var allDefines = definesString.Split(';').ToList();
            if (!allDefines.Contains(define))
            {
                allDefines.Add(define);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
            }
        }

        private static void RemoveScriptingDefineSymbol(string define)
        {
            var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var allDefines = definesString.Split(';').ToList();
            if (allDefines.Contains(define))
            {
                allDefines.Remove(define);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
            }
        }
    }
}
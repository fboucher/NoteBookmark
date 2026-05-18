using System.Reflection;

namespace NoteBookmark.BlazorApp.Components.Shared
{
    public static class AppVersion
    {
        public static string Display { get; } = GetVersion();

        private static string GetVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            if (asm is null)
                return "v0.0.0";

            var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
                return informational;

            var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrEmpty(file))
                return file;

            var nameVer = asm.GetName().Version?.ToString();
            if (!string.IsNullOrEmpty(nameVer))
                return nameVer;

            return "v0.0.0";
        }
    }
}

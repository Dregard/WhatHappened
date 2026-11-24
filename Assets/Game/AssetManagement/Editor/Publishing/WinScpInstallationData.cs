namespace Game.AssetManagement.Publishing
{
    /// <summary>A WinSCP .NET assembly together with the WinSCP.exe that will be launched for it.</summary>
    public sealed class WinScpInstallationData
    {
        public string AssemblyPath { get; private set; }
        public string ExecutablePath { get; private set; }
        public string AssemblyVersion { get; private set; }
        public string ExecutableVersion { get; private set; }

        public bool VersionsMatch
        {
            get { return AssemblyVersion == ExecutableVersion; }
        }

        /// <summary>Creates a pair description; versions are file versions read from disk.</summary>
        public WinScpInstallationData(string assemblyPath, string executablePath, string assemblyVersion, string executableVersion)
        {
            AssemblyPath = assemblyPath;
            ExecutablePath = executablePath;
            AssemblyVersion = assemblyVersion ?? string.Empty;
            ExecutableVersion = executableVersion ?? string.Empty;
        }
    }
}

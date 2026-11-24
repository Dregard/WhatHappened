using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Finds a WinSCPnet.dll and a WinSCP.exe of the same version, so the .NET assembly never talks to a different executable.</summary>
    public static class WinScpInstallationLocator
    {
        private const string AssemblyFileName = "WinSCPnet.dll";
        private const string ExecutableFileName = "WinSCP.exe";

        /// <summary>
        /// Looks in the configured folder first, then in the standard install folders. Prefers a pair with equal versions,
        /// pairing a dll with an exe from another folder if needed; otherwise returns the first dll with its own folder's exe
        /// (versions differ, the caller decides how to proceed). Throws a readable error when nothing usable exists.
        /// </summary>
        public static WinScpInstallationData Locate(string configuredFolder)
        {
            var foldersList = GetCandidateFolders(configuredFolder).Where(Directory.Exists).ToList();
            var assembliesList = foldersList.Select(folder => Path.Combine(folder, AssemblyFileName)).Where(File.Exists).ToList();
            var executablesList = foldersList.Select(folder => Path.Combine(folder, ExecutableFileName)).Where(File.Exists).ToList();
            if (assembliesList.Count == 0 || executablesList.Count == 0)
            {
                throw new RemoteContentPublishException(PublishStageAlias.Configuration,
                    "WinSCP не найден (нужны WinSCPnet.dll и WinSCP.exe). Установите WinSCP с компонентом .NET assembly или укажите его папку в настройках Remote Content Publishing.");
            }
            foreach (var assemblyPath in assembliesList)
            {
                var assemblyVersion = ReadVersion(assemblyPath);
                var sameFolderExecutable = Path.Combine(Path.GetDirectoryName(assemblyPath), ExecutableFileName);
                var match = File.Exists(sameFolderExecutable) && ReadVersion(sameFolderExecutable) == assemblyVersion
                    ? sameFolderExecutable
                    : executablesList.FirstOrDefault(executable => ReadVersion(executable) == assemblyVersion);
                if (match != null)
                {
                    return new WinScpInstallationData(assemblyPath, match, assemblyVersion, assemblyVersion);
                }
            }
            var firstAssembly = assembliesList[0];
            var fallbackExecutable = File.Exists(Path.Combine(Path.GetDirectoryName(firstAssembly), ExecutableFileName))
                ? Path.Combine(Path.GetDirectoryName(firstAssembly), ExecutableFileName) : executablesList[0];
            return new WinScpInstallationData(firstAssembly, fallbackExecutable, ReadVersion(firstAssembly), ReadVersion(fallbackExecutable));
        }

        /// <summary>Returns the configured folder followed by the standard WinSCP install folders.</summary>
        private static IEnumerable<string> GetCandidateFolders(string configuredFolder)
        {
            if (!string.IsNullOrWhiteSpace(configuredFolder))
            {
                yield return configuredFolder;
            }
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WinSCP");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WinSCP");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WinSCP");
        }

        /// <summary>Reads the file version (major.minor.build) of a binary; an unreadable version yields an empty string.</summary>
        private static string ReadVersion(string path)
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            return info.FileMajorPart + "." + info.FileMinorPart + "." + info.FileBuildPart;
        }
    }
}

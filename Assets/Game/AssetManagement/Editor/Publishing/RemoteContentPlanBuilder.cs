using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Builds the ordered upload plan from the current build's own file registry. Files that only exist on disk from earlier builds
    /// are never included. Subfolders below each profile build path are preserved on the server.
    /// </summary>
    public sealed class RemoteContentPlanBuilder
    {
        private const string BundleExtension = ".bundle";
        private const string HashExtension = ".hash";
        private const string CatalogPrefix = "catalog";

        private readonly Func<string, bool> _fileExists;

        /// <summary>Creates a builder; the existence check is injected so tests need no disk files.</summary>
        public RemoteContentPlanBuilder(Func<string, bool> fileExists)
        {
            _fileExists = fileExists;
        }

        /// <summary>
        /// Builds the plan. Throws a readable configuration error when nothing remote was built, a built file is missing,
        /// or no .hash exists (a publication without .hash could not be verified).
        /// </summary>
        public PublishPlanData Build(AddressablesOutputData output, string destinationRoot)
        {
            var locationsArray = output.LocationsList.OrderByDescending(location => location.LocalDirectory.Length).ToArray();
            var filesList = new List<PublishFileData>();
            foreach (var builtFile in output.BuiltFilesList)
            {
                var path = NormalizePath(builtFile);
                var location = locationsArray.FirstOrDefault(candidate => IsUnder(path, candidate.LocalDirectory));
                var stage = Classify(path);
                if (location == null || stage == PublishStageAlias.None)
                {
                    continue;
                }
                if (!_fileExists(path))
                {
                    throw new RemoteContentPublishException(PublishStageAlias.Configuration, $"Файл текущей сборки Addressables не найден на диске: {Path.GetFileName(path)}.");
                }
                var relative = path.Substring(location.LocalDirectory.Length).TrimStart('/');
                var url = location.LoadUrl.TrimEnd('/') + "/" + string.Join("/", relative.Split('/').Select(Uri.EscapeDataString));
                filesList.Add(new PublishFileData(path, RemoteContentPathMapper.MapToRemotePath(destinationRoot, url), url, stage));
            }
            if (filesList.Count == 0)
            {
                throw new RemoteContentPublishException(PublishStageAlias.Configuration, "Текущая сборка Addressables не создала файлов удалённого контента (проверьте Remote.BuildPath/Remote.LoadPath).");
            }
            if (!filesList.Any(file => file.Stage == PublishStageAlias.Hash))
            {
                throw new RemoteContentPublishException(PublishStageAlias.Configuration, "Текущая сборка Addressables не создала удалённый .hash: включите Build Remote Catalog.");
            }
            var orderedList = filesList.OrderBy(file => (int)file.Stage).ThenBy(file => file.RemotePath, StringComparer.Ordinal).ToList();
            return new PublishPlanData(RemoteContentPathMapper.NormalizeRoot(destinationRoot), orderedList);
        }

        /// <summary>Normalizes to an absolute-looking path with forward slashes and no trailing slash.</summary>
        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
        }

        /// <summary>Assigns the upload stage by file name: *.bundle, catalog*.json/bin, *.hash; anything else (e.g. content state) is not published.</summary>
        private static PublishStageAlias Classify(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var name = Path.GetFileName(path);
            if (extension == BundleExtension)
            {
                return PublishStageAlias.Bundles;
            }
            if (extension == HashExtension)
            {
                return PublishStageAlias.Hash;
            }
            var isCatalog = name.StartsWith(CatalogPrefix, StringComparison.OrdinalIgnoreCase) && (extension == ".json" || extension == ".bin");
            return isCatalog ? PublishStageAlias.Catalog : PublishStageAlias.None;
        }

        /// <summary>Returns whether a path lies inside the directory, ignoring case because build folders are local Windows paths.</summary>
        private static bool IsUnder(string path, string directory)
        {
            return path.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase);
        }
    }
}

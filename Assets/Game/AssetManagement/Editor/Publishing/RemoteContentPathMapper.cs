using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Pure helpers that map public content URLs to SFTP paths without duplicating path segments.</summary>
    public static class RemoteContentPathMapper
    {
        private const string ProjectFolderMarker = "RemoteContent";

        /// <summary>Normalizes a user-entered path: forward slashes, one leading slash, no trailing slash. Blank input yields an empty string.</summary>
        public static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }
            var segmentsArray = root.Trim().Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return segmentsArray.Length == 0 ? string.Empty : "/" + string.Join("/", segmentsArray);
        }

        /// <summary>Returns unescaped path segments of an absolute URL; throws a readable error when the URL is not absolute.</summary>
        public static string[] GetUrlSegments(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new RemoteContentPublishException(PublishStageAlias.Configuration, $"Remote.LoadPath не является абсолютным URL: '{url}'.");
            }
            return uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        }

        /// <summary>
        /// Maps a content file URL to its SFTP path under the destination root. The longest run of trailing root folders that also
        /// appears in the URL directory path is shared, so /RemoteContent/Game/main/Android under root .../RemoteContent/Game
        /// gives .../RemoteContent/Game/main/Android/file. With no shared folders the whole URL path is appended.
        /// </summary>
        public static string MapToRemotePath(string destinationRoot, string fileUrl)
        {
            var rootSegmentsArray = NormalizeRoot(destinationRoot).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var urlSegmentsArray = GetUrlSegments(fileUrl);
            var directoryCount = urlSegmentsArray.Length - 1;
            var sharedEnd = 0;
            for (var length = Math.Min(rootSegmentsArray.Length, directoryCount); length >= 1 && sharedEnd == 0; length--)
            {
                for (var end = length; end <= directoryCount; end++)
                {
                    if (SegmentsEqual(rootSegmentsArray, rootSegmentsArray.Length - length, urlSegmentsArray, end - length, length))
                    {
                        sharedEnd = end;
                        break;
                    }
                }
            }
            var remainderArray = urlSegmentsArray.Skip(sharedEnd).ToArray();
            var rootPart = "/" + string.Join("/", rootSegmentsArray);
            return remainderArray.Length == 0 ? rootPart : rootPart + "/" + string.Join("/", remainderArray);
        }

        /// <summary>Returns the folder part of a remote path; the root slash for top-level names.</summary>
        public static string GetDirectory(string remotePath)
        {
            var index = remotePath.LastIndexOf('/');
            return index <= 0 ? "/" : remotePath.Substring(0, index);
        }

        /// <summary>Returns the folder name that follows the RemoteContent segment of the load URL, used only for the settings hint.</summary>
        public static bool TryGetProjectFolderName(string loadUrl, out string projectFolderName)
        {
            projectFolderName = string.Empty;
            if (!Uri.TryCreate(loadUrl, UriKind.Absolute, out _))
            {
                return false;
            }
            var segmentsList = new List<string>(GetUrlSegments(loadUrl));
            var index = segmentsList.IndexOf(ProjectFolderMarker);
            if (index < 0 || index + 1 >= segmentsList.Count)
            {
                return false;
            }
            projectFolderName = segmentsList[index + 1];
            return true;
        }

        /// <summary>Compares equal-length slices of two segment arrays by ordinal, case-sensitive comparison (SFTP paths are case-sensitive).</summary>
        private static bool SegmentsEqual(string[] left, int leftStart, string[] right, int rightStart, int length)
        {
            for (var index = 0; index < length; index++)
            {
                if (!string.Equals(left[leftStart + index], right[rightStart + index], StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

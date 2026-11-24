using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Downloads .hash files with HttpClient on a worker thread, bypassing intermediate caches. Only HTTPS is accepted.</summary>
    public sealed class HttpRemoteHashDownloader : IRemoteHashDownloader
    {
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>Blocks until the file text is downloaded or the timeout expires.</summary>
        public string Download(string url, int timeoutSeconds)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Проверка .hash выполняется только по HTTPS-адресу контента: " + url);
            }
            return Task.Run(() => DownloadAsync(uri, timeoutSeconds)).GetAwaiter().GetResult();
        }

        /// <summary>Requests the URL with no-cache semantics and returns the response body.</summary>
        private static async Task<string> DownloadAsync(Uri uri, int timeoutSeconds)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (var cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
                using (var response = await Client.SendAsync(request, cancellation.Token))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
    }
}

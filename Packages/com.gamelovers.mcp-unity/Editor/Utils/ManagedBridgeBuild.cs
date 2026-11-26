using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace McpUnity.Utils
{
    // Shared by the Windows orchestrator and Unity editor. Keep the lock file on disk:
    // deleting it would allow two processes to lock different files on Unix.
    public static class ManagedBridgeBuild
    {
        public const string InstallArguments = "ci --include=dev --no-audit --no-fund";
        public const string CheckArguments = "ls --all --include=dev --json";
        public const string BuildArguments = "run build";

        public static FileStream Acquire(string server, CancellationToken ct)
        {
            var timer = Stopwatch.StartNew();
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                try { return new FileStream(Path.Combine(server, ".orchestrator-build.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) when (timer.Elapsed < TimeSpan.FromMinutes(10))
                {
                    if (ct.WaitHandle.WaitOne(250)) ct.ThrowIfCancellationRequested();
                }
            }
        }

        // Caller holds Acquire for the entire operation, including any source patch.
        public static void Ensure(string server, bool forceBuild, Func<string, bool> run, CancellationToken ct)
        {
            if (!File.Exists(Path.Combine(server, "package-lock.json")))
                throw new InvalidOperationException("MCP package-lock.json is missing; restore the pinned package.");
            var index = Path.Combine(server, "build", "index.js");
            var incomplete = Path.Combine(server, ".orchestrator-build-incomplete");
            ct.ThrowIfCancellationRequested();
            bool install = !Directory.Exists(Path.Combine(server, "node_modules")) || !run(CheckArguments);
            if (!install && !forceBuild && File.Exists(index) && !File.Exists(incomplete)) return;
            File.WriteAllText(incomplete, "Build not completed");
            if (install && !run(InstallArguments))
                throw new InvalidOperationException("MCP npm ci --include=dev failed.");
            ct.ThrowIfCancellationRequested();
            if (install && !run(CheckArguments))
                throw new InvalidOperationException("MCP local dependencies are still incomplete after npm ci.");
            if (File.Exists(index)) File.Delete(index);
            if (!run(BuildArguments) || !File.Exists(index))
                throw new InvalidOperationException("MCP npm build failed or did not produce build/index.js.");
            ct.ThrowIfCancellationRequested();
            File.Delete(incomplete);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using McpUnity.Tools;
using McpUnity.Resources;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using McpUnity.Utils;

namespace McpUnity.Unity
{
    /// <summary>
    /// Drains work queued from background WebSocket threads on the Unity main thread via
    /// EditorApplication.update, which keeps firing even when the Editor is unfocused.
    ///
    /// This replaces dispatching through EditorApplication.delayCall: delayCall is a plain
    /// static delegate, and a "+=" performed from the WebSocketSharp background thread is not
    /// reliably observed/drained by the main thread while the Editor is idle in the
    /// background. The result was that requests received while Unity was not the foreground
    /// app were never processed, and the MCP client timed out. Draining a thread-safe queue
    /// from EditorApplication.update fixes this without relying on cross-thread delegate
    /// mutation.
    /// </summary>
    [InitializeOnLoad]
    internal static class McpMainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        static McpMainThreadDispatcher()
        {
            EditorApplication.update -= Drain;
            EditorApplication.update += Drain;
        }

        public static void Enqueue(Action action)
        {
            if (action != null) _queue.Enqueue(action);
        }

        private static void Drain()
        {
            while (_queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { McpLogger.LogError($"MainThreadDispatcher action failed: {ex}"); }
            }
        }
    }

    /// <summary>
    /// WebSocket handler for MCP Unity communications
    /// </summary>
    public class McpUnitySocketHandler : WebSocketBehavior
    {
        private static int _activeOperations;
        private readonly McpUnityServer _server;
        private readonly int _connectionGeneration;

        /// <summary>
        /// Creates a WebSocket handler for the active server generation.
        /// </summary>
        public McpUnitySocketHandler(McpUnityServer server, int connectionGeneration)
        {
            _server = server;
            _connectionGeneration = connectionGeneration;

            // Native bridge clients do not send Origin. Browsers always do, and must never be
            // allowed to drive the Editor through a cross-site WebSocket connection.
            OriginValidator = origin => origin == null;
        }

        /// <summary>
        /// Create a standardized error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorType">Type of error</param>
        /// <returns>A JObject containing the error information</returns>
        public static JObject CreateErrorResponse(string message, string errorType)
        {
            return new JObject
            {
                ["error"] = new JObject
                {
                    ["type"] = errorType,
                    ["message"] = message
                }
            };
        }
        
        /// <summary>
        /// Handle incoming messages from WebSocket clients.
        /// WebSocketSharp invokes this on a background thread; we marshal the entire
        /// message-handling body onto Unity's main thread via EditorApplication.delayCall
        /// before touching any Editor APIs.
        ///
        /// Why this matters: accessing EditorStyles or scheduling EditorCoroutines from
        /// a background thread can NRE inside PropertyEditor+Styles..cctor, which under
        /// CLR rules permanently bricks that type for the rest of the AppDomain and
        /// turns the Inspector black until Unity is restarted.
        /// </summary>
        protected override void OnMessage(MessageEventArgs e)
        {
            if (!_server.ShouldTrackClient(_connectionGeneration))
            {
                CloseUntrackedConnection();
                return;
            }

            string data = e.Data;
            // Dispatch via a thread-safe queue drained in EditorApplication.update rather than
            // EditorApplication.delayCall. A delayCall "+=" from this background thread is not
            // reliably drained by the main thread while the Editor is unfocused/idle, so the
            // request would never run and the client would time out. See McpMainThreadDispatcher.
            McpMainThreadDispatcher.Enqueue(() => {
                if (Context?.WebSocket?.ReadyState != WebSocketState.Open || !_server.ShouldTrackClient(_connectionGeneration)) return;
                HandleMessageAsync(data);
            });
        }

        /// <summary>
        /// Handle WebSocket connection open.
        /// Supports multiple concurrent MCP clients (e.g. multiple Claude Code instances).
        /// Cleans up only inactive (dead) sessions to prevent file descriptor accumulation
        /// while keeping other active clients connected.
        /// websocket-sharp uses Mono's IOSelector/select(), which can crash when FD
        /// values exceed ~1024, so stale session cleanup is important.
        /// See: https://github.com/CoderGamester/mcp-unity/issues/110
        /// </summary>
        protected override void OnOpen()
        {
            if (!_server.ShouldTrackClient(_connectionGeneration))
            {
                CloseUntrackedConnection();
                return;
            }

            // Clean up inactive (dead) sessions to prevent file descriptor accumulation.
            // Only removes sessions that are no longer connected — active clients are preserved.
            // Note: Do NOT use ActiveIDs here — it pings every client and blocks.
            var inactiveIds = Sessions.InactiveIDs.ToList();
            if (inactiveIds.Count > 0)
            {
                foreach (var oldId in inactiveIds)
                {
                    // Also remove from our tracking dictionary
                    _server.Clients.TryRemove(oldId, out _);
                    try
                    {
                        Sessions.CloseSession(oldId, CloseStatusCode.Normal, "Stale session cleanup");
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogWarning($"Error closing stale session {oldId}: {ex.Message}");
                    }
                }
                McpLogger.LogInfo($"Cleaned up {inactiveIds.Count} inactive session(s)");
            }

            // Extract client name from the X-Client-Name header (if available)
            string clientName = "";
            NameValueCollection headers = Context.Headers;
            if (headers != null && headers.Contains("X-Client-Name"))
            {
                clientName = headers["X-Client-Name"];
            }

            if (!_server.ShouldTrackClient(_connectionGeneration))
            {
                CloseUntrackedConnection();
                return;
            }

            // Add the client to the server's tracking dictionary
            _server.Clients[ID] = clientName;

            McpLogger.LogInfo($"WebSocket client connected (ID: {ID}, Name: {(string.IsNullOrEmpty(clientName) ? "Unknown" : clientName)}, Total clients: {_server.Clients.Count})");
        }

        /// <summary>
        /// Handle WebSocket connection close
        /// </summary>
        protected override void OnClose(CloseEventArgs e)
        {
            _server.Clients.TryGetValue(ID, out string clientName);

            // Remove the client from the server
            _server.Clients.TryRemove(ID, out _);

            string reason = e.Reason;
            if (reason == "An exception has occurred while receiving.")
            {
                reason = "connection closed by client";
            }

            McpLogger.LogInfo($"WebSocket client '{clientName}' disconnected: {reason} (Remaining clients: {_server.Clients.Count})");
        }

        /// <summary>
        /// Handle WebSocket errors
        /// </summary>
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            McpLogger.LogError($"WebSocket error: {e.Message}");
        }

        /// <summary>
        /// Process a WebSocket message on the Unity main thread.
        /// Safe to call EditorCoroutineUtility, Selection, and other Editor APIs from here.
        /// </summary>
        // Independent callback survives recreation of the socket handler and normal domain reloads.
        // Only a matching observed run can complete the saved test operation.
        [InitializeOnLoad]
        private sealed class DurableTestCallbacks : ICallbacks
        {
            private static readonly TestRunnerApi Api;
            private static string PendingPath => Path.Combine(OperationDirectory, "active-test.json");
            static DurableTestCallbacks()
            {
                Api = ScriptableObject.CreateInstance<TestRunnerApi>();
                Api.RegisterCallbacks(new DurableTestCallbacks());
            }
            internal static void Prepare(string operationId, JObject parameters)
            {
                if (File.Exists(PendingPath)) throw new InvalidOperationException("An unresolved test operation exists; inspect it before launching another test run");
                Directory.CreateDirectory(OperationDirectory);
                Write(new JObject { ["operationId"] = operationId, ["started"] = false, ["parameters"] = parameters.DeepClone() });
            }
            private static void Write(JObject value)
            {
                var temporary = PendingPath + ".tmp";
                File.WriteAllText(temporary, value.ToString(Formatting.None));
                if (File.Exists(PendingPath)) File.Replace(temporary, PendingPath, null);
                else File.Move(temporary, PendingPath);
            }
            internal static string PendingId => File.Exists(PendingPath) ? JObject.Parse(File.ReadAllText(PendingPath))["operationId"]?.Value<string>() : null;
            internal static bool StartObserved(string id) => IsPending(id) && JObject.Parse(File.ReadAllText(PendingPath))["started"]?.Value<bool>() == true;
            internal static void Finish(string id)
            {
                if (IsPending(id)) File.Delete(PendingPath);
            }
            internal static bool IsPending(string id)
            {
                return File.Exists(PendingPath) && JObject.Parse(File.ReadAllText(PendingPath))["operationId"]?.Value<string>() == id;
            }
            public void RunStarted(ITestAdaptor testsToRun)
            {
                if (!File.Exists(PendingPath)) return;
                var pending = JObject.Parse(File.ReadAllText(PendingPath));
                if (pending["started"]?.Value<bool>() == true)
                {
                    SaveOperation(pending["operationId"].Value<string>(), "run_tests", CreateErrorResponse("A different test run started before the previous result was recovered; outcome unknown", "test_run_interrupted"));
                    File.Delete(PendingPath); return;
                }
                pending["started"] = true; pending["rootId"] = testsToRun.Id; pending["rootName"] = testsToRun.FullName;
                Write(pending);
            }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!File.Exists(PendingPath)) return;
                var pending = JObject.Parse(File.ReadAllText(PendingPath));
                if (pending["started"]?.Value<bool>() != true || pending["rootId"]?.Value<string>() != result.Test.Id || pending["rootName"]?.Value<string>() != result.Test.FullName) return;
                var parameters = pending["parameters"] as JObject ?? new JObject();
                var results = new JArray();
                Collect(result, results, parameters["returnOnlyFailures"]?.Value<bool>() ?? true, parameters["returnWithLogs"]?.Value<bool>() ?? false);
                var total = result.PassCount + result.FailCount + result.SkipCount;
                var response = new JObject { ["success"] = true, ["testCount"] = total, ["passCount"] = result.PassCount,
                    ["failCount"] = result.FailCount, ["skipCount"] = result.SkipCount, ["resultState"] = result.ResultState,
                    ["message"] = $"Recovered test run: {result.PassCount}/{total} passed; {result.FailCount} failed; {result.SkipCount} skipped",
                    ["durationSeconds"] = result.Duration, ["results"] = results, ["recoveredAfterReload"] = true };
                SaveOperation(pending["operationId"].Value<string>(), "run_tests", response);
                File.Delete(PendingPath);
            }
            private static void Collect(ITestResultAdaptor result, JArray results, bool onlyFailures, bool logs)
            {
                if (result.HasChildren) { foreach (var child in result.Children) Collect(child, results, onlyFailures, logs); return; }
                if (onlyFailures && !result.ResultState.StartsWith("Failed", StringComparison.Ordinal)) return;
                results.Add(new JObject { ["name"] = result.Name, ["fullName"] = result.FullName, ["state"] = result.ResultState,
                    ["message"] = result.Message, ["duration"] = result.Duration, ["logs"] = logs ? result.Output : null, ["stackTrace"] = result.StackTrace });
            }
        }

        private static readonly string OperationGeneration = Guid.NewGuid().ToString("N");
        private static string OperationDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/UnityOrchestrator/Operations"));

        // Durable receipt and result: never replay commands after transport loss or reload.
        private static void SaveOperation(string id, string method, JObject result = null)
        {
            if (!Guid.TryParse(id, out var parsed)) return;
            Directory.CreateDirectory(OperationDirectory);
            var path = Path.Combine(OperationDirectory, parsed.ToString("N") + ".json");
            var value = new JObject { ["operationId"] = id, ["method"] = method, ["generation"] = OperationGeneration,
                ["status"] = result == null ? "running" : "completed", ["updatedUtc"] = DateTime.UtcNow.ToString("O") };
            if (result != null) value["result"] = result.DeepClone();
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, value.ToString(Formatting.None));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }

        // A receipt from a previous domain is interrupted, never presumed successful.
        private static JObject ReadOperation(JObject parameters)
        {
            var id = parameters["operationId"]?.Value<string>();
            if (!Guid.TryParse(id, out var parsed)) return new JObject { ["status"] = "missing" };
            var path = Path.Combine(OperationDirectory, parsed.ToString("N") + ".json");
            if (!File.Exists(path)) return new JObject { ["status"] = "missing", ["operationId"] = id };
            var value = JObject.Parse(File.ReadAllText(path));
            if (value["status"]?.Value<string>() == "running" && value["generation"]?.Value<string>() != OperationGeneration)
                value["status"] = value["method"]?.Value<string>() == "run_tests" && DurableTestCallbacks.IsPending(id) ? "running" : "interrupted";
            return value;
        }

        // Version-adaptive, read-only probe. 1.x exposes IsRunActive internally; newer
        // versions expose a public probe. Unsupported signatures fail closed.
        public static bool? QueryTestRunnerActive()
        {
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            foreach (var name in new[] { "IsAnyRunActive", "IsRunActive", "IsTestRunActive" })
            {
                var method = typeof(TestRunnerApi).GetMethods(flags).FirstOrDefault(m => m.Name == name && m.ReturnType == typeof(bool) && m.GetParameters().Length == 0);
                if (method != null) return (bool)method.Invoke(null, null);
            }
            return null;
        }

        // Public managed recovery path: absence of a live runner must be proven by its API.
        private static JObject RecoverTestOperation(JObject parameters)
        {
            var id = parameters["operationId"]?.Value<string>();
            if (string.IsNullOrEmpty(id)) id = DurableTestCallbacks.PendingId;
            if (string.IsNullOrEmpty(id)) return new JObject { ["success"] = true, ["status"] = "none" };
            var receipt = ReadOperation(new JObject { ["operationId"] = id });
            if (receipt["method"]?.Value<string>() != "run_tests") return new JObject { ["success"] = false, ["reason"] = "Not a recorded test operation" };
            bool? active = QueryTestRunnerActive();
            if (parameters["action"]?.Value<string>() == "inspect")
                return new JObject { ["success"] = true, ["receipt"] = receipt, ["runnerActive"] = active.HasValue ? new JValue(active.Value) : JValue.CreateNull() };
            if (parameters["action"]?.Value<string>() != "resolve_inactive") return new JObject { ["success"] = false, ["reason"] = "Unsupported recovery action" };
            if (active != false || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return new JObject { ["success"] = false, ["reason"] = "Runner is active, unknown, or editor is busy; receipt retained; no test cancelled", ["operationId"] = id };
            // Do not release a different pending test or an unrelated editor operation.
            var pendingId = DurableTestCallbacks.PendingId;
            if (!string.IsNullOrEmpty(pendingId) && !string.Equals(pendingId, id, StringComparison.Ordinal))
                return new JObject { ["success"] = false, ["reason"] = "A different test operation is pending" };
            var completed = receipt["status"]?.Value<string>() == "completed";
            if (!completed && receipt["generation"]?.Value<string>() == OperationGeneration && !DurableTestCallbacks.StartObserved(id))
                return new JObject { ["success"] = false, ["reason"] = "Start has not been observed in this domain; dispatch may still be queued. Receipt retained." };
            var failure = CreateErrorResponse("Runner confirmed inactive; previous test has no recoverable result and must not be claimed passed", "test_result_unavailable");
            if (!completed) SaveOperation(id, "run_tests", failure);
            DurableTestCallbacks.Finish(id);
            // Release a same-domain waiter as well as its disk receipt. Does not start/cancel tests.
            var service = typeof(McpUnitySocketHandler).Assembly.GetType("McpUnity.Services.TestRunnerService");
            service?.GetMethod("ResolveInactiveWaiters", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            return new JObject { ["success"] = true, ["resolvedInactive"] = true, ["operationId"] = id,
                ["previousResultPreserved"] = completed, ["markedPassedByRecovery"] = false,
                ["message"] = "Inactive receipt resolved. Inspect any preserved result; rerun only required isolated checks. No live run was cancelled." };
        }

        private async void HandleMessageAsync(string data)
        {
            bool trackedOperation = false, activeCounted = false;
            string operationId = null, operationMethod = null;
            try
            {
                if (!_server.ShouldTrackClient(_connectionGeneration))
                {
                    CloseUntrackedConnection();
                    return;
                }

                McpLogger.LogInfo("WebSocket command received on main thread");
                JObject requestJson;
                try
                {
                    requestJson = JObject.Parse(data);
                }
                catch (JsonReaderException jre)
                {
                    McpLogger.LogError($"Invalid JSON received: {jre.Message}. Data: {data}");
                    // Attempt to send a parse error response. No requestId is available yet.
                    Send(CreateResponse(null, CreateErrorResponse($"Invalid JSON format: {jre.Message}", "invalid_json")).ToString(Formatting.None));
                    return;
                }

                var method = requestJson["method"]?.ToString();
                var parameters = requestJson["params"] as JObject ?? new JObject();
                var requestId = requestJson["id"]?.ToString();
                var deadline = requestJson["_deadlineUtcMs"]?.Value<long?>();
                if (deadline.HasValue && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= deadline.Value)
                {
                    Send(CreateResponse(requestId, CreateErrorResponse($"MCP_NOT_DISPATCHED: {method}; editor queue deadline expired", "timeout")).ToString(Formatting.None));
                    return;
                }
                McpLogger.LogInfo($"Dispatch request {requestId}; method={method}; compiling={EditorApplication.isCompiling}; updating={EditorApplication.isUpdating}");
                trackedOperation = method != "get_play_mode_status" && method != "get_scene_info" && method != "get_console_logs" && method != "get_operation_result" && method != "recover_test_operation";
                if (trackedOperation)
                {
                    operationId = requestId; operationMethod = method;
                    var previous = ReadOperation(new JObject { ["operationId"] = requestId });
                    if (previous["status"]?.Value<string>() != "missing")
                    {
                        var priorResult = previous["result"] as JObject ?? CreateErrorResponse("MCP_RESULT_UNKNOWN: operation already dispatched; inspect recorded status", "operation_pending");
                        if (Context?.WebSocket?.ReadyState == WebSocketState.Open) Send(CreateResponse(requestId, priorResult).ToString(Formatting.None));
                        trackedOperation = false; return;
                    }
                    SaveOperation(operationId, operationMethod);
                }
                if (trackedOperation) { _activeOperations++; activeCounted = true; }
                // We need to dispatch to Unity's main thread and wait for completion
                var tcs = new TaskCompletionSource<JObject>();

                if (string.IsNullOrEmpty(method))
                {
                    tcs.SetResult(CreateErrorResponse("Missing method in request", "invalid_request"));
                }
                else if (method == "recover_test_operation")
                {
                    tcs.SetResult(RecoverTestOperation(parameters));
                }
                else if (method == "get_operation_result")
                {
                    tcs.SetResult(ReadOperation(parameters));
                }
                else if (method == "reload_active_scene")
                {
                    tcs.SetResult(ReloadActiveScene(parameters));
                }
                else if (method == "cleanup_temporary_assets")
                {
                    tcs.SetResult(await CleanupTemporaryAssets(parameters, deadline));
                }
                else if (_server.TryGetTool(method, out var tool))
                {
                    if (method == "run_tests") DurableTestCallbacks.Prepare(operationId, parameters);
                    EditorCoroutineUtility.StartCoroutineOwnerless(ExecuteTool(tool, parameters, tcs));
                }
                else if (_server.TryGetResource(method, out var resource))
                {
                    EditorCoroutineUtility.StartCoroutineOwnerless(FetchResourceCoroutine(resource, parameters, tcs));
                }
                else
                {
                    tcs.SetResult(CreateErrorResponse($"Unknown method: {method}", "unknown_method"));
                }

                JObject responseJson = await tcs.Task;
                if (method == "get_play_mode_status")
                {
                    bool compiling = EditorApplication.isCompiling;
                    bool updating = EditorApplication.isUpdating;
                    bool transitioning = EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode;
                    responseJson["orchestratorReadiness"] = new JObject {
                        ["ready"] = !compiling && !updating && !transitioning && _activeOperations == 0,
                        ["reason"] = compiling ? "compilation" : updating ? "asset import" : transitioning ? "play mode transition" : _activeOperations > 0 ? "previous bridge operation" : "ready",
                        ["activeOperations"] = _activeOperations
                    };
                }
                if (trackedOperation)
                {
                    SaveOperation(operationId, operationMethod, responseJson);
                    if (method == "run_tests") DurableTestCallbacks.Finish(operationId);
                }
                JObject jsonRpcResponse = CreateResponse(requestId, responseJson);
                string responseStr = jsonRpcResponse.ToString(Formatting.None);

                McpLogger.LogInfo($"WebSocket message response for request ID '{requestId}': {responseStr}");

                // Send the response back to the client
                if (Context?.WebSocket?.ReadyState == WebSocketState.Open && _server.ShouldTrackClient(_connectionGeneration)) Send(responseStr);
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error processing message: {ex.Message}");

                var failure = CreateErrorResponse($"Internal server error: {ex.Message}", "internal_error");
                if (trackedOperation)
                {
                    try { SaveOperation(operationId, operationMethod, failure); if (operationMethod == "run_tests") DurableTestCallbacks.Finish(operationId); }
                    catch (IOException journalError) { McpLogger.LogError("Operation journal: " + journalError.Message); }
                }
                if (Context?.WebSocket?.ReadyState == WebSocketState.Open) Send(CreateResponse(operationId, failure).ToString(Formatting.None));
            }
            finally { if (activeCounted) _activeOperations--; }
        }

        // Scene paths are taken from Unity, never from an arbitrary caller-supplied destination.
        private static JObject ReloadActiveScene(JObject parameters)
        {
            string memoryBackup = "", diskBackup = "";
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || _activeOperations > 1)
                    return new JObject { ["success"] = false, ["reason"] = "Editor is busy; wait for readiness before scene recovery" };
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (EditorSceneManager.IsPreviewScene(scene)) throw new IOException("Preview/prefab stage is not a reloadable project scene; return to the main stage first");
                var scenePath = scene.path ?? "";
                var project = Path.GetDirectoryName(Application.dataPath);
                var fullPath = string.IsNullOrEmpty(scenePath) ? "" : Path.GetFullPath(Path.Combine(project, scenePath));
                if (fullPath.Length > 0 && !fullPath.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Active scene is outside selected project");
                for (var parent = fullPath.Length == 0 ? null : fullPath; parent != null; parent = Path.GetDirectoryName(parent))
                    if ((File.Exists(parent) || Directory.Exists(parent)) && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked active scene path rejected");
                string Hash(string path)
                {
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "";
                    using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
                        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                }
                var diskHash = Hash(fullPath);
                if (parameters["action"]?.Value<string>() == "capture")
                    return new JObject { ["success"] = true, ["scenePath"] = scenePath, ["sceneHandle"] = (int)scene.handle, ["diskSha256"] = diskHash, ["dirty"] = scene.isDirty };
                if (parameters["action"]?.Value<string>() != "reload") throw new IOException("Unknown scene recovery action");
                if (parameters["expectedScenePath"]?.Value<string>() != scenePath || parameters["expectedSceneHandle"]?.Value<int>() != (int)scene.handle)
                    throw new IOException("Active scene changed; capture again before reloading");
                var beforeHash = parameters["expectedDiskSha256"]?.Value<string>();
                if (beforeHash == null || (beforeHash.Length != 0 && (beforeHash.Length != 64 || beforeHash.Any(c => !Uri.IsHexDigit(c)))))
                    throw new IOException("Capture a valid disk fingerprint before editing the scene");
                var diskChanged = !string.Equals(beforeHash, diskHash, StringComparison.OrdinalIgnoreCase);
                var dirty = scene.isDirty;
                // A scene already dirty at capture may predate the on-disk state; do not overwrite it.
                var alreadyDirty = parameters["expectedEditorWasDirty"]?.Value<bool>() ?? true;
                if (dirty || string.IsNullOrEmpty(scenePath))
                {
                    var folder = "Assets/__OrchestratorRecovery/" + Guid.NewGuid().ToString("N");
                    var recovery = Path.GetFullPath(Path.Combine(project, folder));
                    for (var parent = recovery; parent != null; parent = Path.GetDirectoryName(parent))
                        if (Directory.Exists(parent) && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked recovery directory rejected");
                    Directory.CreateDirectory(recovery);
                    memoryBackup = folder + "/EditorState.unity";
                    if (!EditorSceneManager.SaveScene(scene, memoryBackup, true)) throw new IOException("Editor scene backup failed; original scene retained");
                    if (File.Exists(fullPath)) { diskBackup = folder + "/DiskState.unity"; File.Copy(fullPath, Path.Combine(project, diskBackup)); }
                    if (dirty && !diskChanged && !alreadyDirty && !string.IsNullOrEmpty(scenePath))
                        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Saving active scene failed; backup preserved and reload cancelled");
                }
                var target = string.IsNullOrEmpty(scenePath) ? memoryBackup : scenePath;
                if (!File.Exists(Path.Combine(project, target))) throw new IOException("Scene file is missing; backups preserved and reload cancelled");
                var placeholder = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                if (!EditorSceneManager.CloseScene(scene, true))
                { EditorSceneManager.CloseScene(placeholder, true); throw new IOException("Could not close active scene; reload cancelled"); }
                var restored = EditorSceneManager.OpenScene(target, OpenSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(restored);
                EditorSceneManager.CloseScene(placeholder, true);
                var warning = (diskChanged || alreadyDirty) && dirty && !string.IsNullOrEmpty(scenePath) ? "Disk scene reloaded; conflicting or pre-existing unsaved editor state is preserved in " + memoryBackup + "; disk snapshot: " + diskBackup :
                    string.IsNullOrEmpty(scenePath) ? "Unnamed scene restored from recovery copy " + memoryBackup : "";
                return new JObject { ["success"] = true, ["reloaded"] = true, ["scenePath"] = restored.path, ["memoryBackup"] = memoryBackup, ["diskBackup"] = diskBackup, ["warning"] = warning };
            }
            catch (Exception ex)
            { return new JObject { ["success"] = false, ["reason"] = ex.Message, ["memoryBackup"] = memoryBackup, ["diskBackup"] = diskBackup }; }
        }

        // Runs on the editor main thread. No caller-supplied filesystem path is accepted.
        private static async Task<JObject> CleanupTemporaryAssets(JObject parameters, long? deadline)
        {
            JObject Retained(string reason) => new JObject { ["success"] = true, ["deleted"] = false, ["reason"] = reason };
            bool projectChanged = false;
            void Changed() { projectChanged = true; }
            EditorApplication.projectChanged += Changed;
            try
            {
                var id = parameters["scopeId"]?.Value<string>();
                if (!Guid.TryParseExact(id, "N", out _)) return Retained("Invalid temporary scope id");
                var expected = parameters["files"] as JObject;
                if (expected == null) return Retained("Missing sealed file inventory");
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || _activeOperations > 1)
                    return Retained("Editor is busy or in Play Mode");
                var assetRoot = "Assets/__OrchestratorTemp-" + id;
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, "__OrchestratorTemp-" + id));
                bool Exists(string path)
                {
                    try { File.GetAttributes(path); return true; }
                    catch (FileNotFoundException) { return false; }
                    catch (DirectoryNotFoundException) { return false; }
                }
                void NoLinks(string path)
                {
                    for (var current = path; current != null; current = Path.GetDirectoryName(current))
                        if (Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                            throw new IOException("Linked asset path retained");
                }
                NoLinks(root);
                string Hash(string path)
                {
                    NoLinks(path);
                    using (var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
                }
                JObject Inventory()
                {
                    var actual = new JObject();
                    void Walk(string dir)
                    {
                        NoLinks(dir);
                        foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
                        {
                            NoLinks(entry);
                            var relative = entry.Substring(root.Length + 1).Replace('\\', '/');
                            if (Directory.Exists(entry)) { actual[relative + "/"] = "directory"; Walk(entry); }
                            else actual[relative] = Hash(entry);
                        }
                    }
                    if (Directory.Exists(root)) Walk(root);
                    else if (File.Exists(root)) throw new IOException("Scope replaced by a file");
                    if (Exists(root + ".meta")) actual["@root.meta"] = Hash(root + ".meta");
                    return actual;
                }
                bool Same(JObject a, JObject b) => a.Count == b.Count && a.Properties().All(x => b[x.Name]?.Value<string>() == x.Value.Value<string>());
                if (!Exists(root) && !Exists(root + ".meta"))
                    return new JObject { ["success"] = true, ["deleted"] = true };
                if (!Same(Inventory(), expected)) return Retained("Assets changed after sealing");
                for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.isDirty || scene.path == assetRoot || scene.path.StartsWith(assetRoot + "/", StringComparison.Ordinal))
                        return Retained("Dirty or temporary scene still open; preserve editor state");
                }
                foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                    if (EditorUtility.IsPersistent(obj) && EditorUtility.IsDirty(obj) && AssetDatabase.GetAssetPath(obj).StartsWith(assetRoot + "/", StringComparison.Ordinal))
                        return Retained("Temporary asset has unsaved edits");
                // External serialized dependencies are retained; disposable roots cannot enter production.
                var inspected = 0;
                foreach (var path in AssetDatabase.GetAllAssetPaths().Where(x => !AssetDatabase.IsValidFolder(x) && x != assetRoot && !x.StartsWith(assetRoot + "/", StringComparison.Ordinal)))
                {
                    if (deadline.HasValue && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= deadline.Value) return Retained("Dependency inspection deadline expired; assets preserved");
                    // Every external edge is checked, so recursive traversal is unnecessary.
                    if (AssetDatabase.GetDependencies(path, false).Any(x => x == assetRoot || x.StartsWith(assetRoot + "/", StringComparison.Ordinal)))
                        return Retained("Referenced by " + path);
                    if (++inspected % 20 == 0) await Task.Yield();
                    if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || _activeOperations > 1)
                        return Retained("Editor state changed during dependency inspection");
                }
                for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.isDirty || scene.path == assetRoot || scene.path.StartsWith(assetRoot + "/", StringComparison.Ordinal))
                        return Retained("Scene state changed during inspection");
                }
                foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                    if (EditorUtility.IsPersistent(obj) && EditorUtility.IsDirty(obj) && AssetDatabase.GetAssetPath(obj).StartsWith(assetRoot + "/", StringComparison.Ordinal))
                        return Retained("Temporary asset changed during inspection");
                if (!Same(Inventory(), expected)) return Retained("Assets changed during inspection");
                if (deadline.HasValue && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= deadline.Value) return Retained("Cleanup deadline expired; assets preserved");
                if (projectChanged) return Retained("Project assets changed during dependency inspection; retry when idle");
                if (!AssetDatabase.DeleteAsset(assetRoot)) return Retained("AssetDatabase refused deletion");
                return new JObject { ["success"] = true, ["deleted"] = !Directory.Exists(root) && !File.Exists(root + ".meta"), ["reason"] = "AssetDatabase cleanup" };
            }
            catch (Exception ex) { return Retained(ex.Message); }
            finally { EditorApplication.projectChanged -= Changed; }
        }

        private void CloseUntrackedConnection()
        {
            try
            {
                WebSocket webSocket = Context?.WebSocket;
                if (webSocket?.ReadyState == WebSocketState.Open)
                {
                    webSocket.Close(CloseStatusCode.Away, "Server is restarting");
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Error closing untracked WebSocket connection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Execute a tool with the provided parameters
        /// </summary>
        private IEnumerator ExecuteTool(McpToolBase tool, JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            try
            {
                if (tool.IsAsync)
                {
                    tool.ExecuteAsync(parameters, tcs);
                }
                else
                {
                    var result = tool.Execute(parameters);
                    tcs.SetResult(result);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error executing tool {tool.Name}: {ex.Message}\n{ex.StackTrace}");
                tcs.SetResult(CreateErrorResponse(
                    $"Failed to execute tool {tool.Name}: {ex.Message}",
                    "tool_execution_error"
                ));
            }
            
            yield return null;
        }
        
        /// <summary>
        /// Fetch a resource with the provided parameters
        /// </summary>
        private IEnumerator FetchResourceCoroutine(McpResourceBase resource, JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            try
            {
                if (resource.IsAsync)
                {
                    resource.FetchAsync(parameters, tcs);
                }
                else
                {
                    var result = resource.Fetch(parameters);
                    tcs.SetResult(result);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error fetching resource {resource.Name}: {ex.Message}\n{ex.StackTrace}");
                tcs.SetResult(CreateErrorResponse(
                    $"Failed to fetch resource {resource.Name}: {ex.Message}",
                    "resource_fetch_error"
                ));
            }
            yield return null;
        }
        
        /// <summary>
        /// Create a JSON-RPC 2.0 response
        /// </summary>
        /// <param name="requestId">Request ID</param>
        /// <param name="result">Result object</param>
        /// <returns>JSON-RPC 2.0 response</returns>
        private JObject CreateResponse(string requestId, JObject result)
        {
            // Format as JSON-RPC 2.0 response
            JObject jsonRpcResponse = new JObject
            {
                ["id"] = requestId
            };
            
            // Add result or error
            if (result.TryGetValue("error", out var errorObj))
            {
                jsonRpcResponse["error"] = errorObj;
            }
            else
            {
                jsonRpcResponse["result"] = result;
            }
            
            return jsonRpcResponse;
        }
    }
}

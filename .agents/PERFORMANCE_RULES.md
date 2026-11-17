# Performance
## Decisions and algorithms
- Preserve agreed behavior. Choose algorithms/data structures for expected data and object counts; avoid repeated full scans, nested searches and recomputation when indices, incremental updates or bounded caching fit the workload.
- Prefer a simple adequate solution. Do not introduce pooling, Jobs/Burst/ECS, threads or a loading framework solely because they may be faster. Reuse project infrastructure; dependencies follow AGENTS.
- Per-frame work follows ARCHITECTURE_RULES. Update only relevant/changed state where behavior permits; do not reduce required input, physics or visual responsiveness silently.
- For performance fixes, measure a reproducible baseline and the result under comparable conditions. For new code, avoid evident waste without requiring benchmarks for every method. Unmeasured gains are hypotheses, not verified improvements.

## Memory and resource lifetime
- Define an owner, acquisition point and release point for loaded assets, runtime-created Unity objects, buffers, caches and pools. Shared resources need explicit shared ownership; release only after all consumers finish.
- Load content for its intended usage window. Keep persistent/shared content only for a justified lifetime; do not preload an entire catalog by default. Balance latency, peak memory and repeated load/unload churn.
- Bound caches/pools and define eviction/teardown; pooled inactive objects may still retain assets. Avoid unbounded growth and duplicate runtime copies. Separate disk/download size from resident CPU/GPU memory.
- Reuse the existing loading solution. Use Addressables where managed asynchronous asset/scene loading fits the task; do not migrate all assets or install it without authorization.

## Addressables (when used)
- Inspect the installed package version and loading abstraction before choosing APIs. Track operation ownership and balance acquired references with the corresponding release/unload policy, including failure paths; avoid double release and use-after-release.
- Distinguish loaded assets, instantiated objects and scenes; follow the correct API and auto-release/instance-tracking behavior. Ordinary prefab clones do not independently retain Addressables ownership: keep their asset ownership alive until all clones finish.
- Handle concurrent/repeated requests, failures, transitions and owner teardown. Cancellation of a waiter does not prove that the underlying load stopped or ownership was released: verify the actual API/wrapper contract and clean up late results without applying stale state.
- Avoid synchronous waits in interactive paths. Define loading readiness and failure behavior before activating consumers; preserve shared resources through scene transitions and account for overlapping old/new content at peak memory.
- Group bundles by usage/lifetime and sharing when changing content layout. Inspect dependency duplication and unnecessary retention; do not assume releasing one asset immediately frees bundle memory. No routine forced GC or Resources.UnloadUnusedAssets as a substitute for correct ownership.

## Validation and project context
- Use available Unity Profiler/Memory Profiler, Addressables diagnostics and build reports as appropriate. Check CPU/GPU/frame spikes, GC allocations, resident/peak memory and loading latency relevant to the task, preferably in a representative device/player build.
- For loading changes, exercise repeated enter/exit or load/release cycles, shared consumers, errors and teardown during loading. Look for growing retained resources and stale callbacks; distinguish intentionally retained caches from leaks.
- Record platform/device, budgets, persistent content, loading/group policies and reproducible profiling procedures only in `.agents/project/`, with selective INDEX routing per PROJECT_CONTEXT_RULES. Do not invent budgets; clarify them when they materially affect the design.
- Validation/reporting follows VALIDATION_RULES. If tools/builds are unavailable, state what remains unverified and provide focused manual checks; never claim measured performance or leak freedom without evidence.

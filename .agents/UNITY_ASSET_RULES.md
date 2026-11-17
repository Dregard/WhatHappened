# Unity integrity
- Preserve `.meta` files/GUIDs; move assets with their meta files. Do not edit Library/Temp/obj/Logs or generated content.
- Before changing/renaming serialized fields, inspect affected scenes/prefabs/assets and preserve data with appropriate migration. This includes configuration migrations.
- Prefer Editor APIs/project tooling for complex asset changes; no blind serialized-text rewrites.
- Before Unity/MCP operations, inspect the actual available tools and editor readiness; do not assume tool names, parameters, ports or capabilities from prior sessions. Use a supported equivalent workflow if available; avoid generating temporary MenuItems for operations already supported by working tools. Report material verification limits.
- For asset moves, preserve GUIDs and inspect path-based consumers, Editor tooling, loading keys, Addressables settings and affected scene/prefab references. Keep external identifiers stable unless migration is in scope; folder moves alone do not justify regrouping bundles.
- Respect recorded Unity version/packages. Inspect initialization/teardown and clean up subscriptions, async operations and tweens with their owner's lifetime.

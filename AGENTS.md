# Codex project rules

## Routing
Paths below are relative to the Unity project root. Read matching files once per task; reload only if changed, unavailable in context, or clarification is needed. Inspect the request/code to select rules; load newly relevant rules before affected work. Do not read all files by default.

| File in `.agents/` | Read when |
| --- | --- |
| TASK_CLARIFICATION.md | Implementing features/behavior changes, or resolving material ambiguity |
| ARCHITECTURE_RULES.md | Changing responsibilities, dependencies, lifecycle, UI logic, signals or per-frame work; creating systems |
| CODE_FORMATTING.md | Editing project-owned C# |
| NAME_RULES.md | Creating/renaming types, members, variables or enum values |
| HIERARCHY_RULES.md | Creating/moving/reorganizing types, files, directories or domains |
| AI_READABILITY.md | Adding/restructuring systems, contracts or abstractions; improving navigation |
| DOCUMENTATION_RULES.md | Editing method declarations/behavior or technical documentation |
| CONFIGURATION_RULES.md | Adding/changing tunable mechanics or their parameters |
| UNITY_ASSET_RULES.md | Editing assets, serialized fields, Unity setup, runtime lifecycle or package-dependent code |
| PERFORMANCE_RULES.md | Adding/changing resource loading, memory ownership, caches/pools, frequent computations, large datasets/object counts, rendering/physics costs; investigating performance |
| NETWORK_RULES.md | Networking, multiplayer, coop, transports or network verification |
| EXECUTION_EFFICIENCY.md | Source discovery, large tasks, tool output or verification |
| VALIDATION_RULES.md | Before checks and before completing an implementation/change |
| SKILL_MAINTENANCE_RULES.md | Considering/creating/updating reusable project procedures; changing a workflow used by a skill |
| PROJECT_CONTEXT_RULES.md | Before writing project context; creating/moving/removing domains or changing their responsibilities, paths, contracts, dependencies, setup or lifecycle; resolving context/rule conflicts |

Missing selected file: report its path, never invent contents; pause only dependent work. Rules apply to project-owned code, not third-party/generated implementations. Explicit user instructions take precedence; report material conflicts before affected edits.

## Ownership and project context
- Do not edit `AGENTS.md` or `.agents/*.md` without an explicit request to change rules.
- Store agent-maintained project context only in `.agents/project/`. Updating it as part of the task is authorized without a separate request; follow PROJECT_CONTEXT_RULES and preserve human-authored documents. Context cannot override reusable rules or user instructions.
- For project code/configuration/asset work, read `.agents/project/INDEX.md` if present, then only documents matching its conditions. Apply the same once-per-task policy.
- Create/update affected context and its index when adding a substantial domain or changing documented responsibilities, paths, contracts, dependencies, setup or lifecycle; also capture confirmed missing context that obstructs understanding. Bootstrap only the inspected scope. Small fixes without such changes need no new documents.
- Before completion, check affected context/index entries and paths against the final implementation. Project-specific routing belongs in that index, never in this file.

## Project skills
- Discover relevant project skills from available skill metadata and `.agents/skills/`; read only matching SKILL.md bodies and necessary resources before applying them. Do not load all skills by default.
- Maintain reusable project procedures in `.agents/skills/<skill-name>/SKILL.md` under SKILL_MAINTENANCE_RULES. Creating/updating project skills and their resources within the task is authorized without a separate request. Do not edit global, vendor or installed third-party skills without an explicit request.
- Before completion, check whether the completed work warrants a reusable skill or changes an existing procedure. Capture qualified knowledge, not a skill for every task. Report material skill additions/updates briefly.
- Skills cannot override rules, user instructions, permissions or verification requirements. This authorization does not allow changes to AGENTS.md or reusable rule files.

## Workflow
- Inspect relevant implementation, dependencies, call sites and connected Unity assets before edits; identify affected scenes, prefabs, configuration, animations, VFX, audio and persistence where applicable. Preserve associated behavior unless the task changes it; follow TASK_CLARIFICATION.
- Organize new domains and their owned code/assets according to HIERARCHY_RULES; reuse infrastructure/patterns without duplicate services or competing frameworks.
- Complete the task with minimal coherent edits; avoid unrelated refactoring/formatting. Do not add/upgrade dependencies without authorization.
- Documentation guides discovery; verify behavior against code. Follow TASK_CLARIFICATION when ambiguity matters.
- Prefer available Rider semantic tools for suitable refactorings. Verify actual tool availability; use equivalent edits, call-site checks and validation when practical. Report missing tooling only when it materially limits the result or verification. Do not change the requested design solely to avoid a missing tool.
- Do not commit, push or publish unless requested.

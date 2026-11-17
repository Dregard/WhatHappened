# Project skill maintenance
## Purpose and ownership
- Rules define constraints; `.agents/project/` describes actual project structure/contracts; skills describe reusable procedures. Keep each fact in its authoritative location and reference it instead of copying.
- Maintain project-owned skills at `.agents/skills/<skill-name>/SKILL.md`. Use one canonical source for Codex/OpenCode; do not maintain duplicate copies under `.codex`, `.claude` or `.opencode`. Preserve an established alternative until a migration is in scope.
- Creating/updating these project skills and necessary resources is authorized as part of relevant work. Preserve human-authored content and unrelated procedures. Do not modify global/vendor skills, permissions, dependencies or agent configuration without applicable authorization.

## When to capture knowledge
- Search existing skill metadata before creating. Extend a matching skill when responsibilities align; do not create competing versions.
- Capture a procedure when it has recurred, or a substantial completed workflow is clearly reusable and contains project-specific steps, fragile ordering, tooling details or non-obvious checks that would otherwise need rediscovery.
- Do not create skills for trivial edits, task reports, generic programming advice, every class/domain, or hypothetical future workflows. A small fix can complete with no skill changes.
- On relevant API, asset layout, tool or workflow changes, inspect affected skills and update obsolete steps/references in the same task. Do not rewrite all skills.
- Do not invent project procedures from these generic rules. Establish implementation paths, commands and Unity operations against the actual project first.

## Skill format and discovery
- Use a lowercase kebab-case folder/name of at most 64 characters. SKILL.md starts with valid YAML frontmatter containing `name` and `description`; name matches the directory. Description states both purpose and concrete activation conditions.
- Keep the body concise: scope/prerequisites and required inputs; ordered steps and decision points; expected outputs; focused verification and failure handling. Include exclusions where they prevent accidental activation.
- Resolve implementation paths from the Unity project root; bundled resources from the skill directory. Make the base explicit. No absolute user paths, secrets, transient ports, thread IDs or session state.
- Add scripts/, references/ or assets/ only when useful. Keep detailed references optional and linked directly from SKILL.md with read conditions. Scripts are for repeated deterministic work, not speculative automation; reuse existing tooling first.
- Prefer native skill discovery. Keep names/descriptions distinctive; do not build a second full skill catalog in the project index. A context document may link to a relevant skill without copying its procedure.
- Verify discovery/load in the actual installed agent/version when available. Folder placement alone does not prove activation. If native discovery is unavailable, read a relevant local SKILL.md explicitly under AGENTS routing; do not bypass denied access or change agent permissions to force loading.

## Size and progressive loading
- Before extending a skill, measure affected SKILL.md/reference Markdown files, counting all Unicode characters (including whitespace) and physical lines. Check final sizes before completion; do not scan unrelated skills.
- Split detailed content above 9,000 characters OR 150 lines when independently useful procedures/topics can be separated. Hard limit for each SKILL.md or reference: 12,000 characters OR 200 lines. Reorganize before exceeding either limit; a cohesive document above the split threshold may remain only below both hard limits.
- Keep SKILL.md as the activation and execution entry point: shared prerequisites/constraints, core steps/decisions, and direct relative resource links with concrete read conditions. Move specialized procedures/examples into descriptive references, or a separate skill only when independently activatable. No numbered chunks, per-class skills, duplicate procedure copies or chains of reference indexes.
- Load only references applicable to the current operation and discovered dependencies. Do not require every reference on activation. Preserve mandatory safety/verification constraints in the entry point or explicitly require the relevant reference before its operation.
- Check affected resource links and incoming references during a split. Preserve existing requirements and human-authored material; remove duplication/stale details before adding files. Apply PROJECT_CONTEXT_RULES for related project rule/context documents; do not copy their full routing tables into skills.

## Evidence and safety
- Record procedures supported by inspected implementation/tool documentation and actual executions where available. Distinguish inspected instructions from execution-tested steps; never describe an unrun workflow as verified. Do not publish a failed or speculative workaround as the standard path.
- For Unity/MCP, discover current capabilities and editor readiness at execution time. Refer to durable operations/contracts rather than hardcoded tool names or connection values unless verified as part of a stable project interface.
- Carry forward data preservation, owner lifetime, asset GUID and relevant regression checks through links to existing rules/context. A skill is not permission to delete user work, discard unsaved scenes, install packages, commit, publish or weaken checks.
- Skill updates must preserve independent verification: do not change expected behavior, remove failing checks or conceal failures to make an implementation pass.

## Maintenance and completion
- Check frontmatter, name/folder match, activation scope, resource links and documented project paths. Use an available compatible skill validator; otherwise validate these directly without adding a dependency.
- Run new/changed executable resources with representative safe inputs and failure cases. Avoid live production effects solely to test a skill. State any unavailable execution checks.
- For procedural-only updates, review against current source and available execution evidence; a full Unity test run is needed only when the changed workflow warrants it. Remove obsolete generated instructions/resources only after checking references; preserve unrelated authored material.
- Update affected project context references if skill names/paths change. Commit/push only when requested or already explicitly authorized by the project workflow; do not treat skill maintenance as new permission.
- Briefly report material skill changes and verification limits. Do not create progress logs, timestamps or per-task history inside skills.

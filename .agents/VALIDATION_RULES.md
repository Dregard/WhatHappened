# Validation
- Check the final diff against the task, agreed behavior/edge cases and applicable rules. For mechanic changes, check affected presentation, animations, VFX, audio, input, authored configuration and integrations for unintended regression.
- For new/reorganized domains, check ownership of code/assets, public-contract dependency direction and existing assembly constraints; no unintended duplicate assets or cyclic domain dependencies. Asset integrity follows UNITY_ASSET_RULES.
- Check whether context maintenance was triggered; update affected documents/index per PROJECT_CONTEXT_RULES and verify affected document/source paths. No new context is required when a small fix changes none of the documented facts.
- Run available relevant compilation/tests/project checks. No implementation-mirroring tests or tests for trivial formatting.
- Distinguish IDE/static checks, Unity compilation and runtime validation. Never claim success without a successful actual run.
- Report changes, checks performed, unresolved issues and manual Unity checks still needed.
- If the task changes a reusable workflow, check affected project skills and apply SKILL_MAINTENANCE_RULES; validate changed skill metadata/links and executable resources without claiming unrun procedural checks.

- Network checks follow NETWORK_RULES.md: actual multiplayer connections are manual-only; never launch real peers to satisfy automatic acceptance. Reuse prior evidence only under EXECUTION_EFFICIENCY.md.

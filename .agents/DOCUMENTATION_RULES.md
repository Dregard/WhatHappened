# Documentation
- XML comment immediately above every project-owned method, including interface methods and Unity callbacks. Generated methods/anonymous lambdas are exempt.
- Explain intent, relevant preconditions/invariants, side effects and return semantics; keep simple methods brief. Do not restate names or invent constraints.
- Async: explain completion, cancellation and lifetime ownership when relevant. State/group/input/serialized-data changes: explain caller-visible constraints.
- Update comments with behavior. Resolve discrepancies using intended requirements and code. No unverified guarantees (thread safety, no allocations, idempotence). Inline comments explain non-obvious reasoning only.
- Match final implementation/requirements; distinguish verified facts, assumptions, proposals and unchecked behavior. No secrets or machine-specific paths in reusable documentation.
- Preserve existing documentation language; otherwise English for code/docs, user's language in chat. Tooltips follow CONFIGURATION_RULES.
- Architecture/domain/project notes follow PROJECT_CONTEXT_RULES. No Docs/ARCHITECTURE.md or domain README solely for AI; user-requested human documentation may use the requested path.

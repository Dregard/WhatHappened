# AI-readable code
- A new agent should locate systems, understand contracts and change code without prior chats. Keep it maintainable for humans too.
- Use descriptive names, explicit dependencies, consistent domain layouts and discoverable entry points/public contracts.
- Prefer focused methods and straightforward control flow; avoid hidden side effects, unnecessary indirection, global mutable state and speculative abstractions.
- Keep cohesive code together; do not add forwarding wrappers or split classes solely for shortness. More files/interfaces/comments do not inherently help AI.
- Keep one source of truth for behavior/configuration.
- Discover through `.agents/project/INDEX.md` and matching documents, then entry points, contracts, code, call sites and connected assets. Expand to relevant dependencies; do not read the entire repository by default. Verify documentation against implementation.
- Keep domain terminology consistent across code, assets and context. Document important aliases where legacy names differ; avoid renaming serialized/public identifiers solely for vocabulary.
- A domain should reveal its owner, entry point, public surface and essential setup without prior chats. Record durable discoveries and changes through PROJECT_CONTEXT_RULES; avoid duplicate inventories or speculative context.
- Non-obvious invariants, async completion, input locks, lifecycle and serialized-state behavior follow DOCUMENTATION_RULES. Durable decisions/context follow PROJECT_CONTEXT_RULES.

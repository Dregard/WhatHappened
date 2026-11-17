# Architecture
## Domains and dependencies
- Reuse existing dependency injection, lifecycle, UI and signals; do not introduce a competing framework.
- No project Singletons, global mutable service instances or service-locator dependencies. Static constants/pure helpers and established third-party static APIs are allowed.
- A domain is a cohesive feature/subsystem, not every class. Each new domain has an entry point orchestrating initialization, lifecycle and public API; delegate specialized behavior.
- Follow an existing reference domain verified against current rules. If none exists, explain boundaries/entry point before substantial implementation; do not copy known legacy defects. Record useful reference paths in project architecture context.
- Define domain ownership, public contracts and dependency direction; avoid cyclic domain dependencies and access to another domain's internals. Infrastructure and shared utilities must not depend on concrete gameplay domains. Integrate domains at the composition root; use contracts/adapters where needed without speculative interfaces.
- Keep gameplay decisions separate from Unity presentation and external integrations where it improves clarity/testing. Do not impose aggregates, repositories, a layer stack or interfaces solely to claim DDD compliance. Code/asset placement follows HIERARCHY_RULES.
- Wire dependencies through the composition root/Installer or owning entry point. No component self-registration in global registries, locators or DI during construction/Unity callbacks.
- Explicit initialization may subscribe to events; teardown must unsubscribe. Cross-domain access uses intended public contracts/communication, not internal implementations.

## Extending behavior and ownership
- Before changing behavior, identify its authoritative owner from current code and these rules. Put feature rules and runtime state in that owner, not an unrelated System/manager merely because it receives input/events or has convenient references. Existing misplaced code is not a reference architecture.
- Entry points/orchestrators coordinate lifecycle and calls through public APIs; delegate specialized gameplay decisions, timers and state to the responsible feature. For example, a LocationSystem may gate gameplay during transitions or request a player reset; player attack execution, hit detection and attack cooldown belong to player combat.
- Extend an existing cohesive class; extract a focused collaborator when responsibilities differ. Do not create a class/interface for every small change or route every operation through a central System. Keep public entry points and dependency paths easy to discover.
- If the touched behavior is already misplaced, move the relevant behavior and its state together to the correct owner within task scope, preserving callers, serialized references and existing presentation. Avoid unrelated refactoring; report a concrete blocker instead of extending the ownership violation silently.
- Define timing semantics for cooldowns and other timed actions: what starts the timer, whether actions may overlap, and how pause/cancellation/failure affect state. Reuse established behavior; clarify material ambiguity. Do not let awaited target reactions/animations or a finally block accidentally determine cooldown start. Finally is for cleanup; gameplay timing there requires explicit intended semantics.
- Before finishing, check touched classes for unrelated feature rules/state, verify the public call path reaches the responsible owner, and verify preserved effects/animations and intended timing. Use focused inspection; these checks do not require a new test suite or whole-project scan.

## Passive UI
- Views display supplied state, expose interactions and handle presentation/layout/animation. Controllers/presenters decide and execute use cases.
- No business rules, persistence, progression or reward calculations in views. Follow existing UI naming/wiring.

## Signals
- Reuse existing `ISignal` and infrastructure; payloads are structs implementing it.
- Follow existing domain static `Signals` containers/names/payload conventions; no empty containers. Prefer immutable payloads; mutable collections require the existing contract to justify them.
- Nested payloads are permitted by HIERARCHY_RULES.

## Per-frame work and size
- In Update/FixedUpdate/LateUpdate/equivalent ticks: avoid repeated scene/dependency/component searches, blocking I/O and unnecessary allocations. Cache stable dependencies; pool/reuse buffers when justified.
- Necessary gameplay/physics/movement/camera calculations may run per frame. Moving expensive work to another callback does not solve its frequency/cost.
- Maximum target: 400 physical lines per project-owned C# file, including comments/blanks. Split by responsibility, not arbitrary count or partial-class workarounds. Request an explained exception before exceeding it. Third-party/generated code is exempt.

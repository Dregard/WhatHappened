# Predicted cooperative player movement
Read when changing player locomotion authority/ticks, input prediction, reconcile/replay, hook movement ownership, or diagnosing landing/stop corrections. Installed paths/settings are described in [movement context](project/DOMAIN_CoopMovement.md); verify them against current source.

## Simulation and inputs
- Use the installed FishNet API/version and compatible existing transport, broadcasts and TimeManager. Inspect actual NetworkObject, NetworkTransform, PredictionManager, TimeManager and smoothing settings before selecting an integration; absent components and defaults are evidence, not an assumed active prediction pipeline.
- A physical motor has one simulation owner and one fixed network step. Owner prediction, authoritative server stepping and replay use the same movement logic and tick duration. Host ownership must not step the same motor twice. Disable competing automatic physics/interpolation and transform synchronization while prediction owns it; preserve solo ownership on teardown.
- Send complete continuous input every tick, including zero after stopping. Missing horizontal/climb input is neutral; retain only explicitly supported held/counter state. Redundancy must not duplicate consumed commands or synthesize jump/dash presses. Preserve one-shot edge identity through packet loss, baseline and re-entry.
- Bound input history/queues. Incomplete history requires an authoritative baseline/epoch reset, never partial replay represented as complete correction. Do not mask errors through larger smoothing, tolerances or teleporting onto the ground.

## Restore and replay
- Capture a complete post-tick simulation state: position/rotation, velocity and vertical motion, grounding/unground state, jump state/grace/held/press counters, dash/wall timers, crouch dimensions, stamina, facing/speed, pending impulses/knockback and rope/climb/release/pull contributions.
- Restore capsule dimensions, motor state/constraints and gameplay modules before replay. Clear obsolete scene contact references and reacquire contacts/grounding in the shared motor phase before calculating velocity. Replay only commands after the acknowledged input tick. Verify moving contacts separately before claiming dynamic-platform determinism.
- Replay retains collision/contact and necessary gameplay simulation, but must not repeat sounds, particles, camera shake, UI or RPC/action dispatch. Separate authoritative gameplay commits from presentation; commit the final hook visual once after replay. Suppress duplicate hook-release notifications without suppressing explicit user release.
- Reject stale states/inputs across run, location, life, travel, recovery and forced movement ownership boundaries. A pending baseline/correction must not accept former-epoch reconcile. Takeover/release invalidates former rope operations and pending release momentum while preserving independent velocity where intended.

## Physics and presentation
- Correct the collider-bearing root exactly. Smooth only owned graphics leaves and a separate rope frame, preserving authored offsets/bindings. Visual interpolation must never move a collider-bearing ancestor; restore original bindings on teardown.
- Keep physical motor, combat/query capsule, remote renderer root and mesh bounds distinct during diagnosis. A graphics-only defect does not establish physical terrain penetration.
- Remote players display interpolation between ordered server-confirmed samples, holding at the newest sample during silence. No future simulation, velocity forecast or extrapolation. Reset buffers at lifetime/authority boundaries. Query geometry and hook endpoints must use their intended physical/visible frame consistently.
- Preserve wall slide/jump, dash collision, surface climbing, external pulls, knockback and door displacement through the shared simulation; do not add independent transform writers for these mechanics.

## Diagnostic order and focused acceptance
1. Classify the affected object/frame (physical motor, query collider, renderer root or mesh). Inspect actual runtime/serialized settings, installed API and every relevant position/rotation/velocity writer in Update, FixedUpdate, network ticks, Replicate/Reconcile, root motion, animation, hook and forced movement paths.
2. Compare matching ticks and roles before attributing a cause. Optional diagnostics are disabled by default and report tick, role, replay, position/velocity before and after restore/replay, grounded and correction magnitude. Distinguish raw restore delta from the final replay correction.
3. Run inspected isolated checks in order: flat walk/stop, jump/landing, then movement/jumps/climb/release with hook. Cover affected dash, wall slide, exact restoration, lifecycle boundaries, missing/zero/edge input, replay effects and physical/visual separation.
4. Follow [network rules](NETWORK_RULES.md) for safe checks and manual multiplayer acceptance. Compilation or isolated fixtures do not prove host/client, role-swap or latency/loss behavior. Preserve unknown test outcomes and use supported recovery without replaying an unresolved operation.

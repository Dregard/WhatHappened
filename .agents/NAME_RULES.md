# Naming
- Types use PascalCase. Domain entry points/high-level orchestrators end in `System`; subordinate behavior controllers use `Controller` where accurate, not every top-level class.
- Data containers, including ScriptableObject subclasses, must end in `Data` for configuration/authored data intended to remain unchanged during gameplay, or `Model` for state intended to change during gameplay. Classify by intended usage, not technical field writability; immutability includes referenced/exposed data. Do not substitute `Config`, `Settings` or `Configuration` suffixes for these containers. Other roles use descriptive project-consistent names. Preserve established infrastructure/third-party names; local naming habits do not override these suffix rules for new project-owned containers.
- Abstract classes start `Abstract`; interfaces start `I` + PascalCase.
- Methods/properties/events/constants/public fields: PascalCase. Private/protected fields: `_camelCase`; static fields follow access level. Parameters/locals: camelCase. Boolean names express conditions.
- Prefer encapsulation; naming does not justify public mutable state.
- Dictionaries: plural semantic name + `By{KeyMeaning}`, e.g. `_enemiesById`.
- Other collections: plural semantic name + declared-container suffix: List, Array, Set (HashSet), Enumerable (IEnumerable), Queue, Stack; e.g. `_enemiesList`. Other abstractions follow consistent existing examples; never infer runtime container type.
- Enums end `Alias`; members use PascalCase. Ordinary enums include `Unknown = -1`, `None = 0`, then distinct explicit values. Flags use None=0 and bit values; no automatic Unknown=-1 (all bits set). Preserve serialized values; no cosmetic renumbering.

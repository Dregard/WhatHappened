# C# formatting
- Use block bodies for methods, constructors, local functions and accessors with logic; no expression-bodied members. Simple auto-properties are allowed.
- Accessors stay lightweight/predictable: move I/O, expensive work and unrelated mutations to named methods.
- Lambdas are allowed for clear callbacks/subscriptions, including DOTween. Prefer named methods for complex callbacks or unsubscription requiring the same delegate.
- Explicit member access modifiers where supported. Ordinary interface contracts use explicit `public` if supported; no `protected` contract methods or default implementations added for formatting.
- Interface order: delegates, events, properties, methods. Nested contract delegates follow HIERARCHY_RULES.
- Otherwise preserve project indentation, braces, namespaces and member layout. Remove unused imports in touched code; no unrelated formatting.
- Method comments follow DOCUMENTATION_RULES; do not copy those requirements here.

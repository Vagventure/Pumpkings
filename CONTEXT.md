# Plastic Seeds Context

Plastic Seeds is a short 3D point-and-click cleanup game. This is the stable project-wide context and the first document agents should read. Do not load every linked document by default.

## Route From Here

- Use [Context Map](CONTEXT-MAP.md) to select additional domain context by task intent.
- Use [Code Map](CODEMAP.md) to locate runtime owners, editor tooling, and tests.
- Treat source code and Unity state as implementation truth; use these documents to find the smallest relevant working set.

## Current Domain

The player controls a 3D character by clicking walkable ground, removes spawned trash to lower current pollution and earn budget, spends budget on shop items that raise awareness, and reaches milestone events with narrative and optional reward choices.

## Vocabulary Rules

Use terms from [Glossary](docs/contexts/glossary.md). Prefer `progress event` over `tier`, `gold gathered` for gross income, `budget` for spendable currency, `threat produced` for cumulative generated pollution, and `current pollution` for the value that can rise, fall, and trigger loss.

## Stable Boundaries

- Runtime totals reset with the scene/run; there is no required cross-session persistence.
- Loss is based on current pollution, not cumulative threat produced.
- Scene and prefab wiring is explicit Unity state and must be verified when relevant.
- Focused context belongs under `docs/contexts/`; routing and cross-system summaries belong under `docs/context-map/`.

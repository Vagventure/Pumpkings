# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- `CONTEXT.md` at the repo root for stable project-wide context.
- `CONTEXT-MAP.md` at the repo root to route by task intent to only the relevant focused documents.
- `CODEMAP.md` at the repo root to locate the smallest relevant source, editor, and test working set.
- ADRs under `docs/adr/` only when they touch the area you're about to work in.

If any of these files don't exist, proceed silently. Don't flag their absence; don't suggest creating them upfront. The producer skill (`/grill-with-docs`) creates them lazily when terms or decisions actually get resolved.

## File structure

This is one game domain with hierarchical, topic-focused retrieval:

```text
/
|- .codex/agents/
|- CONTEXT.md
|- CONTEXT-MAP.md
|- CODEMAP.md
|- Assets/
|  |- Scripts/
|  `- Tests/EditMode/
|- docs/
|  |- contexts/
|  |- context-map/
|  |- code-map/
|  |- agents/
|  `- adr/
`- .scratch/
```

Roles:

- `.codex/agents/` defines project-scoped custom agents such as the read-only `spark_retriever`.
- `CONTEXT.md` is the small, stable entry context.
- `CONTEXT-MAP.md` routes task signals to focused domain documents.
- `CODEMAP.md` routes task areas to detailed maps under `docs/code-map/`.
- `docs/contexts/` explains one domain or presentation topic in depth.
- `docs/context-map/` records cross-system flows, assumptions, communication, and Unity wiring checks.
- `docs/code-map/` locates runtime owners, supporting files, custom editors, and tests for one task area.
- `docs/agents/unity-mcp-routing.md` caches Unity MCP operation routing while live session resources remain authoritative.
- `docs/adr/` records durable decisions.

Do not infer that the presence of `CONTEXT-MAP.md` means there are multiple bounded contexts. It is a retrieval router for this single game domain.

## Retrieval discipline

```text
CONTEXT.md -> CONTEXT-MAP.md route -> CODEMAP.md route -> selected docs/code/tests
```

Expand to another route only when the selected implementation crosses a system seam. Source code and live Unity state win when a routing document is stale; update the stale document as part of finishing the task.

## Maintenance

After every task, review whether domain behavior, terminology, ownership, event flow, Unity wiring, paths, custom editors, tests, or routing changed. Edit only the affected focused document and detailed code map. Edit root maps only when their routes changed; otherwise leave all documentation untouched. Keep maps concise and remove obsolete routes rather than accumulating historical inventories.

## Use the glossary's vocabulary

When your output names a domain concept, use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal - either you're inventing language the project doesn't use or there's a real gap worth noting for `/grill-with-docs`.

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding it.

## Agent skills

### Git commits

Agents must not create Git commits in this repository. Leave all changes uncommitted in the working tree for the user to review and commit manually.

### Issue tracker

Issues for this repo are tracked as local markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

This repo uses the default triage labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

This repo uses a single-context documentation layout. See `docs/agents/domain.md`.

### Context routing and code map maintenance

Use the repository documentation as hierarchical retrieval rather than loading every context file:

1. Read root `CONTEXT.md` for the stable project-wide context.
2. Use root `CONTEXT-MAP.md` to select only the additional context documents relevant to the task.
3. Use root `CODEMAP.md` to select one detailed map under `docs/code-map/`, then locate runtime owners, supporting files, editor tooling, and tests.
4. Read source files directly for implementation truth. Context documents and maps are routing aids, not substitutes for verification.

After every task, agents must review whether the work changed domain behavior, terminology, system ownership, event flow, Unity wiring expectations, file locations, custom editors, or test locations. Edit documentation only when one of those facts or its routing changed. Update the affected focused context and detailed code map; update root `CONTEXT-MAP.md` or `CODEMAP.md` only when their routes changed. If nothing relevant changed, leave all maps untouched. Do not add task history or refresh timestamps. Verify changed links and paths before finishing.

### 5.3 Spark support agent

Agents may, and are encouraged to, use a 5.3 Spark sub-agent as a fast, token-efficient support agent when that model is available. Its purpose is to reduce the main agent's context and token usage through quick scanning and information retrieval.

When the Codex client supports named custom agents, use the project-scoped `spark_retriever` agent from `.codex/agents/spark_retriever.toml` for this work. That agent pins `gpt-5.3-codex-spark` and read-only sandboxing. If named custom-agent selection is unavailable, a generic sub-agent may follow the same contract, but the main agent must treat its backend model as unverified.

Good tasks for the 5.3 Spark sub-agent include:

- locating relevant files, symbols, references, and documentation;
- reporting file and directory metadata such as paths, sizes, counts, timestamps, and other useful inventory information;
- identifying which tools or Unity MCP operations are likely to be needed before the main agent acts;
- reading and analysing a file when it is small enough to do so efficiently;
- returning concise facts, candidate locations, and summaries to support the main agent.

Spark must use a metadata-first pass before reading file contents. Its default report is 300-600 tokens and contains only:

- exact paths plus size, line count, and useful timestamps when available;
- relevant symbols and references;
- related tests and custom editors;
- recommended files or Unity MCP resources/tools for the main agent to inspect;
- concise verified facts or explicit uncertainty.

Spark must not repeat the task, narrate its process, paste raw code or large tool output, provide a full implementation plan, or restate facts already supplied by the main agent. It may analyse a small file when efficient or when explicitly requested. Give metadata/retrieval sub-agents a narrow brief with only the context required for that scan.

For Unity MCP reconnaissance, Spark should prefer the cached `docs/agents/unity-mcp-routing.md` map plus cheap live readiness/resources. Limit a live MCP reconnaissance attempt to roughly 10 seconds per request and 20 seconds total. On timeout or connection failure, do not retry: report the cached route, mark live state unverified, and return control to the main agent. The main agent performs consequential Unity MCP operations.

The main agent remains responsible for programming, edits, complete or high-stakes analysis, architectural decisions, verification, and the final answer. Treat Spark's output as supporting evidence and verify it in proportion to the risk of the task.

### Unity MCP

Unity MCP is expected to be available as a Streamable HTTP MCP endpoint at `http://127.0.0.1:8080/mcp`. The `/mcp` route returns MCP protocol responses, not a web page, so agents must connect through an MCP client and initialise a JSON-RPC session rather than treating the endpoint as a browser URL.

Read `docs/agents/unity-mcp-routing.md` for the cached operation-to-resource/tool map. Do not rediscover the complete tool schemas on every task. At the start of each MCP session, still read the live `mcpforunity://instances`, `mcpforunity://custom-tools`, and `mcpforunity://editor/state` resources because instances, project extensions, and readiness can change.

To reduce repeated setup work and accelerate the start of Unity tasks, agents should initialise the Unity MCP JSON-RPC session and, in parallel, read only the relevant domain documentation for spawning, progress events, and UI, together with the existing EditMode test patterns. Avoid broad documentation or project scans unless the task requires them.

Agents should use Unity MCP whenever it is available and relevant to inspecting, operating, or verifying the Unity project. A 5.3 Spark sub-agent may first identify suitable Unity MCP tools or operations and retrieve the narrowly scoped context above, but the main agent remains responsible for consequential actions, implementation, and final verification.

Agents do not need to ask the user for permission merely to connect to or use Unity MCP. Proceed directly with read-only inspection, editor-state queries, console reads, targeted test execution, Play Mode verification, and other normal in-scope verification. MCP mutations that are a normal implementation step inside the user's requested scope may also proceed without a separate MCP-specific confirmation. This does not expand task scope: continue to follow the explicit restriction on automatic `.unity` scene and `.prefab` file changes, and request direction when a consequential mutation requires authority not already provided by the task.

### Unity setup notes

### Proportional verification

Match verification depth to task size, risk, and number of systems affected.

For a short, localized, low-risk task, the agent may stop after the smallest relevant static or targeted check. The final response may simply state that the change is done, disclose that Play Mode was not run, and ask the user to verify the named behavior in Play Mode. Do not run a full Unity verification flow merely by default, and never claim that unperformed verification passed.

For a large, multi-step, cross-system, or high-risk task, perform full relevant verification before finishing. This normally includes targeted tests, compilation and console checks, Unity MCP inspection, required scene/Inspector wiring checks, and Play Mode behavior when the result depends on runtime lifecycle, coroutines, timing, physics, NavMesh, UI interaction, audio, animation, scene state, or integration between systems. Report exactly what was run and any remaining manual checks.

Escalate from the short flow to the full flow whenever a failure could be hidden by compilation alone, the change crosses more than one runtime owner, or the task explicitly requests complete verification.

Whenever an agent changes code in a way that requires Unity Inspector or scene/prefab wiring, the final response must include a `Unity setup` section with concrete steps for what to assign, create, or verify in Unity.

When adding, renaming, or removing serialized fields on a Unity component, always check for a matching custom editor under `Assets/Scripts/Editor/` before claiming the field is available in the Inspector. If a `[CustomEditor]` draws fields manually through `OnInspectorGUI`, update that editor to `FindProperty` and `PropertyField` the new serialized field. This repo has already hit hidden-field bugs from changing runtime scripts without updating their custom inspectors.

Do not edit Unity scene (`.unity`) or prefab (`.prefab`) files automatically unless the user explicitly asks for scene/prefab file changes. Prefer code changes plus concrete Unity setup instructions.

For PRD coding tasks, first read the relevant task and domain context before editing code. In the final response, always end with a brief `Unity setup` section, even if it only says that no Unity scene, prefab, or Inspector changes are needed.

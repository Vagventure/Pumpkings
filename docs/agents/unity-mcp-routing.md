# Unity MCP Routing

Cached routing for `mcp-for-unity-server` observed on 2026-07-15: server `3.4.4`, protocol `2025-03-26`, Unity `6000.4.6f1`, project `Pumpkins`, WebGL target. Refresh this document only when the server, tool groups, or useful operation routes change.

## Connect Once Per Session

Endpoint: `http://127.0.0.1:8080/mcp` using Streamable HTTP.

Using Unity MCP does not itself require a separate user confirmation. Read-only inspection, console checks, targeted tests, Play Mode verification, and normal in-scope operations may proceed directly. Separate authorization is needed only when the intended mutation exceeds the task's scope; automatic scene and prefab changes remain restricted by root `AGENTS.md`.

1. POST JSON-RPC `initialize` and retain the `Mcp-Session-Id` response header.
2. POST `notifications/initialized` with that session header.
3. Read the exact live resource URIs below; do not treat `/mcp` as a web page.
4. If multiple instances exist, call `set_active_instance` with the exact `Name@hash`. Do not cache an instance hash between editor runs.

## Live Session Reads

| Need | Resource URI | Notes |
| --- | --- | --- |
| Connected editor instance(s) | `mcpforunity://instances` | Read every session; current observation had one `Pumpkins` instance. |
| Readiness, compilation, play mode, active scene | `mcpforunity://editor/state` | Require `data.advice.ready_for_tools`; payload state lives under `data`. |
| Project root, Unity version, platform | `mcpforunity://project/info` | Stable but cheap. |
| Project-scoped capabilities | `mcpforunity://custom-tools` | Read every session before assuming project extensions. |
| Available/active tool groups | `mcpforunity://tool-groups` | Activate optional groups with `manage_tools`. |
| Tests inventory | `mcpforunity://tests` | First page only; use test tools for filtering/runs. |
| Tags and layers | `mcpforunity://project/tags`, `mcpforunity://project/layers` | Read before mutation. |
| Menu commands | `mcpforunity://menu-items` | Read before `execute_menu_item`. |
| Scene GameObject resource rules | `mcpforunity://scene/gameobject-api` | Use exact dynamic URIs described there; never invent them. |
| Prefab resource rules | `mcpforunity://prefab-api` | Read before prefab resource inspection. |

## Operation Routing

| Operation | Preferred resource/tool | Token-saving defaults |
| --- | --- | --- |
| Check Unity readiness | `mcpforunity://editor/state` | Read before consequential tools and after refresh/compile. |
| Find scene objects | `find_gameobjects` | Return IDs first; fetch details only for selected IDs. |
| Inspect hierarchy | `manage_scene` with hierarchy query | Start around 50 items/page and follow cursor only as needed. |
| Inspect components | GameObject component resource described by `gameobject-api` | Metadata/properties off first; request properties only for selected components. |
| Search assets | `manage_asset` search | 25-50 items/page; `generate_preview=false`. |
| Inspect/modify scene objects | `manage_gameobject`, `manage_components`, `manage_scene` | Scene/prefab mutations still require explicit user scope under `AGENTS.md`. |
| Inspect/modify prefabs | Prefab resources, `manage_prefabs`, `manage_asset` | Read prefab resource rules first. |
| Script search | `find_in_file`, asset/file resources | Bound result count. |
| Script mutation through MCP | `script_apply_edits`, `apply_text_edits`, `manage_script` | Prefer structured edits; repository agents may still use local `apply_patch`. |
| Refresh after script changes | `refresh_unity` -> editor state -> `read_console` | Wait for compilation/domain reload before using new types. |
| Console diagnosis | `read_console` | Filter errors/warnings and bound count. |
| Run tests | Activate `testing`; `run_tests` -> `get_test_job` | Filter test names; keep details off unless failed. Use 30-60s job wait when useful. |
| Verify Unity APIs | Activate `docs`; `unity_reflect` -> `unity_docs` | Reflect existence/signature before fetching documentation. |
| Repetitive operations | `batch_execute` | Prefer one batch; current editor reports max 25 commands. Parallelize read-only commands only. |
| Profiling | Activate `profiling`; `manage_profiler` | Capture only relevant counters/frames. |
| UI Toolkit | Activate `ui`; `manage_ui` | This group is for UI Toolkit, not general uGUI inspection. |
| Animation | Activate `animation`; `manage_animation` | Use for Animator/controller/clip operations. |
| VFX/shaders/textures | Activate `vfx`; `manage_vfx`, `manage_shader`, `manage_texture` | Search existing project assets first. |
| Build | `manage_build` | Poll long-running jobs; use only when build verification is in task scope. |

## Default Tool Groups

`core` is enabled by default. Optional groups observed: `animation`, `asset_gen`, `docs`, `probuilder`, `profiling`, `scripting_ext`, `testing`, `ui`, and `vfx`. Activate only the group required for the task.

## Refresh Conditions

Refresh the cached map when MCP reports `tools.listChanged`, a routed tool/resource is missing, server version changes, or a project custom tool materially changes the preferred workflow. A new session or instance hash alone does not require editing this document.

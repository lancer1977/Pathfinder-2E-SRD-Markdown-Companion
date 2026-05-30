# MudBlazor RAG Chat UI

## Summary

This feature replaces the Python `rpg-chat-ui` implementation with a C# Blazor Web App using MudBlazor.

The app should preserve the original behavior:

- build an index from markdown files
- retrieve relevant chunks with semantic scoring
- generate chat responses through Ollama
- return citations and source previews
- store chat history and pinned answers

It also adds a standardized repository shape:

- `docs/` for feature and roadmap tracking
- `src/` for the app code
- `scripts/` for fast local tasks, Docker, and database refresh
- `pub.sh` for GHCR image publishing
- `.github/workflows/` for deployment automation
- `Dockerfile` and `docker-compose.yml` for container deployment

The corpus refresh flow now also supports selective imports:

- whole-repo refresh as an opt-in
- root-based selective import for faster tests and local development
- bestiary/enemy content can be excluded when not needed
- presets are available for common refresh scopes

## Status

- [x] Identify the source behavior to preserve
- [x] Choose the new UI stack
- [x] Scaffold the Blazor app
- [x] Implement the corpus refresh pipeline
- [x] Implement the chat experience
- [x] Add deployment workflows
- [x] Add selective corpus import controls
- [x] Add preset import modes
- [x] Validate the full stack against live Ollama and a fresh repo clone (passed with `RUN_LIVE_CORPUS_TESTS=1 OLLAMA_URL=http://127.0.0.1:11434 dotnet test tests/PathfinderRagChatUi.Tests/PathfinderRagChatUi.Tests.csproj --filter FullyQualifiedName~Live_Companion_Repo_Rag_Path_Requires_OptIn -p:RestoreIgnoreFailedSources=true`)

## Assumptions

- The app is single-host and self-contained.
- The markdown corpus is refreshed from the Obsidian TTRPG Community Pathfinder repository before indexing.
- SQLite is the local durable store.
- Ollama remains the LLM backend.
- MudBlazor handles the app shell and chat UI.
- Docker is the deployment target.

## Behavior To Preserve

- streaming chat output
- citations attached to each answer
- source preview access
- local conversation history
- pinned answers with optional notes
- index rebuild progress feedback

## Behavior To Improve

- a cleaner C# service layout
- explicit DTOs and options objects
- a repeatable database refresh path
- selective import for faster local and test refreshes
- preset-driven refresh scopes for common workflows
- a named no-bestiary preset for rules and compendium
- deployment-friendly scripts
- docs that distinguish current behavior from planned work

## Implementation Shape

The first pass will keep the app in a single `src/PathfinderRagChatUi` project. The services, UI components, and API endpoints will live together there until a split is clearly justified.

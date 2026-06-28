# Project Atlas

## Purpose

Pathfinder-2E-SRD-Markdown-Companion is a MudBlazor/Blazor rules chat application that builds a local SQLite-backed RAG corpus from the Pathfinder 2E SRD Markdown repository.

## Project Map

| Path | Role | Validation |
| --- | --- | --- |
| `src/PathfinderRagChatUi` | Blazor Web App, MudBlazor UI, chat, corpus refresh, and persistence | `dotnet test PathfinderRagChatUi.sln` |
| `tests/PathfinderRagChatUi.Tests` | Unit and integration-style tests for refresh/chat/storage seams | `dotnet test PathfinderRagChatUi.sln` |
| `scripts/` | Local run, refresh, Docker, and publish helpers | Operator smoke per script purpose |
| `Dockerfile` and `docker-compose.yml` | Container build and local deployment shape | Docker build/run checks |
| `docs/roadmaps/` | Migrated roadmap mirrors that point to GitHub Issues | GitHub issue state |

## Validation Entry Points

Primary local gate:

```bash
dotnet test PathfinderRagChatUi.sln
```

Opt-in live corpus/RAG path:

```bash
RUN_LIVE_CORPUS_TESTS=1 OLLAMA_URL=http://127.0.0.1:11434 dotnet test tests/PathfinderRagChatUi.Tests/PathfinderRagChatUi.Tests.csproj --filter FullyQualifiedName~Live_Companion_Repo_Rag_Path_Requires_OptIn -p:RestoreIgnoreFailedSources=true
```

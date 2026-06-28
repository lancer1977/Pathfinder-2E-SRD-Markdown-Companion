# Pathfinder 2E SRD Markdown Companion

MudBlazor-based C# replacement for the original `rpg-chat-ui` prototype.

## Tags

- rpg
- pathfinder-2-e-srd-markdown-companion
- dotnet
- tabletop
- pathfinder
- docker

## Goal

Provide a self-hosted rules chat app that:

- pulls the Pathfinder SRD Markdown repository fresh
- builds a local searchable database from the markdown corpus
- serves a polished browser UI with MudBlazor
- supports streaming chat answers with citations
- stores history and pins locally
- includes helper scripts and GitHub deployment automation

## Current Status

- [x] Document the target shape
- [x] Scaffold the .NET solution
- [x] Implement the C# Blazor/MudBlazor app shell
- [x] Implement corpus refresh, SQLite storage, and Ollama integration
- [x] Implement chat, citations, history, and pins
- [x] Add deployment workflows and helper scripts
- [x] Add unit tests and an opt-in live corpus test
- [x] Validate refresh against a live Ollama instance and the fresh GitHub corpus clone (passed with `RUN_LIVE_CORPUS_TESTS=1 OLLAMA_URL=http://127.0.0.1:11434 dotnet test tests/PathfinderRagChatUi.Tests/PathfinderRagChatUi.Tests.csproj --filter FullyQualifiedName~Live_Companion_Repo_Rag_Path_Requires_OptIn -p:RestoreIgnoreFailedSources=true`)

## Planned Stack

- .NET 10 / ASP.NET Core 10
- Blazor Web App
- MudBlazor
- Ollama for embeddings and generation
- Seq for structured logging
- SQLite for local persistence
- Docker for deployment and packaging

## Source Corpus

The app will refresh from:

- `https://github.com/Obsidian-TTRPG-Community/Pathfinder-2E-SRD-Markdown`

That repo is treated as the source of truth for rebuilding the database.

## Docker Deployment

The app is now container-first:

- `Dockerfile` builds the app image
- `docker-compose.yml` runs the app locally
- GitHub Actions pushes images to GHCR
- helper scripts in `scripts/` handle build, refresh, up, and down
- Ollama defaults to `http://192.168.0.252:11434`
- Seq defaults to `https://seq.polyhydragames.com`
- Seq API key is loaded from local `.env` via `Seq__ApiKey`

Useful commands:

```bash
./scripts/docker-build.sh
./scripts/docker-up.sh
./scripts/docker-refresh.sh
./scripts/docker-down.sh
./pub.sh
```

`./pub.sh` builds the container image and pushes it to `ghcr.io/lancer1977/pathfinder-2e-srd-markdown-companion` as `latest` by default. Pass a version tag to publish a second immutable tag:

```bash
./pub.sh main
```

The publish helper also loads optional local secrets from `~/.config/secrets/ghcr.env` and `~/.config/secrets/polyhydra.env`.

## Docs

- [Docs Index](./docs/README.md)
- [Feature docs](docs/features/mudblazor-rag-chat-ui/README.md)
- [Portfolio roadmap](./docs/roadmaps/portfolio-roadmap.md)
- [Roadmap](docs/roadmaps/csharp-mudblazor-port-and-deployment/README.md)

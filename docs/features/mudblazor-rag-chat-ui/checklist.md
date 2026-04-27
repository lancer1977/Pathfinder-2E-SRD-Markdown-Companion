# Checklist

## Discovery

- [x] Inspect the original Python app
- [x] Identify the current repo state
- [x] Confirm the MudBlazor direction

## Documentation

- [x] Add a feature summary
- [x] Add a status checklist
- [x] Capture assumptions
- [x] Capture preserved behavior

## Scaffolding

- [x] Create the .NET solution
- [x] Create the Blazor Web App project
- [x] Add MudBlazor package wiring
- [x] Add app configuration

## Corpus Refresh

- [x] Clone the Pathfinder markdown repo fresh
- [x] Normalize markdown files into records
- [x] Generate SQLite tables for chunks and metadata
- [x] Rebuild embeddings through Ollama
- [x] Support selective import and enemy opt-out
- [x] Add presets for common import scopes
- [x] Add a compendium-only preset
- [x] Add an explicit rules + compendium no-bestiary preset

## Chat

- [x] Add chat input and streaming output
- [x] Add citation cards and source previews
- [x] Persist conversation history
- [x] Persist pins with notes

## Deployment

- [x] Add helper scripts for local refresh and run
- [x] Add GitHub Actions workflow(s)
- [x] Add publish/deploy instructions
- [x] Add Dockerfile and compose support
- [x] Add a containerized refresh helper

## Testing

- [x] Add unit tests for the chunker and refresh/RAG path
- [x] Add an opt-in live corpus test
- [x] Keep tests narrow by default using selective import
- [x] Verify the test suite passes locally

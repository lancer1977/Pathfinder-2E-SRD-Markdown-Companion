# Architecture

## Proposed Shape

- `src/PathfinderRagChatUi`
  - Blazor Web App host
  - MudBlazor shell and pages/components
  - corpus, retrieval, and chat services
  - DTOs and options
  - SQLite access helpers
  - API endpoints for chat, refresh, history, and pins
- `scripts/`
  - one-shot database refresh
  - local run helpers
  - publish/deploy helpers
- Seq receives structured logs from the ASP.NET Core host when configured.
- `.github/workflows/`
  - build and deploy automation

## Data Flow

1. Refresh script or endpoint clones the Pathfinder markdown repository fresh.
2. Markdown files are parsed into chunk records.
3. The app asks Ollama for embeddings in batches.
4. Chunk records and embeddings are written to SQLite.
5. Chat requests embed the user query, score chunks, and assemble context.
6. Ollama generates the answer from the selected context.
7. The app returns the response with citations and writes history.
8. Pins reference historical records and are stored separately.

## UI Notes

- Use MudBlazor for the shell, navigation, cards, dialogs, and tables.
- Keep the chat transcript readable and responsive on mobile.
- Preserve citation previews and source inspection.
- Keep the visual language intentionally distinct from the original Python prototype.

## Service Notes

- Keep the business logic in services, not page components.
- Use typed options for corpus, Ollama, and storage settings.
- Use a single durable SQLite database for the app state.
- Use background work only where it materially improves refresh or indexing UX.

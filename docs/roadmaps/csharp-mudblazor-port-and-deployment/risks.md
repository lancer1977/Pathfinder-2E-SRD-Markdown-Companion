# Risks

## Technical

- Ollama availability and model choice may vary across machines.
- The markdown corpus may be large enough to make refresh time noticeable.
- SQLite size and embedding payloads may grow quickly.
- MudBlazor and Blazor hosting details can affect render behavior if not configured cleanly.

## Operational

- GitHub Actions may need secrets or environment-specific deployment details.
- A fresh clone of the source repository depends on network access during refresh.
- Local helper scripts need clear environment requirements to avoid brittle setups.

## Product

- Multiple corpora could complicate the first release if introduced too early.
- Too much UI polish before the core retrieval path works could slow delivery.


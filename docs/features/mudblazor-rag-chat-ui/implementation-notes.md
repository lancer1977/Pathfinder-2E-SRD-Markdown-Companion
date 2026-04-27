# Implementation Notes

## Source Behavior Mapped In

- `build/rebuild index`
- `stream chat`
- `list indexes`
- `list history`
- `pin answer`
- `view source chunk`

## Expected C# Translation

- replace ad hoc JSON files with SQLite tables
- replace Python threading jobs with hosted/background services or tracked refresh jobs
- replace browser template + inline JS with Blazor components and MudBlazor primitives
- keep the API contract stable enough for a browser client to remain simple

## Fresh Corpus Refresh

- the refresh should start from a clean checkout of the source repository
- the database should be regenerated from markdown, not incrementally patched in place
- refresh state should be visible to the UI or an API consumer
- the canonical markdown source is `https://github.com/Obsidian-TTRPG-Community/Pathfinder-2E-SRD-Markdown`

## Practical Constraints

- keep the first implementation understandable
- avoid over-engineering the retrieval layer
- do not split the code into too many projects unless a boundary is clearly pulling its weight

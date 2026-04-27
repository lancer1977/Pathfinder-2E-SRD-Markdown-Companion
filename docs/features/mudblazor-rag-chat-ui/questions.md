# Questions

## Open Decisions

- [ ] Should chat history and pins be per corpus or global?
- [ ] Should the app support multiple corpora or stay focused on Pathfinder first?
- [ ] Should the database refresh run on a schedule or only on demand?
- [ ] Should deployment target a VM, container, or GitHub artifact workflow?
- [ ] Should the repo clone use `git` directly or a zip download from GitHub?

## Current Assumptions

- Pathfinder is the primary and likely only initial corpus.
- On-demand refresh is sufficient for the first pass.
- GitHub Actions should build and publish the app artifact.


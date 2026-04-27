# Selective Corpus Import

## Summary

The corpus refresh flow supports selective indexing of the Pathfinder markdown repository instead of always importing the entire repo.

Default behavior:

- import the structured gameplay roots that power the app
- keep whole-repo import available as an opt-in
- allow the fantasy-bestiary/enemy content to be excluded from a refresh
- offer presets for common import scopes

This is useful for:

- faster local refreshes and tests
- smaller development databases
- avoiding enemy content when the current session does not need it

## Status

- [x] Add request shape for selective import
- [x] Support whole-repo opt-in
- [x] Support bestiary opt-out
- [x] Wire the MudBlazor UI controls
- [x] Update the unit and live tests to use selective import
- [x] Verify the solution builds and tests pass locally

## Behavior

- selective import defaults to the app-configured roots
- the UI can import the whole repository when needed
- the UI can skip `fantasy-bestiary`
- the UI offers presets for `Rules only`, `Compendium only`, `Rules + compendium (no bestiary)`, `Default selective`, and `Whole repo`
- the API accepts import requests directly

## Notes

- The canonical markdown source remains the Obsidian TTRPG Community Pathfinder repository.
- The live tests intentionally use a narrow subset of the repo to keep feedback fast.

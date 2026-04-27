# Checklist

## Discovery

- [x] Confirm the repo contains separate `compendium`, `rules`, and `fantasy-bestiary` roots
- [x] Decide to support selective import rather than forcing full-repo refreshes

## Documentation

- [x] Add a dedicated feature summary
- [x] Capture the whole-repo opt-in behavior
- [x] Capture the fantasy-bestiary opt-out behavior

## Backend

- [x] Add a request DTO for refresh selection
- [x] Make the refresh service honor include and exclude roots
- [x] Keep the API endpoint accepting an optional request body

## UI

- [x] Add MudBlazor controls for import scope
- [x] Add a checkbox to exclude enemy content
- [x] Add preset import modes for common scopes
- [x] Add a compendium-only preset
- [x] Add an explicit rules + compendium no-bestiary preset
- [x] Guard against an empty selective import

## Testing

- [x] Update the local corpus test to use selective import
- [x] Add a test for bestiary opt-out
- [x] Keep the live smoke test narrow and explicit
- [x] Verify `dotnet build` and `dotnet test`

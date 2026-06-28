# Code Health

## Native Validation

Run the repo-native test gate from the repository root:

```bash
dotnet test PathfinderRagChatUi.sln
```

The current SDK requires `Directory.Build.props` to set `AllowMissingPrunePackageData=true`; without it, restore/test fails with `NETSDK1226` for `.NETCoreApp 10.0 Microsoft.AspNetCore.App` prune package data.

## Current Results

- `dotnet test PathfinderRagChatUi.sln`: passed, 5 tests.
- `devstudio validate --repo .`: passed after adding this health file and the project atlas.

Known warnings:

- `NU1903` for `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.
- MudBlazor analyzer warnings for legacy `Checked`/`CheckedChanged` attributes in `Home.razor`.

## Runtime Notes

- Live corpus refresh and Ollama-backed RAG validation remain opt-in because they require network access, a local Ollama host, and model availability.
- `.devstudio/runtime/` and `.devstudio/work/` are generated local state and intentionally ignored.

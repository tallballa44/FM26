# Patch 0.1 - Diagnostic Build

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.1  
Status: source-ready, not yet FM-tested

Patch 0.1 deliberately does **not** change export routing.

## Changes

- Restores F8 as a working UI diagnostic hotkey.
- F8 enumerates candidate `column-headers` + `View` tables.
- Reports container name, visibility, rendered rows, selected rows, headers, and a basic classification.
- Translates user-facing BepInEx logging in the core exporter, configuration, Calendar exporter, Match Stats exporter, and generic list exporter into English.
- Preserves 5.1 handler order, Staff validation, Player fallback behavior, incremental CSV/HTML writing, row limits, and scrolling behavior.
- Adds a local-build project file that references the installed FM26/BepInEx assemblies.

## Test sequence

1. Build and install the patched DLL.
2. Launch FM26 and confirm the console says `FM26Export Patch 0.1`.
3. On Squad, press F8 and save the console output.
4. On Player Search, press F8 and save the console output.
5. On Staff Search with rows selected, press F8 and save the console output.
6. Staff F9 routing is intentionally unchanged in Patch 0.1.

For Staff Search, the key result is whether F8 shows the unrelated standings table and another table whose headers include `Person` and `Preferred Job`.

# FM26 Player Export Fork

This repository is the development home for a patched FM26 Player Export build.

## Current stable fork

The validated fork is **FM26 Player Export 5.1.6 (Patch 0.6)**.

Validated behavior includes:

- F8 UI diagnostics.
- Reliable Staff Search table detection.
- Selected-only Staff Search exports.
- Protection against standings tables being claimed by the Player handler.
- Existing Player Search export behavior preserved.
- Incremental CSV/HTML output, scrolling, row limits, Calendar export, and Match Stats export retained from the 5.1 baseline.
- User-facing BepInEx logging translated to English.

## Branches

- `main` — stable validated fork.
- `baseline-5.1` — preserves the exact original 5.1.0 DLL used as the recovery baseline.
- `dev/table-routing` — development history for the table-routing and staff-export work.

## Source layout

- `src/decompiled-5.1/` — untouched ILSpy recovery of the original 5.1.0 DLL.
- `src/patched-5.1/` — maintained patched source.
- `tools/` — local Windows build scripts.
- `docs/` — patch notes and test history.

## Build

For the current stable source, run:

`tools\Build-Patch-0.6.cmd`

The DLL is written to:

`dist\patch-0.6\FM26PlayerExport.dll`

The build output is intentionally ignored by Git.

## Safety rule

Never overwrite the preserved baseline DLL in the repository. Experimental changes should be tested against Squad, Player Search, and Staff Search before being promoted to `main`.

## Baseline binary

The exact original binary is stored at:

`original/FM26PlayerExport_original.dll`

Assembly metadata identifies it as FM26 Player Export 5.1.0.

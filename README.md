# FM26 Player Export Fork

This repository is the development home for a patched FM26 Player Export build.

## Branches

- `baseline-5.1` — preserves the exact known-good 5.1 DLL currently used in FM26.
- `dev/table-routing` — active development branch for diagnostics, table selection, staff export, and English logging.
- `main` — remains untouched until a patched build is proven stable.

## Goals

1. Preserve all working 5.1 behavior.
2. Restore F8 as a real diagnostic UI rescan.
3. Enumerate all candidate FM26 tables instead of accepting the first plausible one.
4. Prefer visible tables with selected rows.
5. Make Staff Search detection reliable.
6. Prevent PlayerExportHandler from claiming unrelated tables such as standings.
7. Translate user-facing BepInEx console output to plain English.
8. Keep player, calendar, match-stat, CSV, HTML, scrolling, and row-limit behavior unchanged unless a test proves a change is required.

## Safety rule

Never overwrite the baseline DLL. Experimental builds should use a distinct version string and should be tested against Squad, Player Search, and Staff Search before merging.

## Baseline binary

The exact original binary is stored at:

`original/FM26PlayerExport_original.dll`

Assembly metadata identifies it as FM26 Player Export 5.1.0.

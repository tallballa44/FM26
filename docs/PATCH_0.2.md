# Patch 0.2 - Staff Search Routing Fix

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.2  
Status: ready for local build/test

## Evidence from Patch 0.1

F8 diagnostics on Staff Search found two visible table candidates:

1. Standings table:
   `Team | Pld | W | D | L | GD | Pts`
2. Staff table:
   `Status | Person | Preferred Job | Club | Goalkeeping | Attacking | Defending | Fitness | Possession | Technical | Tactical | Set Pieces | Working With Youngsters`

The staff table container was reported as `nonplayertable`.

## Changes

- Generic list routing now enumerates all table candidates rather than committing to the first plausible table.
- Visible candidates are preferred.
- A rejected candidate no longer prevents a handler from checking later candidates.
- Staff detection now accepts:
  - ancestry containing `staff`, `non_player`, or `nonplayer`
  - or the distinctive `Person + Preferred Job` header signature
- Player handler now rejects:
  - standings signature `Team/Pld/W/D/L/Pts`
  - staff signature `Person + Preferred Job`
  - staff/non-player ancestry
- Staff Search does not expose the same selected-row CSS class as player lists in Patch 0.1 diagnostics, so Staff capture now reads rendered staff result rows directly while preserving scrolling and deduplication.
- Player capture remains selected-row based.
- F8 diagnostics remain available.

## Test

1. Build with `tools\Build-Patch-0.2.cmd`.
2. Replace the installed test DLL while FM26 is closed.
3. Launch FM26 and confirm the console says `FM26Export Patch 0.2`.
4. Open Staff Search with a small filtered result set.
5. Press F9.
6. Confirm the console says `Using handler: StaffExportHandler`.
7. Confirm a `staff_export_*.csv` and matching HTML are created.
8. Inspect row count and columns before testing a larger search.
9. Re-test normal Player Search export to ensure the working player path is unchanged.

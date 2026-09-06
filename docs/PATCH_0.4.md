# Patch 0.4 - Selected Staff Only

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.4  
Status: test build

## Goal

Make Staff Search behave like Player Search: only selected staff members should be exported.

## Why this needs a different selector

Patch 0.1 diagnostics showed `selectedRows=0` for Staff Search even when rows were selected. FM26's staff table therefore does not expose selection in the same root-row CSS class used by player lists.

## Changes

- Staff export no longer exports every rendered row.
- Adds a recursive staff-selection detector.
- The detector checks:
  - the standard player `virtualised-list__item--selected` class
  - descendant classes containing `selected` or `checked` (excluding `unselected` / `unchecked`)
  - checked Unity UI Toolkit `Toggle` controls
- Staff export captures only rows for which that detector finds a selection marker.
- Player export behavior is unchanged.
- F8 diagnostics now use the same selection detector and report up to three matched selection markers for Staff Search.

## Test sequence

1. Build with `tools\Build-Patch-0.4.cmd`.
2. Install the new DLL while FM26 is closed.
3. Open Staff Search with a small result set.
4. Select exactly 2 or 3 staff members.
5. Press F8 first.
6. Check whether the Staff candidate reports `selectedRows=2` or `selectedRows=3` and prints selection markers.
7. Press F9.
8. Confirm the exported CSV contains only those selected staff members.

If F8 still reports `selectedRows=0`, do not treat that as a failed routing fix. It means FM26 stores Staff selection in a different UI state than the current detector can see; the F8 marker output will guide the next selector implementation.

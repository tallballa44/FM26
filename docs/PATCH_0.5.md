# Patch 0.5 - Staff Selection Diagnostics

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.5  
Status: diagnostic test build

Patch 0.4 falsely treated every staff row as selected because the first selection heuristic was too broad.

## Changes

- Tightens the staff selector so generic descendant classes containing words like `selected` no longer automatically count unless they match a strict selected-state pattern.
- F8 now logs the first six rendered staff rows regardless of selection.
- For each of those rows it reports:
  - row name
  - viewDataKey
  - row background color
  - descendant classes containing select/check/active/focus/highlight/current
  - descendant background colors
  - toggle states
  - whether the strict detector thinks the row is selected

## Test

1. Build with `tools\Build-Patch-0.5.cmd`.
2. Install while FM26 is closed.
3. Open a small Staff Search result set.
4. Select exactly two rows among the first six visible rows, ideally rows 1 and 3.
5. Press F8 only.
6. Capture the console lines beginning `Staff row 0` through `Staff row 5`.

Do not use F9 for this diagnostic test. The goal is to identify the exact UI state FM26 uses to distinguish selected and unselected staff rows.

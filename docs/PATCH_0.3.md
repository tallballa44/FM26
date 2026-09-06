# Patch 0.3 - Cleanup / Release Candidate

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.3  
Status: release candidate

## Validated in FM26

- Staff Search table correctly detected as `nonplayertable`.
- Staff Search F9 uses `StaffExportHandler`.
- 16 filtered staff rows exported successfully to CSV and HTML.
- Staff columns and values visually checked and looked correct.
- Existing player export workflow was re-tested and remained functional.

## Changes from Patch 0.2

- Sets `staff_export_` before generic capture initialization so the startup log shows the correct prefix.
- Removes stale Patch 0.1 wording from F8 diagnostics.
- No routing or row-capture behavior changed from the validated Patch 0.2 logic.

## Build

Run:

`tools\Build-Patch-0.3.cmd`

Output:

`dist\patch-0.3\FM26PlayerExport.dll`

If this cleanup build behaves identically to Patch 0.2, it is suitable for promotion from `dev/table-routing` to `main`.

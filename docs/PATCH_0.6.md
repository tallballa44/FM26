# Patch 0.6 - Release Candidate

Base: FM26 Player Export 5.1.0  
Fork plugin version: 5.1.6  
Status: release candidate

## Validated behavior

Staff Search routing and selected-only export have been validated in FM26.

Observed successful test:
- Staff table container: `nonplayertable`
- F8 detected `selectedRows=2` after selecting exactly two staff members
- F9 used `StaffExportHandler`
- Export completed with exactly 2 rows
- Generated CSV and HTML were visually checked and looked correct
- Existing Staff Search routing remained correct
- Existing player-export workflow had previously been re-tested successfully after the routing changes

## Cleanup in Patch 0.6

- Alternate staff views using `Person | Job | ...` are now classified as Staff when staff-specific attributes are present.
- Staff fallback detection now understands both:
  - `Person + Preferred Job`
  - `Person + Job + staff-specific attributes`
- No selected-row export behavior changed from the validated Patch 0.5 logic.

## Build

Run:

`tools\Build-Patch-0.6.cmd`

Output:

`dist\patch-0.6\FM26PlayerExport.dll`

If this build launches and behaves identically to Patch 0.5, it is suitable for promotion from `dev/table-routing` to `main`.

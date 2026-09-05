# Decompiling the exact 5.1 DLL

We need the exact 5.1 implementation because the public upstream source is older than the binary.

## Preferred method: ILSpy

1. Install ILSpy (GUI) or `ilspycmd`.
2. Open:
   `original/FM26PlayerExport_original.dll`
3. Export the project/source into:
   `src/decompiled-5.1/`
4. Keep namespaces and file names as generated.
5. Commit the exported source to `dev/table-routing`.

## ilspycmd example

From the repository root:

```powershell
ilspycmd -p -o .\src\decompiled-5.1 .\original\FM26PlayerExport_original.dll
```

If `ilspycmd` is not installed but the .NET SDK is available:

```powershell
dotnet tool install --global ilspycmd
ilspycmd -p -o .\src\decompiled-5.1 .\original\FM26PlayerExport_original.dll
```

## Why this is required

The 5.1 DLL contains configuration and incremental export code that is absent from the public 5.0 source. Patching the public 5.0 source directly could regress working 5.1 behavior.

## What to inspect first after decompilation

- `FM26PlayerExport.cs`
- `PluginConfig.cs`
- `Handlers/GenericScrolledTableHandler.cs`
- `Handlers/StaffExportHandler.cs`
- `Handlers/PlayerExportHandler.cs`

The first development patch should avoid changing scrolling/export writers until table discovery and handler routing are understood.

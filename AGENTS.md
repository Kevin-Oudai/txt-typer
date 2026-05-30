# AGENTS.md

## Project Rules

- Keep the app as a single-window WPF utility using the existing MVVM layout: `Views`, `ViewModels`, `Models`, `Services`, and `Helpers`.
- Preserve script text exactly. Normal text input must send the actual Unicode characters from the script; control tokens may use virtual keys.
- Keep generated build folders (`bin/`, `obj/`) out of source control.
- The root `txt-typer.exe` is the published app artifact. Refresh it after user-facing changes with:

```powershell
dotnet publish .\txt-typer.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o .\publish
Copy-Item .\publish\txt-typer.exe .\txt-typer.exe -Force
Copy-Item .\publish\txt-typer.dll .\txt-typer.dll -Force
Copy-Item .\publish\txt-typer.deps.json .\txt-typer.deps.json -Force
Copy-Item .\publish\txt-typer.runtimeconfig.json .\txt-typer.runtimeconfig.json -Force
```

- Validate code changes with `dotnet build .\txt-typer.csproj -c Release`.
- Record notable changes in `VERSIONS.md`.

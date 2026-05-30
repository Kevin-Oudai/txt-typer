# txt-typer

txt-typer is a single-window WPF desktop utility for repetitive local work where the destination does not accept paste and must receive actual keyboard input. You write or load a script, configure the countdown and delays, switch focus to the target window, and txt-typer simulates keystrokes line by line.

## How it works

1. Enter text in the main editor or load a saved snippet into the editor.
2. Adjust the countdown, per-character delay, and per-line delay.
3. Click `Start Typing`.
4. During the countdown, focus the target window such as a terminal or SSH session.
5. txt-typer types each line as real keyboard input and sends `Enter` after each normal line.

Typed text preserves the exact characters from the script, including uppercase and lowercase letters.

Blank line behavior:

- If `Preserve blank lines` is enabled, empty lines send `Enter`.
- If it is disabled, empty lines are skipped.

## Control tokens

Control tokens are only interpreted when `Enable control tokens` is checked. Otherwise they are typed literally.

Supported tokens:

- `{ENTER}`
- `{TAB}`
- `{ESC}`
- `{UP}`
- `{DOWN}`
- `{LEFT}`
- `{RIGHT}`
- `{BACKSPACE}`
- `{DELETE}`
- `{HOME}`
- `{END}`
- `{PGUP}`
- `{PGDN}`
- `{CTRL+C}`
- `{CTRL+V}`
- `{CTRL+SHIFT+V}`
- `{ALT+TAB}`
- `{WAIT:2000}`

Notes:

- `{WAIT:2000}` pauses for 2000 milliseconds.
- A line containing only a `WAIT` token pauses and does not add an automatic `Enter`.
- Inline tokens are allowed. For example: `sudo su -{ENTER}`.
- A normal line still receives an automatic `Enter` after it finishes typing.

## Snippet storage

Snippets are stored locally as JSON at:

`%LocalAppData%\txt-typer\snippets.json`

Each snippet record contains:

- `Name`
- `Content`

Snippets are never executed directly from the list. They are loaded into the editor first.

## Emergency stop

Typing can be canceled immediately in two ways:

- Click the `STOP` button.
- Press the global hotkey `Ctrl+Alt+F12`.

Cancellation is driven by a `CancellationTokenSource`, so countdown waits, line delays, wait tokens, and long runs can be interrupted quickly without freezing the UI.

## Project structure

- `Views` - WPF window XAML and UI-specific code-behind
- `ViewModels` - main window state and commands
- `Models` - snippets, typing settings, token actions, and progress models
- `Services` - input simulation, token parsing, snippet persistence, dialogs, and global hotkey registration
- `Helpers` - MVVM command helpers, observable base class, key constants, and visual-tree helpers

## Build

Open `txt-typer.slnx` in Visual Studio and run the `txt-typer` project, or build from the command line:

```powershell
dotnet build .\txt-typer.csproj -c Release
```

The project targets `net10.0-windows`. The root `txt-typer.exe` is the published app entry point and uses the adjacent published runtime files in the repository root.

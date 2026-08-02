---
name: run-sredstva-app
description: Build, launch, and drive the ERPiSredstvaApp WPF desktop app (screenshot, click, type, log in) via a UI Automation PowerShell driver. Use when asked to run, start, build, test, or screenshot ERPiSredstvaApp / ERPiSredstva, or to verify a WPF/XAML UI change actually works.
---

Paths below are relative to `ERPiSredstva/` (the repo root — it contains
`ERPiSredstva.slnx`). ERPiSredstvaApp is a .NET 8 WPF desktop app (`net8.0-windows`,
code-behind, no MVVM), run natively on Windows — there is no headless/xvfb story
here, the app just runs.

## ⚠️ Read this before doing anything: production data lives on this machine

`%LocalAppData%\ERPiSredstvaApp\Baze\*.db` holds **real customer SQLite databases**
(e.g. a real company's asset register) if this app has ever been installed/run
by the person developing here. `%LocalAppData%\ERPiSredstvaApp\settings.json`
(`ActiveDbPath`) decides which `.db` file the app opens on launch — and it is
**not** overridable via a `LOCALAPPDATA` environment variable on the child
process: .NET's `Environment.GetFolderPath(LocalApplicationData)` resolves
through the Windows known-folder API (registry-backed), which ignores env var
overrides. So you cannot sandbox this app by just setting an env var.

**Never launch this app for testing without first checking** whether
`%LocalAppData%\ERPiSredstvaApp\Baze\*.db` contains real files, and if so, following
the isolation procedure below. Getting this wrong means an automated click-driver
poking at someone's real financial/asset records.

## Run (agent path) — build, seed an isolated db, drive, screenshot

### 1. Build

```
dotnet build ERPiSredstva.slnx -c Debug
```

Produces `ERPiSredstvaApp\bin\Debug\net8.0-windows\ERPiSredstvaApp.exe`. (Ignore the
`NU1701` warnings about OpenTK/SkiaSharp.Views.WPF being restored for .NETFramework
— harmless, unrelated to this app.)

### 2. Create an isolated scratch database (never point the app at a real one)

```
cd ERPiSredstvaApp\.claude\skills\run-sredstva-app\seed-db
dotnet run -- "C:\path\to\scratch.db"
```

This runs `SredstvaDbContext.Create()` (same code path the app itself uses) against
a brand-new file, applying all EF Core migrations and seeding the default
`admin` / `admin` user. It refuses to run if the target file already exists.

### 3. Point the app at the scratch db (back up settings.json first!)

```powershell
$Settings = "$env:LOCALAPPDATA\ERPiSredstvaApp\settings.json"
$Backup   = "$env:TEMP\sredstva_settings_backup.json"
Copy-Item $Settings $Backup -Force -ErrorAction SilentlyContinue   # skip if it doesn't exist yet

@{ ActiveDbPath = "C:\path\to\scratch.db"; StartMaximized = $true; AutoBackupFrequency = 0; LastAutoBackupDate = $null } `
  | ConvertTo-Json | Set-Content -Encoding utf8 $Settings
```

`AutoBackupFrequency = 0` disables the app's own auto-backup-on-exit feature —
irrelevant on a scratch db, but keeps behavior minimal.

### 4. Drive it

All commands go through `driver.ps1` (this directory). Each invocation is a
fresh `powershell.exe` process; it tracks the running app's PID in
`$env:TEMP\sredstva_driver_state.json` so successive calls find the same window.

```powershell
$Drv = "ERPiSredstvaApp\.claude\skills\run-sredstva-app\driver.ps1"
$Exe = "ERPiSredstvaApp\bin\Debug\net8.0-windows\ERPiSredstvaApp.exe"

powershell -ExecutionPolicy Bypass -File $Drv launch $Exe
powershell -ExecutionPolicy Bypass -File $Drv ss login.png          # screenshot the login window
powershell -ExecutionPolicy Bypass -File $Drv type TxtUsername admin
powershell -ExecutionPolicy Bypass -File $Drv type TxtPassword admin
powershell -ExecutionPolicy Bypass -File $Drv click BtnLogin
powershell -ExecutionPolicy Bypass -File $Drv tree                  # dump AutomationId tree of current window
powershell -ExecutionPolicy Bypass -File $Drv click BtnRashod       # any AutomationId from `tree`, e.g. BtnDashboard/BtnSredstva/BtnKartice/BtnDobavljaci/BtnPrijava/BtnRashod/BtnAmortizacija/BtnRevalorizacija/BtnPopis/BtnRekap
powershell -ExecutionPolicy Bypass -File $Drv ss rashod.png
powershell -ExecutionPolicy Bypass -File $Drv close
```

Commands: `launch <exe>`, `tree`, `click <AutomationId>`, `type <AutomationId> <text>`,
`ss <out.png>`, `close`. `AutomationId` is the control's `x:Name` in XAML (WPF
exposes it 1:1 to UI Automation for named elements).

### 5. Restore the real settings.json immediately

```powershell
Copy-Item $Backup $Settings -Force -ErrorAction SilentlyContinue
Remove-Item $Backup -ErrorAction SilentlyContinue
```

Do this right after step 4 finishes (success or failure) — don't leave the
swap in place. If `$Backup` doesn't exist (no prior settings.json), just
delete `$Settings` instead of restoring it, so the app regenerates fresh
defaults from `%LocalAppData%\ERPiSredstvaApp\Baze\*.db` next real launch — but
this case (no settings.json but real `.db` files present) shouldn't happen in
practice.

## Run (human path)

Visual Studio 2022+ / Rider: open `ERPiSredstva.slnx`, set `ERPiSredstvaApp` as
startup project, F5. Or `dotnet run --project ERPiSredstvaApp\ERPiSredstvaApp.csproj`
— opens a real window, blocks until closed; useless for an agent without the
driver above.

## Test

```
dotnet test ERPiSredstvaData.Tests\ERPiSredstvaData.Tests.csproj
```

Unit tests only cover `ERPiSredstvaData` calculators (amortizacija/popis/revalorizacija) —
no UI coverage. The driver above is the only way to exercise the WPF layer.

## Gotchas

- **`LOCALAPPDATA` env var override does not work.** Confirmed empirically:
  launching the exe with `$env:LOCALAPPDATA` set to a scratch folder still
  read the real `%LocalAppData%\ERPiSredstvaApp\settings.json` and opened the
  real company database (`TxtFirma`/`ImeFirmeText` showed the real firm name).
  Only the settings.json-swap procedure above actually isolates it.
- **`SetForegroundWindow` gets silently denied on repeat calls.** Windows'
  foreground-lock heuristic blocks a background process from repeatedly
  stealing focus — the first screenshot after login succeeded, the next one
  silently captured whatever IDE/terminal window was actually on top instead
  of the app. Fix: go through `(New-Object -ComObject WScript.Shell).AppActivate($pid)`
  instead of raw `user32!SetForegroundWindow` — it's exempt from that lock.
  `driver.ps1`'s `Get-TopWindow` already does this before every `ss`/`click`/`type`.
  Symptom if you re-implement this yourself and skip the fix: a screenshot that
  shows your editor instead of the app, no error raised.
  See [driver.ps1:59-69](driver.ps1#L59-L69).
- **`PasswordBox` has no `ValuePattern`.** UI Automation can't set its value
  directly (by design, for security). `driver.ps1`'s `type` command uses
  `SendKeys` uniformly for both `TextBox` and `PasswordBox` instead of trying
  `ValuePattern` first. `SendKeys` special characters (`{ } + ^ % ~ ( )`) need
  `{}`-escaping if a typed value ever contains them — not needed for
  `admin`/`admin` but will bite on real passwords.
  See [driver.ps1:124-133](driver.ps1#L124-L133).
- **`TextBox`/`PasswordBox` don't support `InvokePattern`.** Only click
  actual `Button` elements; `driver.ps1 click TxtUsername` throws
  "Unsupported Pattern" — that's expected, use `type` for input fields.
- **Default login is `admin` / `admin`** (seeded by
  `ERPiSredstvaData\SredstvaDbContext.cs` `HasData` / the `AddKorisnici` migration)
  on every freshly-created database — including scratch ones from `seed-db`.
- **A fresh db shows "Nije dostupna kompanija" / `Firma: —` and all-zero
  dashboard stats.** That's correct for a scratch db with no `Firma` row —
  not a bug. Screenshots from this driver will look empty unless you first
  drive the "Firme" module to create one.
- Nested `AutomationId`s (e.g. the `BtnRashod`/`BtnDashboard` sidebar buttons)
  render each with a child `Text` element carrying the emoji+label — click
  targets the outer `Button`'s `AutomationId`, not the inner text.

## Troubleshooting

- **`FindFirst` returns `$null` / "Element with AutomationId 'X' not found"**:
  run `driver.ps1 tree` first to confirm the current window and its actual
  `AutomationId`s — you're probably still on the login window, or a different
  page than expected after a `click`.
- **Screenshot captures the wrong window (editor/terminal instead of the app)**:
  see the `SetForegroundWindow` gotcha above; make sure you're on a driver.ps1
  build that uses `AppActivate`.
- **`seed-db` run fails to restore packages**: it's a normal `net8.0` console
  app referencing `..\..\..\..\..\ERPiSredstvaData\ERPiSredstvaData.csproj` — run
  `dotnet build ERPiSredstva.slnx` at the repo root at least once first so
  `ERPiSredstvaData`'s own dependencies are restored.

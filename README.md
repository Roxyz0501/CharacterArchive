# Character Archive

[日本語](README.ja.md)

Character Archive is a character-information manager for XIVLauncher / Dalamud, authored by Roxyz0501. It records logged-in characters, jobs, playtime, Lodestone profiles, and FFXIV configuration-folder mappings locally, and presents them in an in-game UI with CSV export.

## Features

- Records character name, home/current world, Content ID, race, clan, gender, current job and level, and all class/job levels.
- Resolves the matching `FFXIV_CHR...` configuration folder from the Content ID (read-only).
- Records the game’s standard `/playtime` response and displays it as `12d 3h 45m` or `00291:45:00`.
- Looks up Lodestone IDs and public profile face images individually or in a throttled batch.
- Exports character name, server, Lodestone ID, and configuration-folder name as UTF-8 BOM CSV to a user-selected location.
- Provides an optional, click-only support link to Roxyz0501’s Ko-fi page.

## Installation

Source, issues, license, and releases use this plugin’s [individual GitHub repository](https://github.com/Roxyz0501/CharacterArchive). Distribution uses Roxyz0501’s shared Dalamud custom repository:

`https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json`

Add that URL under Dalamud Settings → Experimental → Custom Plugin Repositories and install Character Archive. For local development, build Release and add the output DLL or its folder to Dev Plugin Locations.

## Usage and command

Log into a character to add or update it. Use `/chararchive` to open or close the window. Overview, jobs, character-file mapping, settings, and support are separated into tabs. Lodestone lookup, playtime refresh, and CSV export run only through their controls or explicit settings.

## Settings

- **Language:** Choose 日本語, English, Deutsch, Français, 한국어, 简体中文, or 繁體中文. There is no Auto setting.
- **Automatically acquire ptime on login:** Off by default. When enabled, runs standard `/playtime` once per login after commands become available.
- **Delay after commands become available:** 1–60 seconds; default 1 second.
- **Playtime display:** Game-style or total-hours format.
- **Automatically look up Lodestone ID on login:** Off by default.
- **CSV output folder:** Initial folder for the save dialog; the final path is chosen for each export.

Manual playtime acquisition has a 10-second rate limit. The plugin uses the normal game text-command path and does not construct or directly send custom packets, poll, or automatically retry.

### Initial language selection

When no valid language has been saved, the plugin resolves and saves one language once. It checks the public game-client language first (`IClientState.ClientLanguage`), then the public Dalamud UI language (`IDalamudPluginInterface.UiLanguage`), then falls back to English. The current SDK exposes no public launcher-language source used by this plugin, so no private launcher files or settings are inspected. A saved selection is never overwritten at startup, including after character changes. Legacy English/Japanese values retain their numeric values; missing, old Auto, and invalid values are resolved once.

Language changes apply immediately and persist. Plugin-owned UI, status/error text, support content, and playtime text are localized. The exported CSV schema remains the stable Japanese four-column header originally defined for this plugin (`キャラクター名,サーバー名,ロドストID,設定ファイル名`) regardless of UI language. Character names and game-provided race/job/world text remain whatever the game data source provides; the plugin does not invent translations for unavailable game data.

## Requirements and dependencies

- Windows FINAL FANTASY XIV, XIVLauncher, and Dalamud API 15.
- .NET 10 only when building from source.
- No required plugin dependency.

Dalamud and FFXIVClientStructs are supplied by the runtime. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Data and privacy

The plugin configuration stores Content IDs, character names, worlds, Lodestone IDs, job levels, playtime, FFXIV configuration-folder names/paths, timestamps, and UI language. Typical locations are:

- Configuration: `%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive.json`
- Face-image cache and default CSV folder: `%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive\`
- CSV: the location explicitly chosen by the user

FFXIV folders are checked read-only. Lodestone HTTPS requests occur only after an explicit lookup or when automatic lookup is enabled. They send character name and home world to official `*.finalfantasyxiv.com` endpoints and retrieve public Lodestone IDs and face images. Content IDs, local paths, jobs, playtime, and CSV data are not sent. Batch lookup is serial with a one-second delay; timeout is 20 seconds, and images are restricted to official HTTPS hosts and 5 MB.

The plugin itself does not contact Ko-fi. The browser opens `https://ko-fi.com/roxyz0501` only after the support button is clicked.

## Known limitations

- Lodestone maintenance, search ambiguity, visibility, or HTML changes can prevent ID or image acquisition.
- `/playtime` reports minutes, so the seconds field is always `00`.
- FFXIV updates can temporarily break playtime integration until runtime structures are updated.
- Korean, Simplified Chinese, and Traditional Chinese glyph coverage depends on the active Dalamud font atlas; in-game visual acceptance is required before publication.
- Users are responsible for checking current FFXIV, XIVLauncher, and Dalamud policies.

## Troubleshooting

- For a missing face image, retry individual/batch Lodestone lookup and confirm the profile is public.
- If `/playtime` is unavailable at login, increase the post-unlock delay or refresh manually after login.
- If the language is wrong, choose one of the seven named languages in Settings; that choice persists.
- For a missing configuration folder, confirm it exists in this Windows/FFXIV installation.

## Uninstall and remove data

Uninstall Character Archive in Dalamud. To remove stored data, close the game and Dalamud, then delete the configuration JSON and cache folder above. Delete exported CSV files separately.

## Build and package

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

The next unpublished package is `artifacts\CharacterArchive-0.6.0.0.zip`; its final release URLs must be injected during publication. See [PUBLICATION_CHECKLIST.md](PUBLICATION_CHECKLIST.md).

## Third-party references and attribution

- **BetterPlaytime**, by Infi, [repository](https://github.com/caitlyn-gg/BetterPlaytime), MIT License. Character Archive references its approach of invoking standard `/playtime` and observing `UIModule.PrintPlayTime` for minute-based playtime. Related code is in `GameChatCommandService.cs` and playtime response handling in `Plugin.cs`.
- **FFXIVClientStructs**, by aers and contributors, [repository](https://github.com/aers/FFXIVClientStructs), MIT License. Character Archive uses runtime public APIs including `UIModule`, `RaptureShellModule`, and `Utf8String`; it does not redistribute its binary or source.

Required notices are in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Third-party work is not represented as work authored by Roxyz0501.

## License, disclaimer, and AI assistance

Released under the [MIT License](LICENSE). FINAL FANTASY XIV © SQUARE ENIX CO., LTD. This project is not affiliated with or endorsed by SQUARE ENIX, XIVLauncher, or Dalamud. It is provided without warranty and used at the user’s own risk.

OpenAI Codex assisted with design, implementation, and documentation. Roxyz0501 must review the code and test it in FFXIV before public release.

## Optional support

You may optionally support development at [Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501). Support is never required and does not unlock features.

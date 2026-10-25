# Character Archive

[日本語](README.ja.md)

Character Archive is a character-information manager for XIVLauncher / Dalamud, authored by Roxyz0501. It records logged-in characters, jobs, playtime, Lodestone profiles, and FFXIV configuration-folder mappings locally, and presents them in an in-game UI with CSV export.

## Features

- Displays the selected main-scenario quest, chapter, acceptance state and internal quest sequence, with the last saved observation retained for offline characters.
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

Log into a character to add or update it. Use `/chararchive` to open or close the window. Overview, jobs, character-file mapping, settings, language settings, and support are separated into tabs. Lodestone lookup, playtime refresh, and CSV export run only through their controls or explicit settings.

## Language settings tab

- Choose 日本語, English, Deutsch, Français, 한국어, 简体中文, or 繁體中文 from the independent **Language settings** tab. There is no Auto setting.
- The selector follows the shared Roxyz0501 tab layout and applies changes immediately.

## Settings

- **Automatically acquire ptime on login:** Off by default. When enabled, runs standard `/playtime` once per login after commands become available.
- **Delay after commands become available:** 1–60 seconds; default 1 second.
- **Playtime display:** Game-style or total-hours format.
- **Automatically look up Lodestone ID on login:** Off by default.
- **CSV output folder:** Initial folder for the save dialog; the final path is chosen for each export.

Playtime command attempts have a 10-second rate limit. Automatic acquisition waits for command availability and stops after three local execution failures; use Refresh ptime to retry. It does not retry a successfully submitted command just because no response arrived. The plugin uses the normal game text-command path and does not construct or directly send custom packets.

### Initial language selection

When no valid language has been saved, the plugin resolves and saves one language once. It checks the public game-client language first (`IClientState.ClientLanguage`), then the public Dalamud UI language (`IDalamudPluginInterface.UiLanguage`), then falls back to English. The current SDK exposes no public launcher-language source used by this plugin, so no private launcher files or settings are inspected. A saved selection is never overwritten at startup, including after character changes. Legacy English/Japanese values retain their numeric values; missing, old Auto, and invalid values are resolved once.

Language changes apply immediately and persist. Plugin-owned UI, status/error text, support content, and playtime text are localized. The exported CSV schema remains the stable Japanese four-column header originally defined for this plugin (`キャラクター名,サーバー名,ロドストID,設定ファイル名`) regardless of UI language. Character names and game-provided race/job/world text remain whatever the game data source provides; the plugin does not invent translations for unavailable game data.

## Requirements and dependencies

- Windows FINAL FANTASY XIV, XIVLauncher, and Dalamud API 15.
- .NET 10 only when building from source.
- No required plugin dependency.

Dalamud and FFXIVClientStructs are supplied by the runtime. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Data and privacy

The plugin configuration stores Content IDs, character names, worlds, Lodestone IDs, job levels, playtime, FFXIV configuration-folder names/paths, timestamps, main-scenario quest IDs/names/chapters/states/sequences, and UI language. Typical locations are:

- Configuration: `%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive.json`
- Face-image cache and default CSV folder: `%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive\`
- CSV: the location explicitly chosen by the user

FFXIV folders are checked read-only. Lodestone HTTPS requests occur only after an explicit lookup or when automatic lookup is enabled. They send character name and home world to official `*.finalfantasyxiv.com` endpoints and retrieve public Lodestone IDs and face images. Content IDs, local paths, jobs, playtime, main-scenario progress and CSV data are not sent. Lookups require an exact name/world match in both search and profile data; ambiguous or changed markup is not guessed. All Lodestone HTTP requests (including redirects and images) are serial and spaced at least one second apart. Logical operations have a 20-second deadline; HTML is bounded to 2 MiB and images to 5 MiB. Redirects are restricted to official HTTPS hosts.

The plugin itself does not contact Ko-fi. The browser opens `https://ko-fi.com/roxyz0501` only after the support button is clicked.

## Main scenario progress

The overview includes a Main Scenario column. Each character's Details also has a Main Scenario tab with the quest name/ID, chapter, state, internal quest sequence and last changed observation time. States distinguish In progress, Not accepted, Last completed, and Latest available story completed. A sequence is an internal game value, not a completion percentage.

After a short login readiness period, the plugin checks already-received client state every 10 seconds while logged in; Refresh Current Character also refreshes it. It reads the scenario guide and accepted/completed quest state only, without quest commands, UI automation or new server requests. It does not query offline characters. Game-provided quest/chapter names use the client's data language; the seven-language interface does not translate unavailable game data.

Completion is claimed only when a unique endpoint derived from the installed main-scenario quest graph is confirmed complete. Empty/unavailable guide data does not imply completion. Unavailable observations preserve existing data; the displayed time identifies the last changed recorded state. A new patch can change the endpoint. Internal guide structures can be affected by game updates; actual in-game acceptance remains unverified.

## Saving and recovery

Settings and character snapshots apply in memory immediately and are saved by one changed-only, 60-second scheduler. Unchanged values (including edits reverted before the checkpoint) cause no write. Logout, normal game shutdown, and plugin unload flush pending changes unless saving has stopped after failures. Forced termination can lose the unsaved interval (normally up to about 60 seconds); backups do not recover it.

The same configuration path is retained, with three previous valid generations named `CharacterArchive.json.bak-<20-digit sequence>` beside it. Saves validate a temporary file before atomic replacement. On startup, a corrupt main file is restored from the newest valid backup and reported; the damaged main is kept as `CharacterArchive.json.corrupt-<id>`. If none is valid, saving is blocked instead of replacing your data with defaults. Close the game, retain copies, restore a valid configuration, then reload. Backup/corrupt files contain the same private character information as the main file.

After three consecutive save failures, automatic saving stops and Settings offers **Retry saving**. Incomplete character/job capture never replaces a valid record; after three failures, use **Refresh Current Character** to resume. Same-character simultaneous Lodestone lookups share one operation; shutdown cancels and drains owned work. Existing face images are retained when downloading fails.

Character records are current snapshots, not an append-only event history. No daily history files or empty History files tab are created. See [persistence verification](PERSISTENCE_FIX_REPORT.md) for synthetic test results and remaining live-game checks.

## Known limitations

- Lodestone maintenance, search ambiguity, visibility, or HTML changes can prevent ID or image acquisition.
- `/playtime` reports minutes, so the seconds field is always `00`.
- FFXIV updates can temporarily break playtime integration until runtime structures are updated.
- Korean, Simplified Chinese, and Traditional Chinese glyph coverage depends on the active Dalamud font atlas; in-game visual acceptance is required before publication.
- Users are responsible for checking current FFXIV, XIVLauncher, and Dalamud policies.

See [the 1.1.0.0 review](REVIEW_2026-10-25.md) for reproduced fixes, test coverage and remaining checks.

## Troubleshooting

- If main-scenario progress is missing, wait until login has finished and use Refresh Current Character. Offline records are not live; unavailable data does not clear an existing observation.
- For a missing face image, retry individual/batch Lodestone lookup and confirm the profile is public.
- If `/playtime` is unavailable at login, increase the post-unlock delay or refresh manually after login.
- If the language is wrong, choose one of the seven named languages in the Language settings tab; that choice persists.
- For a missing configuration folder, confirm it exists in this Windows/FFXIV installation.

## Uninstall and remove data

Uninstall Character Archive in Dalamud. To remove stored data, close the game and Dalamud, then delete the configuration JSON, its adjacent `.bak-*`, `.corrupt-*` and any crash-left `.tmp-*` files, and the cache folder above. Delete exported CSV files separately.

## Build and package

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

The release package is `artifacts\CharacterArchive-1.1.0.0.zip`. Plugin-specific tabs are followed by the canonical trailing group in the fixed order Settings, Language settings, Support. See [PUBLICATION_CHECKLIST.md](PUBLICATION_CHECKLIST.md).

## Third-party references and attribution

- **BetterPlaytime**, by Infi, [repository](https://github.com/caitlyn-gg/BetterPlaytime), MIT License. Character Archive references its approach of invoking standard `/playtime` and observing `UIModule.PrintPlayTime` for minute-based playtime. Related code is in `GameChatCommandService.cs` and playtime response handling in `Plugin.cs`.
- **Questionable**, by the Puni.sh Quest Squad, [repository](https://github.com/PunishXIV/Questionable), GNU AGPL v3 (upstream license). Its `QuestInfo` classification was consulted for the game-data `JournalGenre.Icon == 61412` main-scenario discriminator, then independently checked against installed game data. No Questionable code, quest scripts or assets are copied or redistributed; it is not a dependency.
- **FFXIVClientStructs**, by aers and contributors, [repository](https://github.com/aers/FFXIVClientStructs), MIT License. Character Archive uses runtime public APIs including `UIModule`, `RaptureShellModule`, `Utf8String`, `QuestManager`, and `AgentScenarioTree`; it does not redistribute its binary or source.

Required notices are in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Third-party work is not represented as work authored by Roxyz0501.

## License, disclaimer, and AI assistance

Released under the [MIT License](LICENSE). FINAL FANTASY XIV © SQUARE ENIX CO., LTD. This project is not affiliated with or endorsed by SQUARE ENIX, XIVLauncher, or Dalamud. It is provided without warranty and used at the user’s own risk.

OpenAI Codex assisted with design, implementation, and documentation. Roxyz0501 must review the code and test it in FFXIV before public release.

## Optional support

You may optionally support development at [Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501). Support is never required and does not unlock features.

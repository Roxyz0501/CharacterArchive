# Character Archive publication state

## 2026-11-23 — Character Archive 1.8.0.1

- Move the switch button before the portrait without changing column sorting or relog behavior.

## 2026-11-23 — Character Archive 1.8.0.0

- User-triggered character switching through the optional AutoRetainer local command. No upstream implementation copied; no custom packets.

## 2026-11-18 — Character Archive 1.7.0.0

- Optional current-character pin in Overview; default off. Existing visibility filters remain authoritative.

## 2026-11-17 — Character Archive 1.6.0.0

- Sortable overview columns and persistent per-character visibility controls for overview and details. Mapping and CSV use the complete archive. Release build and core tests passed; in-game interaction not verified.

## 2026-11-17 — Character Archive 1.5.0.0

- Overview now optionally hides records last observed as up to date. Incomplete and unknown records remain visible. Export and stored characters are unchanged.
- Overview and detail progress cells use proportional colored fills with numeric labels. Release build passed without warnings and all core tests passed; live rendering not tested.

## 2026-11-17 — Character Archive 1.4.0.0

- Current patch progress published: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.4.0.0 . Core tests passed; shared index and downloaded ZIP hash verified. In-game display not tested.


## 2026-11-17 version 1.2.0.1

- Source/tag: `4e8959942637257e0ac7b4886c5a175b1620aa9c`; anonymous public ZIP hash, four-file allowlist, manifest API15/minimum15.0.4.0/Author/RepoUrl/IconUrl and absence of the old receive hook in the public DLL verified. Repository/icon HTTP200 (image/png).
- Shared index `3f65f01a1ecef5fb399881668aa2ddd9ce7e1341`, connector, pinned and normal main raw URLs verified at 1.2.0.1; all 16 other entries preserved.
- After an explicit user deployment request, installed runtime files were updated to 1.2.0.1 while FFXIV was stopped. The installed DLL matches the public package; local profile/enabled/source state was preserved, previous runtime files backed up under ignored artifacts, and player configuration left untouched.

- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.2.0.1
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.2.0.1/CharacterArchive-1.2.0.1.zip
- ZIP 107,026 bytes; SHA-256 `A58453326D64CE9919E9F3D17864F056CC6576F3BD9E214674290E02FCCF2C5C`.
- Removed the implicated plugin-owned UI packet detour, original-call forwarding and raw packet reads. Observe standard playtime LogMessage 859 through the supported managed event; copy only scalar parameters and queue normal framework updates. Three-failure stop, exception containment, subscription cleanup and optional-feature behavior retained. Requires Dalamud 15.0.4.0.
- Crash evidence places the failure at/under original forwarding, outside the previous guard; exact exception subtype/underlying native target cause and other-plugin contributions cannot be established from the text record alone. See CRASH_FIX_2026-11-17.md.
- Release built against actual Dalamud 15.0.4.0: zero warnings/errors; 28 new receiver checks plus 9 optional-resource, 18 expansion, 43 persistence, 57 review/core/UI localization checks passed. 112 offline data checks passed; runtime DLL metadata contains no old packet callback/raw reader/hook references. No live-game reproduction or command was performed during verification; the separately authorized local deployment is recorded above. Restart FFXIV after updating before retesting housing entry.

## 2026-11-17 version 1.2.0.0

- Published after the user's explicit 2026-11-17 request, resolving the earlier approval block. The existing verified ZIP is unchanged.
- Source/tag: `3b11ac3c6966723b16d259b1ad88cd0190eb47b1`; public repository/icon HTTP200 (image/png), anonymous ZIP hash, four-file allowlist and manifest version/API15/Author/RepoUrl/IconUrl verified.
- Shared index `3eac912d88189c11cc28787f8f2ba8ae05e88b9c`: connector, pinned and normal main raw URLs verified at 1.2.0.0; all 16 other entries preserved.
- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.2.0.0
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.2.0.0/CharacterArchive-1.2.0.0.zip
- ZIP 105,338 bytes; SHA-256 `68EEB86F6EDE3A23095C94DC7634D990CDDF30AB8BA231FE78AC5E198C520D2D`.
- Added current expansion name and separate full-expansion / .0 launch-story completion percentages to Overview and character details. Required-route quest counts, OR/AND handling, installed-patch scope, milestone completion and unavailable observations are distinct. Existing settings/CSV and optional-hook fix retained.
- Clean locked-dependency Release build zero warnings/errors; 18 new completion checks plus nine optional-resource, 43 persistence, 57 review and existing core/UI seven-language checks passed. 108 offline checks cover six expansions/four data languages. Unchanged masks cause zero additional completion reads and unchanged observations cause zero dirty marks; no new server requests. See EXPANSION_PROGRESS_VALIDATION.md. Actual native integration/layout remains unverified; installed DLLs/player configuration untouched.

## 2026-11-16 version 1.1.0.1

- Source/tag: `8dff7f414263aae5b162da4d066d48b76f921877`; public repository/icon/ZIP verified. Shared index `df0bf840f2b125e0c430e9d85b624110f0b67560`, pinned and normal raw URLs verified 1.1.0.1; all 16 other entries unchanged.

- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.1.0.1
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.1.0.1/CharacterArchive-1.1.0.1.zip
- ZIP 99,119 bytes; SHA-256 `61D1A2FF7E80314CD36785F5872E4D2D2D12E2004684E8A3AF86AD1586703527`.
- Log-confirmed installation failure originated in mandatory inline playtime hook creation, not ZIP extraction. Uses the supported virtual-table pointer hook and isolates optional playtime initialization failure so other features load. No command is issued when acquisition is unavailable; stored data retained.
- Clean locked-dependency Release rebuild zero warnings/errors; nine new optional-resource checks plus 43 persistence, 57 review and all existing core/UI-localization checks passed. See INSTALLATION_FIX_2026-11-16.md. Native installation/hook activation in the live game is not yet verified; no installed DLL or production configuration was replaced.

## 2026-10-25 version 1.1.0.0

- Source/tag: `05c7e16bc9ce5b2e6dfa4265cd4c9182debbd3eb`. Public repository/release/icon HTTP200 (image/png); anonymous ZIP hash, four-file allowlist and manifest version/API15/Author/RepoUrl/IconUrl verified.
- Shared index: `c5f82f938d7cb50af16fc976c1a7b163a9809a9c`; connector, commit-pinned and normal main raw URLs verified 1.1.0.0. All 13 other entries preserved.

- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.1.0.0
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.1.0.0/CharacterArchive-1.1.0.0.zip
- ZIP: 98,064 bytes; SHA-256 `E66F4C3E27B40F58D77F377B318F200847D1F74FE90D879A8C0E9F2103D4B1E9`.
- Added read-only main-scenario progress per character and fixed nine reviewed issue groups; see REVIEW_2026-10-25.md for reproductions, scope and remaining manual checks. Existing configuration schema and CSV columns retained.
- Clean locked-dependency Release rebuild zero warnings/errors; existing core checks plus 43 persistence, 57 MSQ/review and 12 offline game-data checks passed. Four languages each contain 1,050 MSQs with the same graph endpoint. No live player data, installed DLL replacement or quest/server action used.
- Published under the project-wide continuing publication authorization. Runtime integration, real Lodestone markup and seven-language visual acceptance remain unverified.

## 2026-10-23 version 1.0.0.2

- Source: https://github.com/Roxyz0501/CharacterArchive
- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.0.0.2
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.0.0.2/CharacterArchive-1.0.0.2.zip
- ZIP size: 83,152 bytes; SHA-256: `EF65B3527741F6EBAF02F7F63CF55626CD8F8829E5B66A792538F25836BE8724`.
- Source/tag: `87269ff30a24618b259e74f624df8492c336f444`. Anonymous public ZIP hash and manifest verified; repository/release/icon returned HTTP200 (image/png). Shared index `9b0e7c05dfb526041866bd3062e48e315d993572` and pinned raw verified 1.0.0.2 with 12 other entries unchanged; normal raw main initially cached 1.0.0.1 (max-age=300).
- Changed-only 60-second saves, logout/normal-shutdown/unload flush, atomic replacement, three valid backups and corruption recovery. Incomplete capture is rejected before mutation; failures stop after three attempts with explicit retry.
- Same-character single-flight lookups and shutdown cancellation/drain; image download failures retain valid cached data. Normal command route and rate limits retained.
- Fixed dependencies restored in locked mode; clean Release rebuild with zero warnings/errors; all core tests and 43 persistence/lifecycle checks passed. Eight unchanged periods: zero writes. Six hundred changes: one save / 238 logical bytes. Physical SSD writes and actual game integration remain unverified.
- Release package remains four allowlisted runtime/license files. See PERSISTENCE_FIX_REPORT.md and distribution/RELEASE_NOTES-1.0.0.2.md for behavior and remaining manual tests.

## 2026-10-12 version 1.0.0.1

- Source: https://github.com/Roxyz0501/CharacterArchive
- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.0.0.1
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.0.0.1/CharacterArchive-1.0.0.1.zip
- SHA-256: `518C2810904FF248EF5FFF63E4E34EB2B28DA25EF6F9E4DCB806C59E0A0FC13C`
- Updated the copied canonical `StandardPluginTabs` implementation and now calls `DrawStandardTrailingTabs` exactly once after every plugin-specific top-level tab.
- The top-level right-edge order is fixed as Settings, Language settings, Support; no plugin-specific tab follows the shared group.
- Added source-level regression checks for one helper call and the required tab order. Clean Release build completed with zero warnings/errors and all core tests passed.
- In-game tab rendering and navigation remain unverified and are disclosed in the release notes.

## 2026-10-11 version 1.0.0.0

- Source: https://github.com/Roxyz0501/CharacterArchive
- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v1.0.0.0
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v1.0.0.0/CharacterArchive-1.0.0.0.zip
- SHA-256: `C22A3EED447643711FC93EC50A4051398DF2E0E65588A8092A542047A6ACB029`
- Copied the canonical `StandardPluginTabs` implementation byte-for-byte and moved the global selector from Settings into the localized `Language settings###LanguageSettings` tab.
- Standardized `Support###Support` colors, spacing, button sizing, recipient, URL, and click-only navigation against the canonical implementation.
- Seven-language selection order, initial detection, migration, immediate switching, and persistence remain intact.
- Clean Release build completed with zero warnings/errors and all core tests passed. The package contains only the DLL, manifest, license, and third-party notices.
- In-game tab rendering, language persistence after an actual game restart, CJK font coverage, and long German/French layouts remain unverified and are disclosed in the release notes.

## 2026-10-08 version 0.6.0.0

- Source: https://github.com/Roxyz0501/CharacterArchive
- Release: https://github.com/Roxyz0501/CharacterArchive/releases/tag/v0.6.0.0
- ZIP: https://github.com/Roxyz0501/CharacterArchive/releases/download/v0.6.0.0/CharacterArchive-0.6.0.0.zip
- SHA-256: `642FE0054C613AB38F62AF3B85F0356675FA4313C30934D7A331342105CA1FC0`
- Added Japanese, English, German, French, Korean, Simplified Chinese, and Traditional Chinese UI localization.
- Initial language is selected once from the public game language, then the public Dalamud UI language, with English fallback. Saved choices are retained and Auto is not exposed.
- Existing English/Japanese enum values are preserved. Missing, legacy Auto, and invalid values migrate once without resetting unrelated settings.
- CSV keeps the original language-independent four-column Japanese schema.
- Clean Release build completed with zero warnings and zero errors. Core, migration, locale normalization, resource completeness, format-placeholder, playtime, and CSV tests passed.
- The package contains only `CharacterArchive.dll`, `CharacterArchive.json`, `LICENSE`, and `THIRD_PARTY_NOTICES.md`; localization resources are compiled into the DLL.
- In-game switching/persistence, Korean/Chinese font coverage, long German/French layout, and native playtime/Lodestone behavior remain unverified and are disclosed in the release notes.

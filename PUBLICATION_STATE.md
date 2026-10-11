# Character Archive publication state

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

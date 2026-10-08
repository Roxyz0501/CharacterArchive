# Character Archive publication state

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

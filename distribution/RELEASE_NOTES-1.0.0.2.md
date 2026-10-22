Character Archive 1.0.0.2 improves persistence and acquisition reliability.

- Save changed settings and character snapshots every 60 seconds, and flush on logout, normal shutdown and unload.
- Validate temporary files before atomic replacement, retain three valid backup generations, and recover a corrupt configuration without overwriting unrecoverable data with defaults.
- Preserve valid character/job records during incomplete capture. Stop after three failures and allow explicit retry; no repeated partial saves or log flooding.
- Coalesce same-character Lodestone lookups, cancel/drain workers on unload, and preserve existing face images when downloading fails.
- Rate-limit playtime execution attempts and stop automatic attempts after three local failures. The normal game command path is retained.

Validation: clean Release build with zero warnings/errors, all core tests and 43 persistence/lifecycle checks passed. Synthetic input test: 600 changes → one save / 238 logical bytes; eight unchanged periods → zero writes. These are not physical SSD measurements.

Live-game login/logout, playtime/Lodestone integration, unload during network activity and seven-language visual checks remain unverified. Forced termination may lose the unsaved interval (normally up to about 60 seconds); backups cannot restore it. See the English/Japanese README and PERSISTENCE_FIX_REPORT.md for retry, recovery and data-removal instructions.

保存・取得処理の安定化版です。60秒変更時保存、ログアウト・正常終了時保存、正常3世代バックアップと破損復旧、不完全な取得結果の上書き防止、3回失敗時の停止と手動再試行、検索重複抑制と終了時回収を追加しました。実ゲーム動作は未検証です。強制終了では未保存分が失われる可能性があります。

Character Archive 1.1.0.0 adds main-scenario progress and fixes issues found in a full code review.

- Show the selected main-scenario quest, chapter, acceptance state, internal quest sequence and saved observation time in Overview and each character's Main Scenario tab.
- Read already-received game data at 10-second intervals; no quest commands or additional server requests. Offline/unavailable observations preserve the previous record. Latest completion requires a verified unique endpoint from installed game data; an empty guide is never completion.
- Require exact Lodestone name/world matches in both search and profile. Protect manual ID edits from late search completion, keep verified IDs when image downloading fails, and avoid generic site images as faces.
- Bound HTML/image downloads and redirects, space every HTTP request, and cancel/drain stalled bodies on unload.
- Protect future configurations from downgrade; validate invalid character records; fix bounded backup-maintenance retry; retain early playtime responses until full capture.
- Cache UI snapshots, retain full row IDs, add horizontal scrolling, wrap details, use atomic CSV replacement, and fix translation fallback/dynamic status text. Routine logs no longer include character names, Content IDs or playtime values.

Validation: clean Release build, zero warnings/errors; existing core checks, 43 persistence/lifecycle checks, 57 MSQ/review checks and 12 offline catalogue checks passed. Installed game data: 1,050 MSQs in each of English/Japanese/German/French, all resolving the same terminal. No live player data was used. Actual native game behavior and seven-language visual acceptance remain unverified; see REVIEW_2026-10-25.md.

メインクエストの進捗を概要・キャラクター詳細へ追加しました。受信済み情報の読み取りだけで、クエスト用コマンドや追加のサーバー照会は行いません。曖昧なロドスト検索、手入力の上書き、画像失敗時のID消失、設定復旧、早期ptime応答、CSV保存、UI・翻訳の問題も修正しました。実ゲームでの動作・表示は未検証です。

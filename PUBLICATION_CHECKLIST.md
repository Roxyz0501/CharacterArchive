# 共通カスタムリポジトリ取り込み前チェックリスト

公開・更新時の確認項目です。公開履歴は PUBLICATION_STATE.md、今回の保存処理の検証は PERSISTENCE_FIX_REPORT.md を参照してください。未チェックの実機項目は未検証です。

## 確定メタデータ

- InternalName: `CharacterArchive`
- Name: `Character Archive`
- AssemblyVersion: `1.1.0.0`
- DalamudApiLevel: `15`
- Author: `Roxyz0501`
- 必須依存プラグイン: なし
- Release ZIP名: `CharacterArchive-1.1.0.0.zip`

Punchline、Description、Tagsを含む機械可読案は`distribution/CharacterArchive.metadata.json`にあります。RepoUrl、DownloadLinkInstall、DownloadLinkUpdateは専用リポジトリと当該バージョンのReleaseを指定しています。公開後にURLとZIPを検証します。

## 人間によるコード・権利確認

- [ ] Roxyz0501が全ソースをレビューする
- [ ] `LICENSE`と`THIRD_PARTY_NOTICES.md`の内容を確認する
- [ ] BetterPlaytimeを参考にした範囲がNOTICEの説明と一致することを確認する
- [ ] 未許諾の画像、アイコン、コード、キャラクターデータが含まれていないことを確認する
- [ ] AI支援の開示内容を確認する

## FFXIV実機確認

- [ ] プラグインのロード、リロード、アンロードで例外が発生しない
- [ ] `/chararchive`でUIを開閉できる
- [ ] 初回起動時、ゲーム言語→Dalamud UI言語→Englishの順で対応言語が選ばれて保存される
- [ ] 独立した`言語設定###LanguageSettings`タブに日本語／English／Deutsch／Français／한국어／简体中文／繁體中文の7つが正本順で並び、Settings内やAuto選択肢がない
- [ ] `DrawStandardTrailingTabs`をトップレベルTabBarの最後に1回だけ呼び、固有タブ → 設定 → 言語設定 → 支援の順である
- [ ] 保存後の再起動とキャラクター切替で手動選択が上書きされない
- [ ] 全タブ、設定、通知、エラー、ツールチップが7言語へ即時切り替わり、CSVの日本語4列スキーマは言語変更で変化しない
- [ ] 韓国語・簡体字・繁体字・欧文アクセントのグリフと、ドイツ語・フランス語長文の折返しを実ゲームで確認する
- [ ] ログイン時のキャラクター情報と全ジョブが正しい
- [ ] ptime自動取得が既定OFFである
- [ ] ptime自動取得で正常送信はログインごとに1回、ローカル実行失敗は10秒間隔・最大3回で停止する
- [ ] コマンド禁止中は実行せず、解除後の設定秒数を待つ
- [ ] ptime手動取得の10秒制限が動作する
- [ ] Lodestone自動検索が既定OFFである
- [ ] 個別検索とまとめて検索の結果、顔画像、1秒間隔が正しい
- [ ] CSVの列、BOM、エスケープ、任意保存先が正しい
- [ ] `支援###Support`タブの通常・ホバー・選択中表示、文字コントラスト、余白、ボタン寸法が共通正本と一致する
- [ ] Ko-fiボタンを押したときだけ`https://ko-fi.com/roxyz0501`が開く
- [ ] 支援しなくても全機能を利用できる

## ビルドとパッケージ

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

`scripts/pack-release.ps1`は`artifacts/CharacterArchive-1.1.0.0.zip`を生成し、内容が次の4ファイルだけであることを検証します。言語リソースと共通タブ実装はDLLへ埋め込まれます。

- `CharacterArchive.dll`
- `CharacterArchive.json`
- `LICENSE`
- `THIRD_PARTY_NOTICES.md`

共通側の自動化では、プラグインディレクトリをカレントにして上記4コマンドを実行し、`artifacts/*.zip`と`distribution/CharacterArchive.metadata.json`を収集します。

## 共通リポジトリ統合時の作業

- [ ] `RepoUrl`へ本プラグイン専用GitHubリポジトリの実在URLを設定する
- [ ] `DownloadLinkInstall`と`DownloadLinkUpdate`へ、個別GitHub Releases上の検証済み同一バージョンZIPを指す実在URLを設定する
- [ ] 共通repo.jsonの1エントリとしてメタデータを取り込む
- [ ] 新版ごとにAssemblyVersion、ZIP名、URL、Changelogを同期する
- [ ] 共通リポジトリ側で提供するアイコンがある場合、Roxyz0501が権利を持つ64～512px画像だけを使用する

## 秘密情報・個人データ

- [ ] APIキー、トークン、Cookie、Webhook、秘密鍵、`.env`がない
- [ ] 実在キャラクター名、Content ID、Lodestone ID、CSV、設定JSON、顔画像がない
- [ ] `bin`、`obj`、IDEキャッシュ、ユーザー固有の絶対パスをGitへ追加していない
- [ ] Release ZIP内にPDB、deps.json、テスト成果物、ローカルパスがない

## 保存処理の実機確認（1.1.0.0）

- [ ] 60秒変更時保存とログアウト・正常終了・無効化時のflush
- [ ] 既存設定の読込と正常3世代バックアップからの復旧
- [ ] 取得失敗停止・保存停止の通知と明示再試行
- [ ] Lodestone検索中の無効化で処理・通信・リソースが残らない

## メインクエストと再点検（1.1.0.0）

- [ ] 実ゲームで進捗・分岐・最終完了・パッチ更新・強くてニューゲームを確認
- [ ] ログイン・ログアウト・キャラクター切替直後に別キャラの進捗を記録しない
- [ ] 概要の横スクロールと詳細タブの7言語表示を確認
- [ ] Lodestone実HTMLとの完全一致判定と顔画像取得を確認
- 合成・オフライン検証結果は REVIEW_2026-10-25.md を参照

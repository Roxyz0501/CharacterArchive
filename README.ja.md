# Character Archive

[English](README.md)

Character Archiveは、Roxyz0501が作成するXIVLauncher / Dalamud向けキャラクター情報管理プラグインです。ログインしたキャラクター、ジョブ、累計プレイ時間、Lodestoneプロフィール、FFXIV設定フォルダとの対応をローカルへ保存し、ゲーム内UIとCSVで確認できます。

## 主な機能

- キャラクター名、ホーム／現在ワールド、Content ID、種族、部族、性別、カレントジョブ／レベル、全クラス・ジョブのレベルを記録
- Content IDから対応する`FFXIV_CHR...`設定フォルダを読み取り専用で特定
- 標準`/playtime`応答を「12日 3時間 45分」または`00291:45:00`で表示
- Lodestone IDと公開プロフィールの顔画像を個別または頻度制御したまとめ検索で取得
- キャラクター名、サーバー名、Lodestone ID、設定フォルダ名をUTF-8 BOM付きCSVとして任意の場所へ出力
- 明示的に押した場合だけRoxyz0501のKo-fiを開く任意支援ボタン

## 導入方法

ソース、Issues、ライセンス、Releaseは本プラグイン専用GitHubリポジトリで管理し、配信はRoxyz0501の共通Dalamudカスタムリポジトリを使用します。共通`repo.json` URLは**未確定**です。公開時に`<ROXYZ0501_SHARED_REPO_JSON_URL>`を実在URLへ置き換えます。

公開後はDalamud設定 → Experimental → Custom Plugin RepositoriesへURLを追加し、プラグイン一覧から導入します。ローカル確認ではReleaseビルド後のDLLまたは格納フォルダをDev Plugin Locationsへ追加します。

## 利用方法・コマンド

キャラクターへログインすると情報が追加・更新されます。`/chararchive`でメインUIを開閉します。概要、ジョブ、キャラファイル紐づけ、設定、支援をタブで表示し、Lodestone検索、ptime再取得、CSV出力は対応する操作または明示設定時だけ実行します。

## 設定

- **言語 — English / 日本語：** 初回起動時だけDalamud／ゲームクライアント言語を判定し、日本語なら日本語、それ以外または検出不能ならEnglishを選択して保存します。以後の起動では再判定・上書きせず、ユーザーが選んだ値を維持します。
- **ログイン時にptimeを自動取得する：** 既定OFF。有効時だけコマンド解禁後、ログインごとに標準`/playtime`を1回実行します。
- **コマンド使用可能後の待機秒数：** 1～60秒、既定1秒。
- **プレイ時間の表示形式：** ゲーム風表示または累計時間表示。
- **ログイン時にLodestone IDを自動取得する：** 既定OFF。
- **CSV出力先：** 保存ダイアログの初期フォルダ。最終保存先は毎回選択できます。

手動ptime取得は10秒制限です。通常のゲーム内テキストコマンド経路だけを使い、独自パケットの生成・直接送信・ポーリング・自動再送は行いません。

## 必要環境・依存関係

- Windows版FINAL FANTASY XIV、XIVLauncher、Dalamud API 15
- ソースからビルドする場合のみ.NET 10
- 必須依存プラグインなし

DalamudとFFXIVClientStructsは実行環境から提供されます。[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)も参照してください。

## データとプライバシー

Dalamud設定へContent ID、キャラクター名、ワールド、Lodestone ID、ジョブレベル、ptime、FFXIV設定フォルダ名／パス、検出日時、UI言語を保存します。

- 設定：`%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive.json`
- 顔画像キャッシュ・既定CSVフォルダ：`%APPDATA%\XIVLauncher\pluginConfigs\CharacterArchive\`
- CSV：ユーザーが明示的に選んだ場所

FFXIV設定フォルダは読み取るだけです。Lodestone HTTPS通信は検索操作時または自動検索を有効にした場合だけ行い、公式`*.finalfantasyxiv.com`へキャラクター名とホームワールドを送信して公開Lodestone ID・顔画像を取得します。Content ID、ローカルパス、ジョブ、ptime、CSVは送信しません。まとめ検索は直列・1秒間隔、タイムアウト20秒、画像は公式HTTPSホスト・5MB以下です。

プラグイン自身はKo-fiへ通信しません。支援ボタンを押した場合だけ既定ブラウザで`https://ko-fi.com/roxyz0501`を開きます。

## 既知の制約

- Lodestoneのメンテナンス、検索結果、公開状態、HTML変更によりIDや顔画像を取得できない場合があります。
- `/playtime`は分単位のため、時間表示の秒部分は常に`00`です。
- FFXIV更新で実行環境の構造が変わると、対応更新までptime取得が動作しない場合があります。
- 利用者自身でFFXIV、XIVLauncher、Dalamudの最新規約・方針を確認してください。

## トラブルシューティング

- 顔画像がない場合は個別／まとめ検索を再実行し、公開プロフィールを確認してください。
- ログイン直後に`/playtime`が使えない場合は待機秒数を増やすか、ログイン完了後に手動再取得してください。
- 表示言語が意図と異なる場合は設定でEnglish／日本語を選択してください。以後その値を維持します。
- 設定フォルダがない場合は、そのWindows／FFXIV環境に対象キャラクターのフォルダが存在するか確認してください。

## アンインストール・データ削除

Dalamudからアンインストールします。保存データも消す場合はゲームとDalamudを終了し、上記の設定JSONとキャッシュフォルダを削除してください。別の場所へ出力したCSVは個別に削除します。

## ビルド・パッケージ

```powershell
dotnet restore .\CharacterArchive.slnx --locked-mode
dotnet build .\CharacterArchive.slnx -c Release --no-restore
dotnet run --project .\CharacterArchive.Tests\CharacterArchive.Tests.csproj -c Release --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\pack-release.ps1 -NoBuild
```

共通リポジトリへ渡す成果物は`artifacts\CharacterArchive-0.5.0.0.zip`と`distribution\CharacterArchive.metadata.json`です。[PUBLICATION_CHECKLIST.md](PUBLICATION_CHECKLIST.md)も参照してください。

## 第三者参照・帰属

- **BetterPlaytime** — 作者Infi、[リポジトリ](https://github.com/caitlyn-gg/BetterPlaytime)、MIT License。標準`/playtime`をテキストコマンド経路で実行し、`UIModule.PrintPlayTime`応答から分単位ptimeを取得する考え方を参考にしています。該当箇所は`GameChatCommandService.cs`と`Plugin.cs`のptime応答処理です。
- **FFXIVClientStructs** — aersおよびコントリビューター、[リポジトリ](https://github.com/aers/FFXIVClientStructs)、MIT License。実行環境提供の`UIModule`、`RaptureShellModule`、`Utf8String`等の公開型・APIを利用し、バイナリやソースは同梱しません。

必要な表示は[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)に収録しています。第三者の成果をRoxyz0501の作成物として扱いません。

## ライセンス・免責・AI支援

[MIT License](LICENSE)で提供します。FINAL FANTASY XIV © SQUARE ENIX CO., LTD. 本プロジェクトはSQUARE ENIX、XIVLauncher、Dalamudの公式製品ではありません。無保証であり、利用は自己責任です。

設計・実装・文書作成にはOpenAI CodexによるAI支援を使用しています。公開前にRoxyz0501によるコードレビューとFFXIV実機確認が必要です。

## 任意支援

[Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501)から任意で支援できます。支援は必須ではなく、機能解放にも関係しません。

using CharacterArchive.Models;
using System.Text.RegularExpressions;

namespace CharacterArchive.Services;

public static partial class LocalizationCatalog
{
    private sealed record Entry(string Ja, string De, string Fr, string Ko, string ZhHans, string ZhHant);

    private static readonly IReadOnlyDictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.Ordinal)
    {
        ["Open the Character Archive window."] = new("Character Archiveのメイン画面を開きます。", "Öffnet das Character-Archive-Fenster.", "Ouvre la fenêtre Character Archive.", "Character Archive 창을 엽니다.", "打开 Character Archive 窗口。", "開啟 Character Archive 視窗。"),
        ["Character Overview"] = new("キャラクター概要", "Charakterübersicht", "Aperçu des personnages", "캐릭터 개요", "角色概览", "角色概覽"),
        ["Character Details"] = new("キャラクター詳細", "Charakterdetails", "Détails du personnage", "캐릭터 상세", "角色详情", "角色詳細"),
        ["Character File Mapping"] = new("キャラファイル紐づけ", "Charakter-Dateizuordnung", "Association des fichiers", "캐릭터 파일 연결", "角色文件关联", "角色檔案關聯"),
        ["Settings"] = new("設定", "Einstellungen", "Paramètres", "설정", "设置", "設定"),
        ["★ Support"] = new("★ 支援", "★ Unterstützen", "★ Soutien", "★ 후원", "★ 支持", "★ 支援"),
        ["Saved characters: {0}"] = new("保存済みキャラクター: {0}", "Gespeicherte Charaktere: {0}", "Personnages enregistrés : {0}", "저장된 캐릭터: {0}", "已保存角色：{0}", "已儲存角色：{0}"),
        ["Refresh Current Character"] = new("現在のキャラクターを再取得", "Aktuellen Charakter aktualisieren", "Actualiser le personnage actuel", "현재 캐릭터 새로고침", "刷新当前角色", "重新整理目前角色"),
        ["Refresh ptime"] = new("ptime再取得", "Spielzeit aktualisieren", "Actualiser le temps de jeu", "플레이 시간 새로고침", "刷新游戏时间", "重新整理遊玩時間"),
        ["Requested ptime"] = new("ptimeを照会しました", "Spielzeit wurde angefordert", "Temps de jeu demandé", "플레이 시간을 요청했습니다", "已请求游戏时间", "已要求遊玩時間"),
        ["Could not request ptime (wait 10 seconds between requests)"] = new("ptimeを照会できませんでした（連続実行は10秒待機）", "Spielzeit konnte nicht angefordert werden (10 Sekunden warten)", "Impossible de demander le temps de jeu (attendez 10 secondes)", "플레이 시간을 요청할 수 없습니다(요청 간 10초 대기)", "无法请求游戏时间（请求间隔需等待10秒）", "無法要求遊玩時間（要求間隔需等待10秒）"),
        ["Log in with a character to add information here."] = new("キャラクターでログインすると、ここに情報が追加されます。", "Melde dich mit einem Charakter an, um hier Daten hinzuzufügen.", "Connectez-vous avec un personnage pour ajouter ses informations ici.", "캐릭터로 로그인하면 여기에 정보가 추가됩니다.", "登录角色后，信息会显示在这里。", "登入角色後，資訊會顯示在這裡。"),
        ["Face"] = new("顔", "Porträt", "Portrait", "얼굴", "头像", "頭像"),
        ["Character Name"] = new("キャラクター名", "Charaktername", "Nom du personnage", "캐릭터 이름", "角色名", "角色名稱"),
        ["Home World"] = new("ホームワールド", "Heimatwelt", "Monde d'origine", "홈 월드", "所属服务器", "所屬伺服器"),
        ["Lodestone ID"] = new("ロドストID", "Lodestone-ID", "ID Lodestone", "로드스톤 ID", "Lodestone ID", "Lodestone ID"),
        ["Current World"] = new("現在地ワールド", "Aktuelle Welt", "Monde actuel", "현재 월드", "当前服务器", "目前伺服器"),
        ["Current Job"] = new("カレントジョブ", "Aktueller Job", "Job actuel", "현재 직업", "当前职业", "目前職業"),
        ["Playtime"] = new("プレイ時間", "Spielzeit", "Temps de jeu", "플레이 시간", "游戏时间", "遊玩時間"),
        ["Last Login Detected"] = new("最終ログイン検出", "Letzte Anmeldung erkannt", "Dernière connexion détectée", "마지막 로그인 감지", "最后检测到登录", "最後偵測到登入"),
        ["No information is available."] = new("表示できる情報がありません。", "Keine Informationen verfügbar.", "Aucune information disponible.", "표시할 정보가 없습니다.", "没有可用信息。", "沒有可用資訊。"),
        ["Basic Information"] = new("基本情報", "Grundinformationen", "Informations générales", "기본 정보", "基本信息", "基本資訊"),
        ["Race / Clan / Sex"] = new("種族 / 部族 / 性別", "Volk / Stamm / Geschlecht", "Race / Ethnie / Sexe", "종족 / 부족 / 성별", "种族 / 部族 / 性别", "種族 / 部族 / 性別"),
        ["Current Job / Level"] = new("カレントジョブ / レベル", "Aktueller Job / Stufe", "Job actuel / Niveau", "현재 직업 / 레벨", "当前职业 / 等级", "目前職業 / 等級"),
        ["Grand Company"] = new("グランドカンパニー", "Staatliche Gesellschaft", "Grande compagnie", "총사령부", "大国防联军", "大國防聯軍"),
        ["Guardian Deity"] = new("守護神", "Schutzgottheit", "Divinité gardienne", "수호신", "守护神", "守護神"),
        ["Starting City"] = new("開始都市", "Startstadt", "Cité de départ", "시작 도시", "起始城市", "起始城市"),
        ["First Detected"] = new("初回検出", "Erstmals erkannt", "Première détection", "최초 감지", "首次检测", "首次偵測"),
        ["Last Detected"] = new("最終検出", "Zuletzt erkannt", "Dernière détection", "마지막 감지", "最后检测", "最後偵測"),
        ["Jobs"] = new("ジョブ", "Jobs", "Jobs", "직업", "职业", "職業"),
        ["Maps character names and servers to Lodestone IDs and configuration folders. You can enter a Lodestone ID manually if automatic lookup fails."] = new("キャラクター名・サーバー名・Lodestone ID・設定フォルダ名の対応です。Lodestone IDは自動取得できない場合、直接入力できます。", "Ordnet Charakternamen und Welten Lodestone-IDs und Konfigurationsordnern zu. Bei fehlgeschlagener Suche kann die Lodestone-ID manuell eingegeben werden.", "Associe les personnages et serveurs aux ID Lodestone et dossiers de configuration. Vous pouvez saisir l'ID manuellement si la recherche échoue.", "캐릭터 이름과 서버를 로드스톤 ID 및 설정 폴더에 연결합니다. 자동 검색 실패 시 ID를 직접 입력할 수 있습니다.", "将角色名和服务器与Lodestone ID及配置文件夹关联。自动查询失败时可手动输入ID。", "將角色名稱和伺服器與Lodestone ID及設定資料夾關聯。自動查詢失敗時可手動輸入ID。"),
        ["Batch Lookup Missing IDs"] = new("未取得をまとめて検索", "Fehlende IDs gesammelt suchen", "Rechercher les ID manquants", "누락 ID 일괄 검색", "批量查询缺失ID", "批次查詢缺少的ID"),
        ["Server"] = new("サーバー名", "Server", "Serveur", "서버", "服务器", "伺服器"),
        ["Configuration Folder"] = new("設定ファイル名", "Konfigurationsordner", "Dossier de configuration", "설정 폴더", "配置文件夹", "設定資料夾"),
        ["Status"] = new("状態", "Status", "État", "상태", "状态", "狀態"),
        ["Action"] = new("操作", "Aktion", "Action", "작업", "操作", "操作"),
        ["Found"] = new("確認済み", "Gefunden", "Trouvé", "확인됨", "已找到", "已找到"),
        ["Not found"] = new("未確認", "Nicht gefunden", "Introuvable", "찾을 수 없음", "未找到", "找不到"),
        ["Lookup"] = new("自動取得", "Suchen", "Rechercher", "검색", "查询", "查詢"),
        ["Looking up..."] = new("取得中...", "Suche läuft …", "Recherche…", "검색 중...", "正在查询...", "正在查詢..."),
        ["Acquisition failed"] = new("取得に失敗しました", "Abruf fehlgeschlagen", "Échec de l'acquisition", "가져오기 실패", "获取失败", "取得失敗"),
        ["Export CSV As..."] = new("CSVを名前を付けて出力", "CSV exportieren als …", "Exporter le CSV sous…", "CSV 다른 이름으로 내보내기...", "CSV另存为...", "CSV另存為..."),
        ["Choose where to save the CSV"] = new("CSVの保存先を選択", "Speicherort für CSV auswählen", "Choisir l'emplacement du CSV", "CSV 저장 위치 선택", "选择CSV保存位置", "選擇CSV儲存位置"),
        ["CSV file{.csv}"] = new("CSVファイル{.csv}", "CSV-Datei{.csv}", "Fichier CSV{.csv}", "CSV 파일{.csv}", "CSV文件{.csv}", "CSV檔案{.csv}"),
        ["(Copies the file path to the clipboard after export)"] = new("（出力後、ファイルパスをクリップボードへコピー）", "(Kopiert nach dem Export den Dateipfad in die Zwischenablage)", "(Copie le chemin dans le presse-papiers après l'exportation)", "(내보낸 후 파일 경로를 클립보드에 복사)", "（导出后将文件路径复制到剪贴板）", "（匯出後將檔案路徑複製到剪貼簿）"),
        ["Automatically acquire ptime on login"] = new("ログイン時にptimeを自動取得する", "Spielzeit bei Anmeldung automatisch abrufen", "Obtenir automatiquement le temps de jeu à la connexion", "로그인 시 플레이 시간 자동 가져오기", "登录时自动获取游戏时间", "登入時自動取得遊玩時間"),
        ["When enabled, runs the standard /playtime command once after login (default: OFF)."] = new("有効時のみ、ログイン後にゲーム標準の /playtime を1回実行します（既定: OFF）。", "Führt nach der Anmeldung einmal den Standardbefehl /playtime aus (Standard: AUS).", "Exécute une fois la commande standard /playtime après la connexion (désactivé par défaut).", "활성화하면 로그인 후 표준 /playtime 명령을 한 번 실행합니다(기본: 꺼짐).", "启用后，登录后执行一次标准/playtime命令（默认：关闭）。", "啟用後，登入後執行一次標準/playtime指令（預設：關閉）。"),
        ["Playtime display format"] = new("プレイ時間の表示形式", "Anzeigeformat der Spielzeit", "Format du temps de jeu", "플레이 시간 표시 형식", "游戏时间显示格式", "遊玩時間顯示格式"),
        ["Game style (e.g. 12d 3h 45m)"] = new("元の表示（例: 12日 3時間 45分）", "Spielstil (z. B. 12 T 3 Std. 45 Min.)", "Style du jeu (ex. 12 j 3 h 45 min)", "게임 형식(예: 12일 3시간 45분)", "游戏格式（例如：12天3小时45分）", "遊戲格式（例如：12天3小時45分）"),
        ["Total hours (e.g. 00291:45:00)"] = new("時間表示（例: 00291:45:00）", "Gesamtstunden (z. B. 00291:45:00)", "Heures totales (ex. 00291:45:00)", "누적 시간(예: 00291:45:00)", "总小时数（例如：00291:45:00）", "總小時數（例如：00291:45:00）"),
        ["Manual ptime requests are also limited to one every 10 seconds."] = new("手動のptime再取得も10秒以内の連続実行を抑止します。", "Manuelle Spielzeitanfragen sind ebenfalls auf eine pro 10 Sekunden begrenzt.", "Les demandes manuelles sont aussi limitées à une toutes les 10 secondes.", "수동 요청도 10초에 한 번으로 제한됩니다.", "手动请求也限制为每10秒一次。", "手動要求也限制為每10秒一次。"),
        ["Delay after commands become available"] = new("コマンド使用可能後の待機秒数", "Verzögerung nach Befehlsfreigabe", "Délai après disponibilité des commandes", "명령 사용 가능 후 대기 시간", "命令可用后的延迟", "指令可用後的延遲"),
        ["1-60 seconds. Waits this long after the game allows text commands (default: 1 second)."] = new("1～60秒。ゲーム側の禁止解除を検出してから、この秒数だけ待って実行します（デフォルト: 1秒）。", "1–60 Sekunden. Wartet nach Freigabe der Textbefehle diese Dauer (Standard: 1 Sekunde).", "1 à 60 secondes après l'autorisation des commandes texte (1 seconde par défaut).", "1~60초. 텍스트 명령 허용 후 지정 시간만큼 대기합니다(기본: 1초).", "1至60秒。游戏允许文本命令后等待此时长（默认：1秒）。", "1至60秒。遊戲允許文字指令後等待此時長（預設：1秒）。"),
        ["Automatically look up Lodestone ID on login"] = new("ログイン時にLodestone IDを自動取得する", "Lodestone-ID bei Anmeldung automatisch suchen", "Rechercher automatiquement l'ID Lodestone à la connexion", "로그인 시 로드스톤 ID 자동 검색", "登录时自动查询Lodestone ID", "登入時自動查詢Lodestone ID"),
        ["When enabled, sends the character name and home world to the official Lodestone search."] = new("有効にすると、キャラクター名とホームワールドを公式Lodestoneへ送信して検索します。", "Sendet bei Aktivierung Charaktername und Heimatwelt an die offizielle Lodestone-Suche.", "Envoie le nom et le monde d'origine à la recherche Lodestone officielle.", "활성화하면 캐릭터 이름과 홈 월드를 공식 로드스톤 검색으로 전송합니다.", "启用后，会将角色名和所属服务器发送至官方Lodestone搜索。", "啟用後，會將角色名稱和所屬伺服器傳送至官方Lodestone搜尋。"),
        ["CSV output folder"] = new("CSV出力先", "CSV-Ausgabeordner", "Dossier de sortie CSV", "CSV 출력 폴더", "CSV输出文件夹", "CSV輸出資料夾"),
        ["Restore Default Folder"] = new("既定の出力先に戻す", "Standardordner wiederherstellen", "Restaurer le dossier par défaut", "기본 폴더 복원", "恢复默认文件夹", "還原預設資料夾"),
        ["Open Output Folder"] = new("出力先を開く", "Ausgabeordner öffnen", "Ouvrir le dossier de sortie", "출력 폴더 열기", "打开输出文件夹", "開啟輸出資料夾"),
        ["Could not open the CSV output folder."] = new("CSV出力先を開けませんでした。", "Der CSV-Ausgabeordner konnte nicht geöffnet werden.", "Impossible d'ouvrir le dossier de sortie CSV.", "CSV 출력 폴더를 열 수 없습니다.", "无法打开CSV输出文件夹。", "無法開啟CSV輸出資料夾。"),
        ["Support Roxyz0501's Development"] = new("Roxyz0501の開発を支援", "Die Entwicklung von Roxyz0501 unterstützen", "Soutenir le développement de Roxyz0501", "Roxyz0501의 개발 후원", "支持Roxyz0501的开发", "支援Roxyz0501的開發"),
        ["If this plugin is useful, you can optionally support development through Ko-fi. All features remain available without support."] = new("このプラグインが役に立った場合、Ko-fiから任意で開発を支援できます。支援の有無で機能が変わることはありません。", "Wenn dieses Plugin hilfreich ist, kannst du die Entwicklung freiwillig über Ko-fi unterstützen. Alle Funktionen bleiben auch ohne Unterstützung verfügbar.", "Si ce plugin vous est utile, vous pouvez soutenir son développement via Ko-fi. Toutes les fonctions restent disponibles sans soutien.", "플러그인이 유용하다면 Ko-fi를 통해 선택적으로 개발을 후원할 수 있습니다. 후원하지 않아도 모든 기능을 사용할 수 있습니다.", "如果此插件对你有帮助，可自愿通过Ko-fi支持开发。不支持也可使用全部功能。", "如果此外掛程式對你有幫助，可自願透過Ko-fi支援開發。不支援也可使用全部功能。"),
        ["Recipient: Roxyz0501"] = new("受取人: Roxyz0501", "Empfänger: Roxyz0501", "Bénéficiaire : Roxyz0501", "수령인: Roxyz0501", "收款人：Roxyz0501", "收款人：Roxyz0501"),
        ["Support URL: {0}"] = new("支援先: {0}", "Support-URL: {0}", "URL de soutien : {0}", "후원 URL: {0}", "支持链接：{0}", "支援連結：{0}"),
        ["Support Roxyz0501 on Ko-fi"] = new("Ko-fiでRoxyz0501を支援", "Roxyz0501 auf Ko-fi unterstützen", "Soutenir Roxyz0501 sur Ko-fi", "Ko-fi에서 Roxyz0501 후원", "在Ko-fi上支持Roxyz0501", "在Ko-fi上支援Roxyz0501"),
        ["Could not open the Ko-fi page."] = new("Ko-fiページを開けませんでした。", "Die Ko-fi-Seite konnte nicht geöffnet werden.", "Impossible d'ouvrir la page Ko-fi.", "Ko-fi 페이지를 열 수 없습니다.", "无法打开Ko-fi页面。", "無法開啟Ko-fi頁面。"),
        ["Display language"] = new("表示言語", "Anzeigesprache", "Langue d'affichage", "표시 언어", "显示语言", "顯示語言"),
        ["The language is selected from game/Dalamud settings only on first launch, then remains fixed here."] = new("初回起動時のみゲーム／Dalamud設定から選択し、その後はここで選んだ言語に固定されます。", "Die Sprache wird nur beim ersten Start aus den Spiel-/Dalamud-Einstellungen gewählt und bleibt danach hier festgelegt.", "La langue est choisie depuis le jeu/Dalamud uniquement au premier lancement, puis reste fixée ici.", "언어는 최초 실행 시에만 게임/Dalamud 설정에서 선택되며 이후 여기서 선택한 값으로 유지됩니다.", "语言仅在首次启动时根据游戏/Dalamud设置选择，之后固定为此处的选择。", "語言僅在首次啟動時依遊戲/Dalamud設定選擇，之後固定為此處的選擇。"),
        ["Job information updates automatically after it is loaded into memory."] = new("ジョブ情報はメモリへ読み込まれた後に自動更新されます。", "Jobinformationen werden nach dem Laden in den Speicher automatisch aktualisiert.", "Les informations de jobs sont actualisées après leur chargement en mémoire.", "직업 정보는 메모리에 로드된 후 자동으로 업데이트됩니다.", "职业信息加载到内存后会自动更新。", "職業資訊載入記憶體後會自動更新。"),
        ["Job Name"] = new("ジョブ名", "Jobname", "Nom du job", "직업 이름", "职业名称", "職業名稱"),
        ["Abbreviation"] = new("略称", "Kürzel", "Abréviation", "약칭", "简称", "簡稱"),
        ["Level"] = new("レベル", "Stufe", "Niveau", "레벨", "等级", "等級"),
        ["{0} (Current)"] = new("{0}（カレント）", "{0} (Aktuell)", "{0} (Actuel)", "{0} (현재)", "{0}（当前）", "{0}（目前）"),
        ["Locked"] = new("未開放", "Nicht freigeschaltet", "Non débloqué", "미개방", "未解锁", "未解鎖"),
        ["CSV export completed: {0}"] = new("CSV出力完了: {0}", "CSV-Export abgeschlossen: {0}", "Export CSV terminé : {0}", "CSV 내보내기 완료: {0}", "CSV导出完成：{0}", "CSV匯出完成：{0}"),
        ["CSV export failed."] = new("CSV出力に失敗しました。", "CSV-Export fehlgeschlagen.", "Échec de l'exportation CSV.", "CSV 내보내기 실패.", "CSV导出失败。", "CSV匯出失敗。"),
        ["CSV export failed. Check the destination folder."] = new("CSV出力に失敗しました。保存先を確認してください。", "CSV-Export fehlgeschlagen. Bitte den Zielordner prüfen.", "Échec de l'exportation CSV. Vérifiez le dossier de destination.", "CSV 내보내기에 실패했습니다. 대상 폴더를 확인하세요.", "CSV导出失败。请检查目标文件夹。", "CSV匯出失敗。請檢查目標資料夾。"),
        ["No characters require lookup"] = new("検索対象はありません", "Keine Charaktere benötigen eine Suche", "Aucun personnage à rechercher", "검색할 캐릭터가 없습니다", "没有需要查询的角色", "沒有需要查詢的角色"),
        ["Batch lookup completed: {0}"] = new("まとめて検索完了: {0}件", "Stapelsuche abgeschlossen: {0}", "Recherche groupée terminée : {0}", "일괄 검색 완료: {0}", "批量查询完成：{0}", "批次查詢完成：{0}"),
        ["Batch lookup {0}/{1}: {2}"] = new("まとめて検索中 {0}/{1}: {2}", "Stapelsuche {0}/{1}: {2}", "Recherche groupée {0}/{1} : {2}", "일괄 검색 {0}/{1}: {2}", "批量查询 {0}/{1}：{2}", "批次查詢 {0}/{1}：{2}"),
    };

    public static IReadOnlyCollection<string> Keys => Entries.Keys.ToArray();

    public static bool TryGet(string key, ResolvedLanguage language, out string value)
    {
        if (!Entries.TryGetValue(key, out var entry))
        {
            value = string.Empty;
            return false;
        }

        value = language switch
        {
            ResolvedLanguage.Japanese => entry.Ja,
            ResolvedLanguage.German => entry.De,
            ResolvedLanguage.French => entry.Fr,
            ResolvedLanguage.Korean => entry.Ko,
            ResolvedLanguage.SimplifiedChinese => entry.ZhHans,
            ResolvedLanguage.TraditionalChinese => entry.ZhHant,
            _ => key,
        };
        return true;
    }

    public static IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        foreach (var (key, _) in Entries)
        {
            var expected = PlaceholderPattern().Matches(key).Select(match => match.Value).ToArray();
            foreach (var language in Enum.GetValues<ResolvedLanguage>())
            {
                _ = TryGet(key, language, out var value);
                if (string.IsNullOrWhiteSpace(value))
                    errors.Add($"{language}:{key}:empty");
                var actual = PlaceholderPattern().Matches(value).Select(match => match.Value).ToArray();
                if (!expected.SequenceEqual(actual))
                    errors.Add($"{language}:{key}:placeholders");
            }
        }
        return errors;
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}

namespace ZapretGUI.Localization;

public enum Language { Russian, English }

/// <summary>Simple two-language string table. All UI text goes through here.</summary>
public static class L
{
    public static event Action? Changed;

    static Language _lang = Language.Russian;
    public static Language Lang
    {
        get => _lang;
        set { if (_lang == value) return; _lang = value; Changed?.Invoke(); }
    }

    static string T(string ru, string en) => _lang == Language.English ? en : ru;

    // ── Language toggle ────────────────────────────────────────────────────
    public static string LangToggle => _lang == Language.English ? "RU" : "EN";

    // ── Main form – top bar ────────────────────────────────────────────────
    public static string BrowseBtn        => T("📂 Обзор",        "📂 Browse");
    public static string RefreshBtn       => T("↻ Статус",         "↻ Status");
    public static string PathNotSelected  => T("Папка zapret не выбрана", "Zapret folder not selected");
    public static string PathLabel(string dir) => dir;

    // ── Main form – groups ─────────────────────────────────────────────────
    public static string StatusGroup   => T("Статус",       "Status");
    public static string ServiceGroup  => T("Сервис",       "Service");
    public static string SettingsGroup => T("Настройки",    "Settings");
    public static string UpdatesGroup  => T("Обновления",   "Updates");
    public static string ToolsGroup    => T("Инструменты",  "Tools");
    public static string LogLabel      => T("Вывод:",       "Output:");
    public static string ClearBtn      => T("Очистить",     "Clear");

    // ── Service buttons ────────────────────────────────────────────────────
    public static string InstallBtn      => T("⬇  Установить сервис",  "⬇  Install Service");
    public static string RemoveBtn       => T("✕  Удалить сервисы",    "✕  Remove Services");
    public static string StatusBtn       => T("ℹ  Проверить статус",   "ℹ  Check Status");
    public static string UpdateIpSetBtn  => T("⬇  Обновить IPSet",     "⬇  Update IPSet");
    public static string UpdateHostsBtn  => T("⬇  Обновить hosts",     "⬇  Update hosts");
    public static string CheckUpdatesBtn => T("🔍 Проверить версию",   "🔍 Check for Updates");
    public static string DiagnosticsBtn  => T("🔬 Диагностика",        "🔬 Diagnostics");
    public static string TestsBtn        => T("🧪 Запустить тесты",    "🧪 Run Tests");

    // ── Status labels ──────────────────────────────────────────────────────
    public static string WinwsYes   => T("● winws.exe: ДА",  "● winws.exe: YES");
    public static string WinwsNo    => T("● winws.exe: НЕТ", "● winws.exe: NO");
    public static string StrategyNone => T("Стратегия: —", "Strategy: —");
    public static string StrategyPrefix => T("Стратегия: ", "Strategy: ");

    public static string SvcRunning      => T("● ЗАПУЩЕН",          "● RUNNING");
    public static string SvcStopped      => T("● ОСТАНОВЛЕН",       "● STOPPED");
    public static string SvcPending      => T("● ОСТАНАВЛИВАЕТСЯ",  "● STOPPING");
    public static string SvcNotInstalled => T("● НЕ УСТАНОВЛЕН",    "● NOT INSTALLED");
    public static string SvcUnknown      => T("● НЕИЗВЕСТНО",       "● UNKNOWN");

    // ── Toggle labels ──────────────────────────────────────────────────────
    public static string GfPrefix => T("🎮 Game Filter: ", "🎮 Game Filter: ");
    public static string GfOff    => T("ВЫКЛ",     "OFF");
    public static string GfTcpUdp => T("TCP+UDP",  "TCP+UDP");
    public static string GfTcp    => T("TCP",      "TCP");
    public static string GfUdp    => T("UDP",      "UDP");

    public static string IpSetPrefix => T("📋 IPSet: ", "📋 IPSet: ");
    public static string IpSetAny    => T("ANY (все IP)",       "ANY (all IPs)");
    public static string IpSetNone   => T("NONE (отключён)",    "NONE (disabled)");
    public static string IpSetLoaded => T("LOADED (список)",    "LOADED (list)");

    public static string AutoUpdatePrefix => T("🔄 Авто-обновление: ", "🔄 Auto-Update: ");
    public static string AutoUpdateOn  => T("ВКЛ",  "ON");
    public static string AutoUpdateOff => T("ВЫКЛ", "OFF");

    // ── Log messages ───────────────────────────────────────────────────────
    public static string LogStatusHeader  => T("── Статус сервисов ──────────────────", "── Service Status ───────────────────");
    public static string LogSeparator     => "────────────────────────────────────";
    public static string LogStrategyLbl   => T("Стратегия:  ", "Strategy:   ");
    public static string LogNoDir         => T("Укажите папку zapret через кнопку «Обзор»", "Select the zapret folder via the Browse button");
    public static string LogDirOk(string d)   => T($"Папка zapret: {d}", $"Zapret folder: {d}");
    public static string LogDirInvalid(string d) => T($"Папка не похожа на zapret — bin\\winws.exe не найден: {d}", $"Folder doesn't look like zapret — bin\\winws.exe not found: {d}");
    public static string LogInstalling(string s) => T($"Устанавливаю сервис со стратегией: {s}", $"Installing service, strategy: {s}");
    public static string LogInstalled(string s)  => T($"Сервис установлен: {s}", $"Service installed: {s}");
    public static string LogRemoving             => T("Удаляю сервисы...", "Removing services...");
    public static string LogRemoved              => T("Сервисы удалены", "Services removed");
    public static string LogDiagHeader           => T("── Диагностика ──────────────────────", "── Diagnostics ──────────────────────");
    public static string LogDiagDone             => T("Диагностика завершена", "Diagnostics complete");
    public static string LogDiagCancelled        => T("Диагностика отменена", "Diagnostics cancelled");
    public static string LogIpSetChanged(string a, string b) => T($"IPSet: {a} → {b}", $"IPSet: {a} → {b}");
    public static string LogAutoUpdate(string s) => T($"Авто-обновление: {s}", $"Auto-Update: {s}");
    public static string LogGameFilterChanged    => T("Game Filter изменён — перезапустите сервис для применения", "Game Filter changed — restart service to apply");
    public static string LogCheckingVersion(string v) => T($"Проверяю версию (локальная: {v})...", $"Checking version (local: {v})...");
    public static string LogUpdateAvailable(string v) => T($"Доступна новая версия: {v}", $"New version available: {v}");
    public static string LogNoUpdate(string v)        => T($"Установлена актуальная версия: {v}", $"Latest version installed: {v}");
    public static string LogVersionFailed             => T("Не удалось получить версию с GitHub", "Failed to fetch version from GitHub");
    public static string LogReleaseUrl(string u)      => T($"Страница релиза: {u}", $"Release page: {u}");
    public static string LogUpdatingIpSet             => T("Загружаю IPSet список...", "Downloading IPSet list...");
    public static string LogDownloading(int p)        => T($"  Загружено: {p}%", $"  Downloaded: {p}%");
    public static string LogCheckingHosts             => T("Проверяю файл hosts...", "Checking hosts file...");
    public static string LogHostsNote                 => T("Файл из репозитория открыт в Блокноте. Скопируйте содержимое в ваш hosts.", "Repository hosts opened in Notepad. Copy its content into your hosts file.");
    public static string LogDiscordCacheCleared       => T("Кэш Discord очищен", "Discord cache cleared");
    public static string LogConflictsRemoved          => T("Конфликтующие службы удалены", "Conflicting services removed");
    public static string LogConflictsFound(string s)  => T($"Конфликтующие службы: {s}", $"Conflicting services: {s}");
    public static string LogNoBats                    => T("В папке zapret не найдено .bat файлов со стратегиями", "No strategy .bat files found in zapret folder");
    public static string LogResultsSaved(string p)    => T($"Результаты сохранены в: {p}", $"Results saved to: {p}");
    public static string LogTestsCancelled            => T("Тестирование отменено", "Testing cancelled");
    public static string LogTestsError(string e)      => T($"Ошибка: {e}", $"Error: {e}");

    // ── Dialog strings ─────────────────────────────────────────────────────
    public static string ConfirmTitle         => T("Подтверждение",    "Confirm");
    public static string ConfirmRemove        => T("Удалить сервис zapret и WinDivert?", "Remove zapret and WinDivert services?");
    public static string UpdateTitle          => T("Обновление",       "Update");
    public static string UpdateQuestion(string v) => T($"Доступна версия {v}. Открыть страницу загрузки?", $"Version {v} is available. Open the download page?");
    public static string DiagClearDiscord     => T("Очистить кэш Discord?", "Clear Discord cache?");
    public static string DiagTitle            => T("Диагностика",      "Diagnostics");
    public static string ConflictTitle        => T("Конфликт",         "Conflict");
    public static string ConflictQuestion(string s) => T($"Обнаружены конфликтующие службы:\n{s}\n\nУдалить их?", $"Conflicting services found:\n{s}\n\nRemove them?");
    public static string StrategySelectTitle  => T("Выберите стратегию", "Select Strategy");
    public static string StrategySelectLabel  => T("Выберите .bat файл для установки сервиса:", "Select a .bat file for service installation:");
    public static string InstallBtnDlg        => T("Установить",  "Install");
    public static string CancelBtnDlg         => T("Отмена",      "Cancel");
    public static string GameFilterTitle       => T("Game Filter", "Game Filter");
    public static string GfOptionOff           => T("Отключён",           "Disabled");
    public static string GfOptionTcpUdp        => T("TCP и UDP (1024-65535)", "TCP and UDP (1024-65535)");
    public static string GfOptionTcp           => T("Только TCP",         "TCP only");
    public static string GfOptionUdp           => T("Только UDP",         "UDP only");
    public static string NoSelectionMsg        => T("Выберите хотя бы одну конфигурацию.", "Select at least one configuration.");
    public static string NoSelectionTitle      => T("Нет выбора",         "No Selection");
    public static string RemoveForTestsMsg     => T("Сервис zapret запущен. Для тестов его нужно удалить.\nУдалить сейчас?", "Zapret service is running. It must be removed for testing.\nRemove now?");
    public static string ServiceActiveTitle    => T("Сервис zapret активен", "Zapret service active");
    public static string ServiceRemovedForTest => T("Сервис zapret удалён. После тестов установите заново.", "Zapret service removed. Re-install after tests.");

    // ── TestForm ───────────────────────────────────────────────────────────
    public static string TestFormTitle   => T("Тестирование стратегий", "Strategy Testing");
    public static string TestTypeGroup   => T("Тип тестирования",       "Test Type");
    public static string TestStandard    => T("Стандартный (HTTP + Ping)", "Standard (HTTP + Ping)");
    public static string TestDpi         => T("DPI-проверка (TCP 16–20 KB, требует curl.exe)", "DPI Check (TCP 16–20 KB freeze, requires curl.exe)");
    public static string ConfigsGroup    => T("Конфигурации",   "Configurations");
    public static string SelectAll       => T("Выбрать все",    "Select All");
    public static string SelectNone      => T("Снять все",      "Deselect All");
    public static string RunBtn          => T("▶  Запустить тесты", "▶  Run Tests");
    public static string StopBtn         => T("✕  Остановить",  "✕  Stop");
    public static string OpenResultsBtn  => T("📂  Открыть результаты", "📂  Open Results");
    public static string OutputGroup     => T("Вывод",          "Output");
    public static string TestReady       => T("Готов",          "Ready");
    public static string TestRunning(string type, int count) =>
        T($"Выполняется: {type} | {count} конфиг(ов)...", $"Running: {type} | {count} config(s)...");
    public static string TestBestConfig(string name) =>
        T($"Готово ✓  |  Лучшая: {name}", $"Done ✓  |  Best: {name}");
    public static string TestCancelled      => T("Отменено",    "Cancelled");
    public static string TestErrorStatus    => T("Ошибка",      "Error");
    public static string CurlMissing        => T("curl.exe не найден в PATH — DPI-тесты недоступны. Установите curl или обновите Windows.", "curl.exe not found in PATH — DPI tests unavailable. Install curl or update Windows.");
    public static string CurlMissingForDpi  => T("curl.exe не найден — DPI-тесты недоступны.", "curl.exe not found — DPI tests unavailable.");
    public static string ServiceRunningWarn => T("ВНИМАНИЕ: Сервис zapret запущен. Удалите его перед тестами.", "WARNING: Zapret service is running. Remove it before testing.");
    public static string CancellingStatus   => T("Отмена...",   "Cancelling...");

    // ── Diagnostics items ──────────────────────────────────────────────────
    public static string DiagBfeName     => "Base Filtering Engine";
    public static string DiagBfeOk       => T("Работает корректно",            "Running correctly");
    public static string DiagBfeErr      => T("Служба не запущена — zapret не будет работать", "Service not running — zapret will not work");
    public static string DiagProxyName   => T("Системный прокси",  "System Proxy");
    public static string DiagProxyOk     => T("Прокси не обнаружен", "No proxy detected");
    public static string DiagProxyWarn(string s) => T($"Включён прокси: {s}", $"Proxy enabled: {s}");
    public static string DiagProxyDetail => T("Убедитесь что прокси корректен или отключите его", "Verify proxy is valid or disable it");
    public static string DiagTcpName     => "TCP Timestamps";
    public static string DiagTcpOk       => T("Включены",                      "Enabled");
    public static string DiagTcpFixed    => T("Были выключены — включено автоматически", "Were disabled — enabled automatically");
    public static string DiagAdguardName => "Adguard";
    public static string DiagAdguardOk   => T("Не обнаружен", "Not detected");
    public static string DiagAdguardErr  => T("Обнаружен AdguardSvc — может конфликтовать с Discord", "AdguardSvc detected — may conflict with Discord");
    public static string DiagKillerName  => "Killer";
    public static string DiagKillerOk    => T("Не обнаружен", "Not detected");
    public static string DiagKillerErr   => T("Обнаружены службы Killer — конфликтуют с zapret", "Killer services detected — conflict with zapret");
    public static string DiagIntelName   => "Intel Connectivity";
    public static string DiagIntelOk     => T("Не обнаружен", "Not detected");
    public static string DiagIntelErr    => T("Intel Connectivity Network Service обнаружен — конфликтует с zapret", "Intel Connectivity Network Service detected — conflicts with zapret");
    public static string DiagCpName      => "Check Point";
    public static string DiagCpOk        => T("Не обнаружен", "Not detected");
    public static string DiagCpErr       => T("Обнаружены службы Check Point — конфликтуют с zapret", "Check Point services detected — conflict with zapret");
    public static string DiagSbName      => "SmartByte";
    public static string DiagSbOk        => T("Не обнаружен", "Not detected");
    public static string DiagSbErr       => T("SmartByte обнаружен — конфликтует с zapret. Отключите через services.msc", "SmartByte detected — conflicts with zapret. Disable via services.msc");
    public static string DiagVpnName     => "VPN";
    public static string DiagVpnOk       => T("VPN-служб не обнаружено",    "No VPN services detected");
    public static string DiagVpnWarn(string s) => T($"Обнаружены VPN-службы: {s}", $"VPN services detected: {s}");
    public static string DiagVpnDetail   => T("Убедитесь что VPN отключён при использовании zapret", "Make sure VPN is disabled when using zapret");
    public static string DiagDnsName     => T("Безопасный DNS", "Secure DNS");
    public static string DiagDnsOk       => T("Зашифрованный DNS настроен",   "Encrypted DNS configured");
    public static string DiagDnsWarn     => T("Зашифрованный DNS не настроен", "Encrypted DNS not configured");
    public static string DiagDnsDetail   => T("Настройте DoH в браузере или параметрах Windows 11", "Configure DoH in your browser or Windows 11 settings");
    public static string DiagHostsName   => T("Файл hosts", "Hosts file");
    public static string DiagHostsOk     => T("Подозрительных записей нет",   "No suspicious entries");
    public static string DiagHostsWarn   => T("Найдены записи youtube.com/youtu.be — может мешать YouTube", "Entries for youtube.com/youtu.be found — may affect YouTube");
    public static string DiagWdName      => T("WinDivert конфликт", "WinDivert conflict");
    public static string DiagWdOk        => T("Конфликтов не обнаружено",   "No conflicts detected");
    public static string DiagWdFixed     => T("winws.exe не запущен, WinDivert активен — удалено", "winws.exe not running, WinDivert active — removed");
    public static string DiagCflName     => T("Конфликтующие bypass", "Conflicting bypasses");
    public static string DiagCflOk       => T("Не обнаружено", "None detected");
    public static string DiagCflErr(string s) => T($"Конфликтующие службы: {s}", $"Conflicting services: {s}");
    public static string DiagCflDetail   => T("Удалите через кнопку ниже или services.msc", "Remove via button below or services.msc");
    public static string DiagSysFileName => "WinDivert64.sys";
    public static string DiagSysFileOk   => T("Файл найден", "File found");
    public static string DiagSysFileErr  => T("WinDivert64.sys не найден в папке bin", "WinDivert64.sys not found in bin folder");
    public static string DiagErrorName   => T("Ошибка", "Error");

    // ── TestService log messages ───────────────────────────────────────────
    public static string TsHeader(string type, int count) =>
        T($"Стандартные тесты: {count} конфиг(ов)", $"Standard tests: {count} config(s)");
    public static string TsDpiHeader(int count) =>
        T($"DPI-тесты: {count} хостов (параллельно)...", $"DPI tests: {count} hosts (parallel)...");
    public static string TsConfig(int i, int total, string name) =>
        T($"[{i}/{total}] {name}", $"[{i}/{total}] {name}");
    public static string TsStarting    => T("Запускаю конфиг...",                  "Starting config...");
    public static string TsTesting     => T("Тестирую цели (параллельно)...",      "Testing targets (parallel)...");
    public static string TsAnalytics   => T("АНАЛИТИКА",     "ANALYTICS");
    public static string TsBest(string s) => T($"Лучшая конфигурация: {s}", $"Best configuration: {s}");
    public static string TsFetchingDpi => T("Загружаю DPI-набор целей...",         "Fetching DPI target suite...");
    public static string TsDpiUnavail  => T("DPI-набор недоступен — проверьте интернет-соединение", "DPI suite unavailable — check your internet connection");
    public static string TsIpSetSwitch(string s) => T($"IPSet временно переключается в 'any' (было: {s})", $"IPSet temporarily switched to 'any' (was: {s})");
    public static string TsIpSetFail(string e)   => T($"Не удалось переключить IPSet: {e}", $"Failed to switch IPSet: {e}");
    public static string TsIpSetRestored         => T("IPSet восстановлен", "IPSet restored");
    public static string TsIpSetRestoreFail(string e) => T($"Не удалось восстановить IPSet: {e}", $"Failed to restore IPSet: {e}");
}

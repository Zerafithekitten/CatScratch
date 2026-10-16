using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AionMeter.Core.Combat;
using AionMeter.Core.Updates;

namespace AionMeter.App.Services;

public enum HistoryMode
{
    BossesOnly,
    BossesAndLong,
    All,
}

public sealed class AppSettings
{
    public static string AppDataDir { get; } =
        Path.Combine(AppContext.BaseDirectory, "userdata");

    private static string FilePath => Path.Combine(AppDataDir, "settings.json");

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    // Overlay window
    public double OverlayLeft { get; set; } = 80;
    public double OverlayTop { get; set; } = 160;
    public double OverlayWidth { get; set; } = 420;
    public double OverlayHeight { get; set; } = 540;
    public bool CompactMode { get; set; } = true;
    public double UiScale { get; set; } = 1;
    public bool StreamMode { get; set; }
    public bool AutomaticFightEnd { get; set; } = true;
    public double WindowOpacity { get; set; } = 0.88;
    public double BackgroundOpacity { get; set; } = 0.9;
    public bool Locked { get; set; }
    public bool ClickThrough { get; set; }
    public bool OverlayVisible { get; set; } = true;
    public Dictionary<string,bool> ModulePins {get;set;}=[];
    /// <summary>Bring the overlay up by itself when a boss is fought nearby or you hit anything.</summary>
    public bool AutoShow { get; set; } = true;
    /// <summary>An overlay that came up by itself hides again after this long out of combat; 0 = stays up.</summary>
    public int AutoHideSeconds { get; set; } = 0;
    /// <summary>Bring the overlay up when the meter starts and when it finds the game, so a player sees it is running.</summary>
    public bool ShowOnStart { get; set; } = true;
    public bool FollowGame { get; set; }
    public string GameExecutable { get; set; } = "";

    // Boss respawn timers
    /// <summary>Tray notification this many minutes before a tracked boss respawns; 0 = off.</summary>
    public int BossAlertMinutes { get; set; } = 2;

    // Meter
    public TargetMode TargetMode { get; set; } = TargetMode.BossOnly;
    /// <summary>True: the leader's bar is full and the rest are sized against it. False: bars show share of party damage.</summary>
    public bool BarsRelativeToTop { get; set; } = true;
    public int MaxRows { get; set; } = 10;
    public const int MinRowSize = 50, MaxRowSize = 100;
    /// <summary>Overlay row size in percent of the original (Settings → Overlay, or Ctrl + wheel over the rows).</summary>
    public int RowSize { get; set; } = 100;
    /// <summary>Gear score and combat power after the names in the overlay (when the party roster gave them).</summary>
    public bool ShowGear { get; set; } = true;
    public bool? ShowCombatPower { get; set; }
    public bool ShowPersonalCombatModule { get; set; }
    public bool ShowDeathRecapModule { get; set; } = true;
    public bool ShowPetTrackerModule {get;set;}
    public int PetGoal {get;set;}
    public bool PetNotify {get;set;}
    public double PetModuleWidth {get;set;}=300;
    public double PetModuleHeight {get;set;}=100;
    public double PetModuleLeft {get;set;}=140;
    public double PetModuleTop {get;set;}=140;
    public bool PersonalCombatCollapsed { get; set; }
    public bool ShowHealingDone { get; set; } = true;
    public double PersonalModuleLeft { get; set; } = 100;
    public double PersonalModuleTop { get; set; } = 100;
    public double PersonalModuleWidth { get; set; } = 320;
    public bool ShowBossPanel { get; set; } = true;
    public int IdleTimeoutSec { get; set; } = 10;
    public int BossIdleTimeoutSec { get; set; } = 30;

    /// <summary>Interface and skill / NPC name language: "en" (default), "de", "ru" or "fr".</summary>
    public string Language { get; set; } = "en";

    /// <summary>Fetch skill icons from the official AION 2 game-data CDN (cached locally).</summary>
    public bool DownloadIcons { get; set; } = true;

    // Updates
    /// <summary>Ask GitHub for a newer version shortly after start and every few hours.</summary>
    public bool CheckUpdates { get; set; } = true;
    public bool BackgroundUpdateDefaultsApplied { get; set; }
    // Older copies have no preference: follow the installed build until explicitly selected.
    public string? UpdateChannel { get; set; }
    public string? LastStartedRelease { get; set; }
    /// <summary>Promoted Stable builds return Beta subscribers to Stable once per actual build transition.</summary>
    internal bool ApplyReleaseTransition(ReleaseVersion current)
    {
        var previous = ReleaseVersion.Parse(LastStartedRelease);
        var promoted = current.Channel == Core.Updates.UpdateChannel.Stable && previous != current
            && Enum.TryParse<UpdateChannel>(UpdateChannel, true, out var selected) && selected == Core.Updates.UpdateChannel.Beta;
        if (promoted) { UpdateChannel = Core.Updates.UpdateChannel.Stable.ToString(); SkippedUpdate = null; }
        LastStartedRelease = current.Display;
        return promoted;
    }
    /// <summary>Installed copies download a new version by themselves and install it at a quiet moment.</summary>
    public bool AutoInstallUpdates { get; set; } = false;
    public string UpdateSource { get; set; } = "";
    /// <summary>A version the user chose to skip ("0.2.0"): not announced again, a newer one is.</summary>
    public string? SkippedUpdate { get; set; }

    // Capture / storage
    public bool SaveHistory { get; set; } = false;
    public bool ManualHistoryDefaultsApplied { get; set; }
    public HistoryMode HistoryMode { get; set; } = HistoryMode.BossesAndLong;
    /// <summary>For <see cref="HistoryMode.BossesAndLong"/>: non-boss fights shorter than this are not saved.</summary>
    public int HistoryMinSeconds { get; set; } = 20;
    public bool RecordPackets { get; set; }
    public bool RecentDebugEnabled { get; set; }
    public string? CaptureDevice { get; set; }

    // Hotkeys (modifier+key, parsed by HotkeyManager)
    public string HotkeyToggleAll { get; set; } = "Shift+F10";
    public string HotkeyToggleOverlay { get; set; } = "Shift+F5";
    public string HotkeyReset { get; set; } = "Shift+F12";
    public string HotkeyClickThrough { get; set; } = "Shift+F11";
    public string HotkeyPersonal { get; set; } = "Shift+F6";
    public string HotkeyDeathRecap { get; set; } = "Shift+F4";
    public string HotkeyEvents { get; set; } = "Shift+F7";
    public string HotkeyPets { get; set; } = "Shift+F9";
    public string HotkeyTimers { get; set; } = "Shift+F8";

    /// <summary>Bumped when the overlay layout changes enough that saved sizes no longer fit.</summary>
    public int LayoutVersion { get; set; } // 0 when missing from an older settings file; new installs get it in Load()
    private const int CurrentLayout = 7;

    public int HotkeyDefaultsVersion {get;set;}
    internal void MigrateHotkeys()
    {
        if(HotkeyDefaultsVersion>=1)return;
        if(string.Equals(HotkeyToggleOverlay,"Ctrl+Shift+D",StringComparison.OrdinalIgnoreCase))HotkeyToggleOverlay="Shift+F5";
        if(string.Equals(HotkeyPersonal,"Ctrl+Shift+C",StringComparison.OrdinalIgnoreCase))HotkeyPersonal="Shift+F6";
        if(string.Equals(HotkeyEvents,"Ctrl+Shift+E",StringComparison.OrdinalIgnoreCase))HotkeyEvents="Shift+F7";
        if(string.Equals(HotkeyTimers,"Ctrl+Shift+T",StringComparison.OrdinalIgnoreCase))HotkeyTimers="Shift+F8";
        if(string.Equals(HotkeyPets,"Ctrl+Shift+P",StringComparison.OrdinalIgnoreCase))HotkeyPets="Shift+F9";
        if(string.Equals(HotkeyToggleAll,"Ctrl+Shift+H",StringComparison.OrdinalIgnoreCase))HotkeyToggleAll="Shift+F10";
        if(string.Equals(HotkeyClickThrough,"Ctrl+Shift+L",StringComparison.OrdinalIgnoreCase))HotkeyClickThrough="Shift+F11";
        if(string.Equals(HotkeyReset,"Ctrl+Shift+R",StringComparison.OrdinalIgnoreCase))HotkeyReset="Shift+F12";
        HotkeyDefaultsVersion=1;
    }
    public static AppSettings Load()
    {
        try
        {
            if (JsonSettingsFile.Read<AppSettings>(FilePath, Json) is { } s)
            {
                if (s.LayoutVersion < 3)
                {
                    // v3 = card layout (portrait header, two-line rows): the old compact size is too small for it.
                    s.OverlayWidth = Math.Max(s.OverlayWidth, 470);
                    s.OverlayHeight = Math.Max(s.OverlayHeight, 540);
                }
                if (s.LayoutVersion < 4)
                {
                    // v4 = English / Russian switch. Earlier builds picked the language from Windows on their own;
                    // the default is now English until the user chooses.
                    s.Language = "en";
                }
                if (s.LayoutVersion < 5)
                {
                    s.CompactMode = true;
                    s.OverlayWidth = 380;
                    s.OverlayHeight = 340;
                    s.WindowOpacity = 0.88;
                }
                if (s.LayoutVersion < 6)
                {
                    s.CompactMode = true;
                    s.OverlayWidth = 300;
                    s.OverlayHeight = 300;
                }
                s.Language = UiText.Normalize(s.Language);
                if (s.LayoutVersion < 7)
                {
                    s.OverlayWidth = Math.Max(s.OverlayWidth, 380);
                    s.OverlayHeight = Math.Max(s.OverlayHeight, 540);
                }
                s.MigrateHotkeys();
                s.ModulePins ??= [];
                s.RowSize = Math.Clamp(s.RowSize, MinRowSize, MaxRowSize);
                s.MaxRows = Math.Clamp(s.MaxRows, 1, 20);
                s.UiScale = Math.Clamp(s.UiScale, .75, 1.5);
                s.WindowOpacity = Math.Clamp(s.WindowOpacity, .4, 1);
                s.BackgroundOpacity = Math.Clamp(s.BackgroundOpacity, .15, 1);
                s.LayoutVersion = CurrentLayout;
                return s;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
        }
        return new AppSettings { LayoutVersion = CurrentLayout, HotkeyDefaultsVersion=1 };
    }

    /// <summary>Throw-away settings (render tool): never written to disk.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Transient { get; set; }

    internal AppSettings CreateDraft()
    {
        var draft = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this, Json), Json)!;
        draft.Transient = true;
        return draft;
    }

    public void Save()
    {
        if (Transient) return;
        JsonSettingsFile.Write(FilePath, this, Json);
    }

    public void ApplyTo(MeterOptions options)
    {
        options.TargetMode = TargetMode;
        options.AutomaticFightEnd = AutomaticFightEnd;
        options.IdleTimeoutMs = Math.Clamp(IdleTimeoutSec, 3, 120) * 1000;
        options.BossIdleTimeoutMs = Math.Clamp(BossIdleTimeoutSec, 5, 300) * 1000;
    }
}

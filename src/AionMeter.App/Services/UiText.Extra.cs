namespace AionMeter.App.Services;

public sealed partial class UiText
{
    // Exact UI literals only. Never translate player names, paths, sound IDs, protocol values or patch notes.
    private static readonly Lazy<IReadOnlyDictionary<string,string>> GermanLiterals = new(CreateGermanLiterals);
    private static readonly Lazy<IReadOnlyDictionary<string,string>> RussianLiterals = new(CreateRussianLiterals);
    private static readonly Lazy<IReadOnlyDictionary<string,string>> FrenchLiterals = new(CreateFrenchLiterals);
    private static IReadOnlyDictionary<string,string> CreateGermanLiterals() => CreateLiterals(German,ExtraGerman);
    private static IReadOnlyDictionary<string,string> CreateRussianLiterals() => CreateLiterals(Ru,ExtraRussian);
    private static IReadOnlyDictionary<string,string> CreateFrenchLiterals() => CreateLiterals(French,ExtraFrench);
    private static IReadOnlyDictionary<string,string> CreateLiterals(UiText translated, IReadOnlyDictionary<string,string> extra)
    {
        var map = new Dictionary<string,string>(extra,StringComparer.Ordinal);
        var english = new UiText();
        foreach (var property in typeof(UiText).GetProperties().Where(p=>p.PropertyType==typeof(string) && p.SetMethod is not null))
        {
            var source=(string?)property.GetValue(english); var target=(string?)property.GetValue(translated);
            if(source is not null && target is not null && source!=target) map.TryAdd(source,target);
        }
        return map;
    }
    public string Localize(string english)
    {
        var literals = Code switch { "de" => GermanLiterals.Value, "ru" => RussianLiterals.Value, "fr" => FrenchLiterals.Value, _ => null };
        return literals is not null && literals.TryGetValue(english, out var value) ? value : english;
    }
    private static readonly (string Prefix,string Translation)[] CapturePrefixes = [("Capturing · ","Erfassung · "),("Listening on ","Lausche auf "),("Capture error: ","Erfassungsfehler: ")];
    public string CaptureMessage(string english)
    {
        if(Code=="en") return english;
        foreach(var (prefix,translated) in CapturePrefixes)
            if(english.StartsWith(prefix,StringComparison.Ordinal)) return (Code=="de" ? translated : Localize(prefix))+english[prefix.Length..];
        return Localize(english);
    }
    private static readonly Dictionary<string,string> ExtraGerman = new(StringComparer.Ordinal)
    {
        ["Boss personal bests"]="Persönliche Bossrekorde",
        ["Most healing"]="Höchste Heilung",
        ["Healing done"]="Verursachte Heilung",
        ["Only real bosses. Records require a confirmed kill, a known starting HP baseline and boss-only damage. Deaths are observed deaths of your own character; missing packets may omit deaths. Data stays on this PC."]="Nur echte Bosse. Rekorde erfordern einen bestätigten Kill, bekannte Start-HP und ausschließlich Bossschaden. Gezählt werden beobachtete Tode deines Charakters; fehlende Pakete können Tode auslassen. Die Daten bleiben auf diesem PC.",
        ["Recent boss attempts · last 10"]="Letzte Bossversuche · maximal 10",
        ["No boss attempts yet. Finish a live boss fight to begin."]="Noch keine Bossversuche. Schließe einen echten Bosskampf ab, um zu beginnen.",
        ["{0} attempts · {1} kills · {2} own deaths"]="{0} Versuche · {1} Kills · {2} eigene Tode",
        ["Best DPS"]="Bester DPS",
        ["Most damage"]="Höchster Schaden",
        ["Strongest hit"]="Stärkster Treffer",
        ["Own deaths: {0}"]="Eigene Tode: {0}",
        ["Listed attempt · not eligible for a record"]="Erfasster Versuch · kein gültiger Rekordkampf",
        ["No qualifying kill yet"]="Noch kein gültiger Kill",
        ["Stopped"]="Beendet",
        ["Zone changed"]="Gebiet gewechselt",
        ["Update channel"]="Update-Kanal",
        ["Stable · tested releases"]="Stable · getestete Versionen",
        ["Beta · early access"]="Beta · Vorabversionen",
        ["Beta releases may contain unfinished features. Changing the channel never installs an update. Returning to Stable can install an older version; your settings and saved fights are kept."]="Beta-Versionen können unfertige Funktionen enthalten. Ein Kanalwechsel installiert kein Update. Bei der Rückkehr zu Stable kann eine ältere Version installiert werden; Einstellungen und gespeicherte Kämpfe bleiben erhalten.",
        ["UI scale"]="UI-Skalierung",
        ["Test mode"]="Testmodus",
        ["Test mode · sample data"]="Testmodus · Beispieldaten",
        ["Stream mode: hide player names"]="Stream-Modus: Spielernamen verbergen",
        ["Only displayed and copied player names are hidden. Local recordings and saved files keep their original data."]="Nur angezeigte und kopierte Spielernamen werden verborgen. Lokale Mitschnitte und gespeicherte Dateien behalten ihre Originaldaten.",
        ["Automatic fight end"]="Automatisches Kampfende",
        ["After inactivity, keep the finished fight until the next fight. Healing and incoming damage count as activity."]="Nach Inaktivität bleibt der abgeschlossene Kampf bis zum nächsten Kampf sichtbar. Heilung und erlittenener Schaden zählen als Aktivität.",
        ["Start test mode"]="Testmodus starten",
        ["Stop test mode"]="Testmodus beenden",
        ["Use sample fights and pet progress to arrange your modules. Test data is not saved."]="Ordne deine Module mit Beispielkämpfen und Pet-Fortschritt an. Testdaten werden nicht gespeichert.",
        ["Player {0}"]="Spieler {0}",
        ["Custom audio"]="Eigene Audiodatei",
        ["Choose audio…"]="Audiodatei wählen…",
        ["Change audio…"]="Audiodatei ändern…",
        ["The selected audio file no longer exists. Choose it again."]="Die gewählte Audiodatei existiert nicht mehr. Wähle sie erneut aus.",
        ["The selected audio file is empty."]="Die gewählte Audiodatei ist leer.",
        ["The selected audio file exceeds 10 MB. Choose a smaller file."]="Die gewählte Audiodatei ist größer als 10 MB. Wähle eine kleinere Datei.",
        ["Unsupported audio format. Choose a WAV or MP3 file."]="Nicht unterstütztes Audioformat. Wähle eine WAV- oder MP3-Datei.",
        ["Windows could not decode this audio file or determine its duration. Choose another WAV or MP3 file."]="Windows konnte diese Audiodatei nicht lesen oder ihre Dauer nicht ermitteln. Wähle eine andere WAV- oder MP3-Datei.",
        ["This sound is {0:0.00} seconds long. Maximum: 10 seconds. Choose a shorter file."]="Dieser Sound ist {0:0.00} Sekunden lang. Maximum: 10 Sekunden. Wähle eine kürzere Datei.",
        ["Custom audio: WAV or MP3, maximum 10 seconds and 10 MB."]="Eigene Audiodatei: WAV oder MP3, höchstens 10 Sekunden und 10 MB.",
        ["Choose a local WAV or MP3 file (up to 10 seconds and 10 MB)"]="Lokale WAV- oder MP3-Datei wählen (höchstens 10 Sekunden und 10 MB)",
        ["Choose an existing WAV or MP3 file first."]="Zuerst eine vorhandene WAV- oder MP3-Datei wählen.",
        ["Choose a valid WAV or MP3 file, up to 10 seconds and 10 MB."]="Wähle eine gültige WAV- oder MP3-Datei mit höchstens 10 Sekunden und 10 MB.",
        ["The audio file could not be opened. Choose another WAV or MP3 file."]="Die Audiodatei konnte nicht geöffnet werden. Wähle eine andere WAV- oder MP3-Datei.",
        ["Tracked world bosses use the watched-boss and ‘Notify before respawn’ settings in Boss timers. Custom audio files must stay at their selected location."]="Verfolgte Weltbosse verwenden die Beobachtungs- und Benachrichtigungseinstellungen der Boss-Timer. Eigene Audiodateien müssen am ausgewählten Ort bleiben.",
        ["Recent debug buffer (60 seconds / 32 MB, experimental)"]="Debug-Rückblick (60 Sekunden / 32 MB, experimentell)",
        ["Save recent debug"]="Debug-Rückblick speichern",
        ["Enable and save settings first. High traffic can shorten the recorded minute."]="Zuerst aktivieren und Einstellungen speichern. Viel Datenverkehr kann die aufgezeichnete Minute verkürzen.",
        ["No recent packets available."]="Keine aktuellen Pakete verfügbar.",
        ["Live capture is not available."]="Live-Erfassung ist nicht verfügbar.",
        ["Copy line {0} of {1}"]="Zeile {0} von {1} kopieren", ["Started · {0} ago"]="Gestartet · vor {0}",
        ["Toggle click-through"]="Durchklicken umschalten",
        ["Double-click to open a separate window."]="Doppelklicken, um ein eigenes Fenster zu öffnen.",
        ["Right-click to open a separate window."]="Rechtsklicken, um ein eigenes Fenster zu öffnen.",
        ["Could not open the browser."] = "Der Browser konnte nicht geöffnet werden.",
        ["Show / hide upcoming events"] = "Anstehende Events ein-/ausblenden",
        ["Show / hide pet tracker"] = "Pet-Tracker ein-/ausblenden",
        ["Show / hide all windows"] = "Alle Fenster ein-/ausblenden",
        ["Attack a pet source to track its souls."] = "Greife einen Pet-Farmgegner an, um die Seelen anzuzeigen.",
        ["No confirmed pet for this target."] = "Für diesen Gegner ist kein Pet bestätigt.",
        ["Attack a monster to see pet progress."] = "Greife ein Monster an, um den Pet-Fortschritt zu sehen.",
        ["Pet progress data not received yet."] = "Pet-Fortschrittsdaten noch nicht empfangen.",
        ["Last known progress; waiting for fresh login data."] = "Zuletzt bekannter Fortschritt; warte auf aktuelle Login-Daten.",
        ["Pet tracker"]="Pet-Tracker",["Automatic phases"]="Automatische Phasen",["Collect every pet"]="Alle Pets sammeln",["Every pet to level 2"]="Alle Pets auf Stufe 2",["Every pet to level 3"]="Alle Pets auf Stufe 3",["All genera"]="Alle Gattungen",["Search pets"]="Pets suchen",["Show needed pets only"]="Nur benötigte Pets anzeigen",["Open farming source"]="Farmquelle öffnen",["Waiting for pet collection data. Change zones once if needed."]="Warte auf Pet-Sammlungsdaten. Falls nötig einmal das Gebiet wechseln.",["Collected"]="Gesammelt",["Goal"]="Ziel",["Max level"]="Maximalstufe",["Select a pet and open its farming source."]="Pet auswählen und Farmquelle öffnen.",["{0} souls left"]="{0} Seelen fehlen",
        ["All factions"]="Alle Fraktionen",["Elyos"]="Elyos",["Asmodians"]="Asmodier",["Sound on collection / level up"]="Ton bei Freischaltung / Stufenaufstieg",["No confirmed pet sources nearby."]="Keine bestätigten Pet-Farmquellen in der Nähe.",["Nearby"]="In der Nähe",["You"]="Du",
        ["FEEDBACK"]="FEEDBACK", ["Report a bug / request a feature"]="Bug melden / Wunsch einreichen",
        ["Opens GitHub in your browser. Choose a bug report or feature request. A GitHub account is required. Nothing is submitted automatically; no logs or captures are attached."]="Öffnet GitHub im Browser. Wähle eine Fehlermeldung oder einen Funktionswunsch. Ein GitHub-Konto ist erforderlich. Nichts wird automatisch gesendet; keine Protokolle oder Mitschnitte werden angehängt.",
        ["Could not open the browser. Please visit GitHub and choose New issue."]="Browser konnte nicht geöffnet werden. Öffne GitHub und wähle New issue.",
        ["Appearance"]="Darstellung", ["Combat"]="Kampf", ["Hotkeys"]="Tastenkürzel", ["History"]="Verlauf", ["Capture & data"]="Erfassung & Daten", ["General"]="Allgemein",
        ["Starting capture…"]="Erfassung wird gestartet…", ["Capture stopped"]="Erfassung beendet", ["Npcap is not installed — get it from npcap.com"]="Npcap ist nicht installiert – erhältlich auf npcap.com", ["Selected network adapter not found"]="Gewählter Netzwerkadapter nicht gefunden", ["AION 2 is not running"]="AION 2 läuft nicht", ["Waiting for the game to connect…"]="Warte auf die Spielverbindung…", ["No Npcap adapter carries the game connection (VPN? pick one in Settings)"]="Kein Npcap-Adapter mit Spielverbindung gefunden (VPN? Adapter in Einstellungen wählen)", ["Connected · no game traffic for a while"]="Verbunden · seit einiger Zeit kein Spielverkehr", ["Demo fight running"]="Demo-Kampf läuft", ["Names appear after the next loading screen"]="Namen erscheinen nach dem nächsten Ladebildschirm",
        ["PREVIEW / DEMO – no confirmed live data"]="VORSCHAU / DEMO – keine bestätigten Live-Daten", ["Npcap is missing. Open the download page, install it yourself and restart the meter. Then teleport once in game."]="Npcap fehlt. Öffne die Downloadseite, installiere es und starte das Meter neu. Teleportiere danach einmal im Spiel.", ["Party confirmed · {0}"]="Gruppe bestätigt · {0}", ["Solo tracking · {0}"]="Solo-Messung · {0}",
        ["Manual schedule · Europe/Berlin"]="Manueller Zeitplan · Europa/Berlin", ["Time from in-game boss list"]="Zeit aus der Boss-Liste im Spiel", ["Observation/interval · estimate"]="Beobachtung/Intervall · Schätzung", ["Community schedule · Shugo.GG"]="Community-Zeitplan · Shugo.GG", ["Custom WAV: PCM or float, maximum 10 seconds and 10 MB."]="Eigene WAV: PCM oder Float, höchstens 10 Sekunden und 10 MB.",
        ["Toggle personal combat module"]="Persönliche Kampfwerte umschalten", ["Started · "]="Gestartet · ", ["Boss is active"]="Boss ist aktiv", ["Time not known yet"]="Zeit noch unbekannt", ["Spawn time reached · awaiting confirmation"]="Wiederkehrzeit erreicht · warte auf Bestätigung", ["Scheduled time reached"]="Geplante Startzeit erreicht",
        ["Copy full fight damage (numbers + %)"]="Gesamten Kampfschaden kopieren (Werte + %)", ["Copy game-chat lines (numbers + %)"]="Zeilen für den Spielchat kopieren (Werte + %)", ["Open to choose a line"]="Öffnen und Zeile auswählen",
        ["WINDOW"]="FENSTER", ["PLAYER DISPLAY"]="SPIELERANZEIGE", ["VISIBILITY"]="SICHTBARKEIT", ["LANGUAGE"]="SPRACHE", ["Language"]="Sprache",
        ["Window width / height"]="Fensterbreite / -höhe", ["Display opacity"]="Deckkraft", ["Background opacity"]="Hintergrund-Deckkraft",
        ["40% = translucent · 100% = opaque. Adjust size and transparency here."]="40 % = durchscheinend · 100 % = deckend. Größe und Transparenz hier anpassen.",
        ["Compact mode: narrow display with two lines per player"]="Kompaktmodus: zwei Zeilen pro Spieler", ["Received party CP/GS appears below player names. Long labels shorten when the meter is narrow."]="Empfangene CP/GS stehen unter den Spielernamen. Lange Angaben werden bei wenig Platz gekürzt.",
        ["Show gear score (GS)"]="Gear Score (GS) anzeigen", ["Show combat power (CP)"]="Kampfkraft (CP) anzeigen", ["PERSONAL COMBAT"]="PERSÖNLICHER KAMPF", ["Personal combat"]="Persönlicher Kampf",
        ["Show personal combat in the DPS meter"]="Persönliche Kampfwerte im Meter anzeigen", ["Show damage taken"]="Erlittenen Schaden anzeigen", ["Show healing done (self and others)"]="Heilung anzeigen (selbst und andere)",
        ["Integrated below the DPS list. Values follow the selected fight and include only received combat events."]="Unter der DPS-Liste integriert. Werte beziehen sich auf den gewählten Kampf und ausschließlich auf empfangene Kampfdaten.",
        ["Damage taken"]="Erlittener Schaden", ["Self healing"]="Selbstheilung", ["Healing others"]="Heilung anderer", ["Collapse personal combat"]="Persönliche Werte einklappen", ["Expand personal combat"]="Persönliche Werte ausklappen",
        ["Show / hide personal combat"]="Persönliche Werte anzeigen / ausblenden", ["Fight duration"]="Kampfdauer", ["Current time · Europe/Berlin · 24-hour clock"]="Uhrzeit · Europa/Berlin · 24-Stunden-Format",
        ["Estimated network RTT from captured game TCP acknowledgments. Includes server ACK delay; a local accelerator measures only its local connection. No probe packets are sent. A dash means no fresh sample."]="Geschätzte Netzwerk-RTT aus empfangenen TCP-Bestätigungen einschließlich Serververzögerung. Bei lokalen Beschleunigern nur die lokale Verbindung. Keine Messpakete werden gesendet. Ein Strich bedeutet: kein aktueller Messwert.",
        ["Statistics"]="Statistiken", ["Copy fight damage (numbers + %)"]="Kampfschaden kopieren (Werte + %)", ["Save last fight"]="Letzten Kampf speichern", ["Check for updates"]="Nach Updates suchen", ["Checking for updates…"]="Suche nach Updates…",
        ["Delete saved fights"]="Gespeicherte Kämpfe löschen", ["No saved fights."]="Keine gespeicherten Kämpfe.", ["No saved fights to delete."]="Keine gespeicherten Kämpfe zum Löschen.",
        ["Fights are saved only when you press Save last fight, unless automatic saving is enabled below. Only the last 10 saved fights are kept."]="Kämpfe werden mit dem Speicherknopf gesichert, sofern unten kein automatisches Speichern aktiviert ist. Nur die letzten zehn gespeicherten Kämpfe bleiben erhalten.",
        ["Updates are installed only after you press Update now."]="Updates werden erst nach einem Klick auf Jetzt aktualisieren installiert.", ["Boss only / everything you hit"]="Nur Boss / alle getroffenen Gegner", ["Download Npcap"]="Npcap herunterladen", ["Exit"]="Beenden", ["Lock position"]="Position sperren",
        ["Click here and press your shortcut. Applied after saving."]="Hier klicken und Tastenkürzel drücken. Wird nach dem Speichern angewendet.", ["Compact mode"]="Kompaktmodus",
        ["Event settings"]="Event-Einstellungen", ["Event sounds"]="Event-Sounds", ["Upcoming events"]="Anstehende Events", ["SCHEDULE"]="ZEITPLAN", ["EVENTS TO SHOW"]="ANGEZEIGTE EVENTS", ["APPEARANCE"]="DARSTELLUNG", ["SOUNDS & BOSS TIMERS"]="SOUNDS & BOSS-TIMER",
        ["All selected events within the next hour. Events stay visible for two minutes after starting, with a pulsing border."]="Alle gewählten Events innerhalb der nächsten Stunde. Events bleiben nach Beginn zwei Minuten mit pulsierendem Rahmen sichtbar.",
        ["Show all world bosses"]="Alle Weltbosse anzeigen", ["Includes scheduled bosses and known respawns received from the game."]="Enthält geplante Bosse und bekannte Wiederkehrzeiten aus dem Spiel.",
        ["Opacity"]="Deckkraft", ["Window width"]="Fensterbreite", ["Open boss timers"]="Boss-Timer öffnen", ["Open schedule source"]="Zeitplan-Quelle öffnen",
        ["Changes are saved automatically. Community schedule from Shugo.GG, checked Oct 12, 2026; availability is not officially confirmed."]="Änderungen werden automatisch gespeichert. Community-Zeitplan von Shugo.GG, geprüft am 12.10.2026; Verfügbarkeit nicht offiziell bestätigt.",
        ["Community schedule · Shugo.GG · checked Oct 12, 2026 · not officially confirmed"]="Community-Zeitplan · Shugo.GG · geprüft am 12.10.2026 · nicht offiziell bestätigt",
        ["No events in the next hour"]="Keine Events in der nächsten Stunde", ["No upcoming event"]="Kein anstehendes Event", ["Open separate window"]="Eigenes Fenster öffnen", ["Close card"]="Karte schließen",
        ["Started"]="Gestartet", ["Today"]="Heute", ["Tomorrow"]="Morgen", ["ago"]="vorher", ["in"]="in", ["EVENT"]="EVENT", ["BOSS"]="BOSS", ["PVP"]="PVP",
        ["Europe"]="Europa", ["NA East"]="NA Ost", ["NA West"]="NA West", ["Shugo Festival"]="Shugo-Festival", ["Dimensional Rift"]="Dimensionsriss", ["Abyss Invasion"]="Abyss-Invasion", ["Tracked world bosses"]="Beobachtete Weltbosse",
        ["Dimensional Invasion"]="Dimensionsinvasion", ["Spacetime Rift"]="Raumzeit-Riss", ["Artifact Siege"]="Artefakt-Belagerung", ["Siege Bosses"]="Belagerungsbosse", ["Guardian Lord Nahma"]="Wächterfürst Nahma", ["Watcher Kaira"]="Wächter Kaira",
        ["SOUND PREFERENCES"]="SOUND-EINSTELLUNGEN", ["Volume"]="Lautstärke", ["Automatic event reminder"]="Automatische Event-Erinnerung", ["At start"]="Bei Beginn", ["1 minute before"]="1 Minute vorher", ["2 minutes before"]="2 Minuten vorher", ["5 minutes before"]="5 Minuten vorher", ["10 minutes before"]="10 Minuten vorher",
        ["Chime"]="Klang", ["Bell"]="Glocke", ["Ping"]="Ping", ["Custom WAV"]="Eigene WAV", ["Choose WAV…"]="WAV wählen…", ["Preview"]="Anhören",
        ["Enable a sound for each event you want to hear. Automatic reminders follow your selected events.\nSimultaneous reminders play one after another."]="Aktiviere Sounds für gewünschte Events. Automatische Erinnerungen folgen deiner Event-Auswahl.\nGleichzeitige Erinnerungen werden nacheinander abgespielt.",
        ["Choose a local WAV file (up to 10 seconds and 10 MB)"]="Lokale WAV-Datei wählen (maximal 10 Sekunden und 10 MB)",
        ["Choose a valid PCM or float WAV file, up to 10 seconds and 10 MB."]="Wähle eine gültige PCM- oder Float-WAV-Datei mit höchstens 10 Sekunden und 10 MB.", ["Choose an existing WAV file first."]="Wähle zuerst eine vorhandene WAV-Datei.",
        ["Tracked world bosses use the watched-boss and ‘Notify before respawn’ settings in Boss timers. Custom WAV files must stay at their selected location."]="Beobachtete Weltbosse verwenden die Auswahl und Vorwarnzeit aus den Boss-Timern. Eigene WAV-Dateien müssen am gewählten Speicherort bleiben.",
        ["Choose WAV audio (*.wav)"]="WAV-Audio auswählen (*.wav)", ["Close"]="Schließen", ["Settings"]="Einstellungen", ["Boss timers"]="Boss-Timer",
        ["Waiting for fresh game data…"]="Warte auf aktuelle Spieldaten…",
        ["Character not confirmed yet. Teleport once or change zones through a loading screen. Waiting for fresh game data…"]="Charakter noch nicht bestätigt. Teleportiere einmal oder wechsle das Gebiet über einen Ladebildschirm. Warte auf aktuelle Spieldaten…",
        ["Character recognized · {0}. Waiting for fresh actor data from the game…"]="Charakter erkannt · {0}. Warte auf aktuelle Charakterdaten aus dem Spiel…",
        ["Delete all {0} saved fights? This cannot be undone."]="Alle {0} gespeicherten Kämpfe löschen? Dies kann nicht rückgängig gemacht werden.", ["Could not delete saved fights: {0}"]="Gespeicherte Kämpfe konnten nicht gelöscht werden: {0}",
        ["Room stats"]="Raumwerte", ["Party stats"]="Gruppenwerte", ["Confirmed party"]="Bestätigte Gruppe", ["Group finder"]="Gruppensuche", ["Damage starts with the next hit."]="Schadensmessung beginnt mit dem nächsten Treffer.",
        ["Last fight saved."]="Letzter Kampf gespeichert.", ["Fight already saved."]="Kampf bereits gespeichert.", ["Could not save fight: {0}"]="Kampf konnte nicht gespeichert werden: {0}",
        ["No completed fight to copy yet."]="Noch kein abgeschlossener Kampf zum Kopieren.", ["Copy fight results"]="Kampfergebnisse kopieren", ["Copy damage ranking (numbers + %)"]="Schadensrangliste kopieren (Werte + %)", ["Copy damage table for Discord"]="Schadenstabelle für Discord kopieren"
    };
}

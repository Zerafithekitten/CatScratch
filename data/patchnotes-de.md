## CatScratch v4.0.5

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.5/data/patchnotes-en.md)

<!-- contents -->
### Auf einen Blick
- [Ein kleines Geheimnis](#little-secret)
<!-- /contents -->

<a id="little-secret"></a>
### ✨ Ein kleines Geheimnis

In diesem Update habe ich eine kleine Überraschung versteckt. Die bleibt vorerst unter meiner Pfote — mehr verrate ich, wenn es Zeit für ihren Auftritt ist. 🐾

Heute also nur ein leises Update und ein Dankeschön, dass ihr dabei seid. Ich hoffe, die Überraschung bringt euch später zum Schmunzeln. 💜

## CatScratch v4.0.4

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.4/data/patchnotes-en.md)

Ein kleiner Fix für einen Schlüssel, der im Spiel ankam, aber in CatScratch nicht mitgezählt wurde. Danke für den Mitschnitt — damit konnte ich genau sehen, welches Update die Katze übersehen hatte. 🐾

<!-- contents -->
### Auf einen Blick
- [Shugo-Schlüssel nach Gegenstandsverwendung live aktualisiert](#shugo-keys)
- [Was die Ungefähr-Markierung bedeutet](#approximate-values)
<!-- /contents -->

<a id="shugo-keys"></a>
### 🔑 Shugo-Schlüssel nach Gegenstandsverwendung live aktualisiert

Wenn ihr mit einem Gegenstand einen Shugo Reward Key hinzufügt, aktualisiert sich das Dungeon-Eintritte-Modul jetzt live. Die Verbrauchsmeldung kannte CatScratch bereits; beim Hinzufügen verwendet das Spiel aber eine andere Paketform. Diese wurde bisher ignoriert.

Im gelieferten Mitschnitt meldet der Server **18 reguläre und 4 zusätzliche Schlüssel**. CatScratch übernimmt diese absoluten Bestände jetzt, statt auf **18 (+3)/21** stehenzubleiben. Das bekannte Maximum bleibt erhalten. Ein erneut eintreffendes identisches Paket fügt keinen weiteren Schlüssel hinzu.

<a id="approximate-values"></a>
### ≈ Gespeicherte Werte können weiterhin live aktualisiert werden

Die Ungefähr-Markierung bedeutet nicht, dass ein Wert gesperrt ist. Sie kennzeichnet einen früher gespeicherten Stand, der auf eine frische Bestätigung wartet. Ein erkanntes Live-Update ersetzt diesen Stand und entfernt die Markierung für den betroffenen Eintrag. Andere noch unbestätigte Einträge behalten ihre Markierung.

Der Fix fragt das Spiel nicht aktiv ab und erfindet keine fehlenden Werte. CatScratch muss weiterhin ein unterstütztes Server-Update empfangen. Danke, dass ihr mir geholfen habt, dieses fehlende Update zu finden. 💜

## CatScratch v4.0.3

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.3/data/patchnotes-en.md)

Eine schnelle Korrektur von mir: Der Patreon-Button aus dem letzten Update führte zur falschen Creator-Adresse. Da ist die Katze einmal falsch abgebogen. 🐾

<!-- contents -->
### Auf einen Blick
- [Die richtige Patreon-Seite](#patreon-link)
<!-- /contents -->

<a id="patreon-link"></a>
### 💜 Die richtige Patreon-Seite

**Einstellungen → Patreon → Patreon öffnen** führt jetzt zu [Zerafithekitten auf Patreon](https://www.patreon.com/cw/Zerafithekitten). README, Paket-Dokumentation und die mitgelieferten englischen/deutschen Patchnotes verwenden ebenfalls die korrigierte Adresse.

Dieser Hotfix korrigiert nur den Link. Paketimport, Designs, Sounds und Pfote funktionieren wie in 4.0.2; eure Einstellungen und installierten Pakete bleiben erhalten. Danke für den schnellen Hinweis!

## CatScratch v4.0.2

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.2/data/patchnotes-en.md)

### 🐾 Ein bisschen mehr Katze

Ich habe ein kleines Dankeschön für alle vorbereitet, die CatScratch unterstützen möchten: ein paar Designs, ein paar Sounds und eine goldene Pfote. Die nützlichen Funktionen bleiben für alle verfügbar. Die Extras sind einfach ein bisschen mehr Katze für diejenigen, die Lust darauf haben.

Außerdem bekommt die Windows-Audioanbindung etwas mehr Geduld, nachdem eine eigentlich gültige MP3 abgelehnt wurde.

<!-- contents -->
### Auf einen Blick
- [Ein eigener Platz für Patreon](#patreon)
- [First Paws: Designs, Sounds und Supporter-Pfote](#first-paws)
- [So funktionieren die Pakete](#packages)
- [Eigene MP3- und WAV-Sounds](#custom-audio)
<!-- /contents -->

<a id="patreon"></a>
### 💜 Ein eigener Platz für Patreon

In den Einstellungen gibt es jetzt den Reiter **Patreon**, zwischen Updates und Feedback. Dort findet ihr den Link zu [meiner Patreon-Seite](https://www.patreon.com/cw/Zerafithekitten) und könnt heruntergeladene CatScratch-Supporter-Pakete importieren.

Entwicklungs-Einblicke und gelegentliche Abstimmungen finden auf Patreon statt. Ein zusätzliches Konto innerhalb von CatScratch braucht ihr dafür nicht.

![Patreon-Einstellungen mit dem optionalen First-Paws-Paket](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.0.2/docs/screenshots/patreon.png)

<a id="first-paws"></a>
### ✨ First Paws

Im ersten separat verteilten Supporter-Paket stecken:

- **Drei Designs:** Midnight Cat, Moonlight und Golden Paw.
- **Drei eigene Sounds:** Paw Chime, Moon Ping und Soft Purr.
- **Eine optionale goldene Pfote** im bisherigen Bedienfeld. Gleiche Größe, gleicher Platz und jederzeit abschaltbar.

Im Patreon-Reiter könnt ihr ein Design auswählen oder einen Sound zuordnen und anschließend **Speichern**. Die Sounds lassen sich für Hidden Cubes, Pet Tracker und Ressourcen-Warnungen verwenden. Nach erneutem Öffnen der Einstellungen erscheinen sie auch in der Event-Soundauswahl. Ob ein Ton aktiviert ist und wie laut er eingestellt ist, bleibt erhalten.

<a id="packages"></a>
### 📦 Herunterladen, importieren, behalten

Das Mitglieder-Paket wird separat heruntergeladen. Entpackt die äußere ZIP und wählt unter Einstellungen → Patreon → Paket importieren die **.cspack**-Datei aus. Die .cspack selbst bleibt gepackt.

- Der Import installiert die Inhalte sofort. Farben, Sound-Zuordnungen und Pfote werden mit Speichern übernommen.
- Die Pakete enthalten signierte Designs und Audiodateien, keine ausführbaren Plugins. CatScratch prüft die Inhalte vor dem Import.
- Nach dem Import funktioniert alles offline. Kein Patreon-Login, keine Abo-Prüfung und keine automatischen Downloads.
- Erhaltene Pakete bleiben nach dem Ende einer Mitgliedschaft nutzbar. Die Signatur bestätigt den Herausgeber, sie ist keine Mitgliedschaftssperre.
- Bisherige öffentliche Funktionen, eigene Farben, eigene Sounds und Fehlerkorrekturen bleiben für alle verfügbar. Ein Import allein überschreibt eure Einstellungen nicht.

<a id="custom-audio"></a>
### 🔊 Etwas mehr Geduld mit eigenen Sounds

Windows kann eine Audiodatei als geöffnet melden, bevor ihre Dauer verfügbar ist. CatScratch wartet jetzt kurz auf diese Angabe und versucht die Prüfung bei einem Fehler einmal mit einem frischen Decoder erneut. Auch die Wiedergabe bekommt nach einem Decoderfehler einen zweiten Versuch.

Die gemeldete **ALARM ALARM.mp3** ist gültig: Sie hat hier wiederholte Prüfungen mit etwa **4,10 Sekunden** bestanden und wurde von Windows bis zum Ende abgespielt. Die ursprüngliche zeitweilige Ablehnung konnte ich nicht nachstellen; ihre genaue Ursache ist deshalb nicht bestätigt. Ungültige Dateien und Sounds über zehn Sekunden werden weiterhin abgelehnt. Eine fehlgeschlagene Auswahl behält euren bisherigen Sound bei.

Danke, dass ihr dieses Ein-Personen-Katzenprojekt unterstützt – ob auf Patreon, mit einer hilfreichen Fehlermeldung oder beim gemeinsamen Testen einer störrischen Kleinigkeit. 💜

## CatScratch v4.0.1

Ein kleiner Hotfix vor dem nächsten Abenteuer. Eine Messung zurückzusetzen sollte einen frischen Kampf ermöglichen – und CatScratch nicht vergessen lassen, wer überhaupt in der Gruppe ist. Genau das konnte bisher passieren.

<!-- contents -->
### Auf einen Blick
- [Schadensanzeige nach dem Zurücksetzen](#meter-reset)
- [Was behoben ist und was noch untersucht wird](#untersuchung)
<!-- /contents -->

<a id="meter-reset"></a>
### 🛠️ Schadensanzeige nach dem Zurücksetzen

- Ein manueller Messungs-Neustart behält jetzt die bereits bestätigte Charakter- und Gruppenzuordnung der laufenden Spielsitzung. Neue Schadensdaten können wieder gezählt werden, ohne auf erneut gesendete Namen und Gruppenlisten zu warten.
- Der Kampf wird weiterhin zurückgesetzt und die Erfassung neu verbunden. Vorhandene Ressourcenstände werden bis zur Bestätigung als ungefähr markiert; ihre laufende Speicherung bleibt verfügbar.
- Echte Logins, Charakterwechsel und das Beenden des Spiels verwerfen weiterhin die alte Zuordnung. Auch beim Schnellwechsel auf derselben Karte hebt eine neue Login-Tabelle die bisherige Gruppenbestätigung auf.
- Wird die Erfassung durch geänderte Erfassungseinstellungen neu gestartet, bleibt eine frische Bestätigung erforderlich.

<a id="untersuchung"></a>
### 🔎 Was noch untersucht wird

Behoben ist der bestätigte Fehler nach dem Zurücksetzen. Die separate Meldung einer leeren Bossanzeige schon davor bleibt offen: Der gelieferte Mitschnitt beginnt erst nach dem Reset und kann die frühere Ursache deshalb nicht belegen. Falls es erneut passiert, bitte sofort vor dem Zurücksetzen den aktuellen Mitschnitt speichern. Er enthält die letzte Minute.

Danke fürs Melden und fürs Helfen, die beiden Probleme auseinanderzuhalten. Eine Stelle weniger, an der die Katze den Faden verliert. 🐾

## CatScratch v4.0.0

### 🐾 Mehr Überblick, weniger Hin und Her

Dieses Update ist etwas größer geworden. Aus „Ich möchte meine Ressourcen sehen“ sind zwei neue Module, eine Charakterübersicht und einige praktische Helfer geworden.

Dazu habe ich an den Stellen nachgebessert, an denen CatScratch beim Spielen noch die Krallen ausgefahren hat: wandernde Fenster, verschwundene Werte und der Schnellwechsel zwischen Charakteren.

Danke für eure Meldungen, Mitschnitte und die Geduld mit den vielen Betas. Hinter CatScratch steckt eine Person mit einer Katze als Fursona. Eure Rückmeldungen haben mir geholfen, Fehler zu finden, die ich allein nicht erwischt hätte. 💜

<!-- contents -->
### Auf einen Blick

- [Ressourcen: Energie und Währungen](#ressourcen)
- [Dungeon-Eintritte: Was ist noch übrig?](#dungeons)
- [Charakterübersicht: Ein Blick auf die anderen Charaktere](#charaktere)
- [Eigene Warnschwellen und Sounds](#warnungen)
- [Wie aktuell sind die Zahlen?](#daten)
- [Weltenbosse nach eurem Geschmack](#bosse)
- [Fenster platzieren und ausrichten](#fenster)
- [Durchklickmodus und Vordergrundverhalten](#durchklick)
- [Hidden Cubes mit einem kleinen Miau](#cubes)
- [Kleine Fehler und ein besserer Empfang nach Updates](#feinschliff)
<!-- /contents -->

<a id="ressourcen"></a>
### 💰 Ressourcen: Energie und Währungen

Das neue Ressourcen-Modul bringt diese Bestände an einem Ort zusammen:

- Odyle Energy
- Abyss Points
- Centuryroot Token
- Deava Pass Shop Coin
- Phantasmal Fragment
- Oath Coins

Unter Einstellungen → Ressourcen wählt ihr jeden Wert einzeln aus. Dazu gibt es eine Liste, eine schmale horizontale Leiste und ein Raster mit zwei Spalten – jeweils mit den passenden Spielsymbolen.

Odyle zeigt reguläre und zusätzliche Energie getrennt. Bestätigte Änderungen durch Regeneration, Gegenstände sowie erhaltene oder ausgegebene Währungen werden übernommen, sobald die entsprechenden Spieldaten eintreffen. Für jede Änderung neu einzuloggen ist damit nicht nötig.

Auswahl, Layout, Position und Pin bleiben nach einem Neustart erhalten. Einen geratenen Auflade-Countdown gibt es nicht.

<a id="dungeons"></a>
### 🎟️ Dungeon-Eintritte: Was ist noch übrig?

Daily Dungeon, Nightmare und Shugo Festival bekommen ein eigenes, optionales Modul unter Einstellungen → Dungeon-Eintritte. Ihr entscheidet selbst, welche sichtbar sein sollen und ob sie als Liste, horizontale Leiste oder Raster erscheinen.

Die Anzeige enthält den regulären Bestand, zusätzliche Eintritte in Klammern und die vom Spiel gemeldete Obergrenze. Position und Pin werden separat gespeichert; Layout-Profile und Durchklickmodus werden unterstützt.

Der Verbrauch wird aktualisiert, wenn das Spiel ihn bestätigt. Je nach Dungeon passiert das beim Abschluss oder beim Abholen der Belohnung. Ein bloßer Gebietswechsel zieht deshalb keinen Eintritt ab. Fehlende Meldungen werden nicht durch geratene Abzüge ersetzt.

<a id="charaktere"></a>
### 👤 Charakterübersicht: Ein Blick auf die anderen Charaktere

„Wie viel hatte ich eigentlich noch auf dem anderen Charakter?“ Dafür gibt es die neue Charakterübersicht. Das Charakterkarten-Symbol bei den Verlauf- und Übersichtsfunktionen im Bedienfeld öffnet die gespeicherten Ressourcen und Dungeon-Eintritte, nach Charakter und Server getrennt. Ihr findet sie auch in den Ressourcen-Einstellungen.

Das Fenster passt jetzt zu Last played with. Klare Trennlinien zwischen Spalten und Zeilen machen daraus eine richtige Tabelle, die Werte sind mittig ausgerichtet. Der Tooltip zeigt die letzte Bestätigung jedes Werts. Im Stream-Modus werden Namen verborgen.

Auch der Schnellwechsel ist korrigiert: Neue Bestände können vor dem neuen Charakternamen eintreffen. CatScratch hält diese Daten nun zurück, bis klar ist, wem sie gehören. Ähnliche Namen wie Zerafi und Zerafí bleiben getrennt.

Falls eine frühere Beta Bestände vermischt gespeichert hat, werden sie nach diesem Update durch erneutes Einloggen der betroffenen Charaktere aufgefrischt. Ausgeloggte Charaktere zeigen ihre gespeicherten Stände; sie werden nicht im Hintergrund abgefragt.

<a id="warnungen"></a>
### 🔔 Eigene Warnschwellen und Sounds

Für jede Ressource und jeden unterstützten Dungeon könnt ihr eine eigene Warnschwelle aktivieren. Auf Wunsch kommt ein mitgelieferter Ton mit einstellbarer Lautstärke dazu.

- Bei Odyle zählt die regulär verfügbare Energie.
- Bei Dungeons zählen reguläre und zusätzliche Eintritte zusammen.
- Nur frisch bestätigte Werte lösen eine Warnung aus.
- Ein niedriger Bestand warnt einmal. Erst nach einer Erholung über die Schwelle und einem erneuten Absinken wird wieder gewarnt.

Alles ist freiwillig. Ohne aktivierte Warnung bleibt die Katze ruhig.

<a id="daten"></a>
### 🕒 Wie aktuell sind die Zahlen?

Ein normaler Wert wurde aus empfangenen Spieldaten bestätigt. Ein ≈ markiert den zuletzt bekannten Stand, der noch auf eine frische Bestätigung wartet. Relog! bedeutet, dass für diesen Wert noch kein verwendbarer Ausgangsstand vorliegt.

Im Tooltip seht ihr jetzt für jeden Wert einzeln den Zeitpunkt und das Alter der letzten Bestätigung. Bei älteren gespeicherten Daten ohne Zeitangabe steht ausdrücklich, dass der Zeitpunkt unbekannt ist.

Bei einem Neustart der Erfassung bleiben bekannte Werte als ungefährer Stand erhalten. Der Testmodus löscht keine echten Ressourcen mehr. Charakter und Server halten die Datensätze auseinander; frische Meldungen ersetzen die zugehörigen gespeicherten Werte.

Eine kleine Statusanzeige im Bedienfeld unterscheidet im Tooltip zwischen eintreffenden Spieldaten, dem Warten auf Charakterdaten und einer gestörten Erfassung.

<a id="bosse"></a>
### 👑 Weltenbosse nach eurem Geschmack

Unter Einstellungen → Events findet ihr eine durchsuchbare Liste bekannter Weltenbosse. Wählt einzelne Bosse aus, markiert Favoriten und vergebt eigene Benachrichtigungstöne – mitgeliefert oder als eigene WAV-/MP3-Datei, mit Vorlaufzeit und Lautstärke.

Ausgewählte Bosse und Favoriten bleiben auch außerhalb des normalen Ein-Stunden-Fensters sichtbar. Favoriten tragen den goldenen Rahmen; bestätigt lebende Bosse stehen bei den gestarteten Events. Unbekannte Spawnzeiten werden als unbekannt angezeigt.

Beobachten bleibt eure Entscheidung: Erkannte oder manuell eingetragene Kills speichern weiterhin Killzeit und Respawn-Timer, schalten aber nicht mehr automatisch die Beobachtungsglocke ein. Vorhandene Auswahlen bleiben erhalten.

<a id="fenster"></a>
### 📐 Fenster platzieren und ausrichten

Upcoming und Started Events erkennen die obere und untere Bildschirmkante, wenn ihr sie dorthin zieht. Unten wachsen neue Einträge nach oben; verschwinden sie, schrumpft das Modul wieder nach unten zurück. Oben bleibt die obere Kante stehen. Zieht ihr das Fenster weg, löst sich die Verankerung. Gespeicherte Positionen und Profile merken sich eure Wahl.

Optionales Einrasten unter Einstellungen → Darstellung hilft beim Ausrichten an Bildschirmkanten und anderen sichtbaren Modulen. Den Abstand wählt ihr selbst. Mit gedrückter Alt-Taste lässt sich das Einrasten beim Verschieben umgehen. Gepinnte Module bleiben fest.

Profile speichern die Einrast-Einstellungen, ohne eure Ressourcen-Auswahl oder Warnschwellen zu überschreiben.

<a id="durchklick"></a>
### 🔒 Durchklickmodus und Vordergrundverhalten

Die Startoption heißt jetzt klarer Mit aktiviertem Durchklickmodus starten. Einschalten und Speichern sorgt dafür, dass CatScratch gesperrt startet – auch wenn ihr die Fenster vor dem Beenden vorübergehend entsperrt hattet.

Das Öffnen von Upcoming oder Started Events entsperrt nicht mehr unbemerkt alle Module. Event-Karten, Pet Tracker und Hidden-Cube-Anzeige behalten ihre Position, wenn Bedienelemente verschwinden, auch nach mehrfachem Sperren und Entsperren. Das Bedienfeld bleibt zum Entsperren anklickbar.

Die Wiederherstellung des Vordergrundstatus wurde nach Fenster- und Anzeigewechseln verstärkt. Eine unabhängige regelmäßige Prüfung erhält Position, Transparenz und den Tastaturfokus des Spiels.

<a id="cubes"></a>
### 🐱 Hidden Cubes mit einem kleinen Miau

Die zusätzlich bestätigte Cube-Variante auf der feindlichen Seite in Verteron wird erkannt. Der Hintergrund der Fundmeldung ist kompakter und sitzt enger am Text; das Sperren verschiebt die Anzeige nicht mehr.

Neue Installationen verwenden den ausgewählten Katzenlaut als Standardton. Eure bereits gespeicherten Klänge, Lautstärke und die Entscheidung für oder gegen einen Ton bleiben erhalten. Ein kleines Miau für neue Nutzer musste aber sein.

<a id="feinschliff"></a>
### 🛠️ Kleine Fehler und ein besserer Empfang nach Updates

- Unvollständige Charakterdaten und teilweise Inventarmeldungen löschen keine anderen bekannten Bestände mehr.
- Odyle-Werte ab 128 und die zugehörigen Dungeon-Eintritte werden korrekt gelesen.
- Bestätigter Verbrauch wird als absoluter Bestand übernommen, damit nichts doppelt abgezogen wird.
- Unveränderte Ressourcen- und Dungeon-Zeilen werden nicht mehr unnötig neu aufgebaut.
- Symbole, Zahlen und Relog!-Hinweise sitzen sauberer; große Zahlen passen besser in schmale Kacheln.
- Die alten Schnappschuss-Sternchen entfallen bei den unterstützten, live aktualisierten Währungen. Noch unbestätigte gespeicherte Werte tragen ≈.
- Die Hilfe erklärt das aktuelle Verhalten: Speichern lässt die Einstellungen offen; X verwirft danach noch nicht gespeicherte Änderungen.
- Patchnotes erscheinen künftig in diesem persönlicheren Stil. Bei deutscher App-Sprache gibt es Deutsch, bei allen anderen Englisch. Für ältere Versionen ohne Übersetzung bleibt Englisch verfügbar.
- Nach einem Stable-Update öffnen sich dessen Patchnotes beim nächsten Start einmal automatisch. Normale Neustarts und Beta-Updates holen sie nicht ständig zurück. Über das Bedienfeld könnt ihr sie jederzeit wieder öffnen.

Alle Änderungen der 3.0.12-Beta-Reihe sind enthalten, zusammen mit den abschließenden Schnellwechsel- und Tabellenkorrekturen. CatScratch light bleibt seine eigenständige Edition.

Danke fürs Testen, fürs genaue Hinsehen und für die Geduld. Viel Spaß mit 4.0.0 – und möge der nächste Cube laut genug miauen. 🐾


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


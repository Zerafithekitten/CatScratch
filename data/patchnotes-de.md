## CatScratch v4.1.4-beta.4

Ein genauerer Blick auf „in der Nähe“ – und mehr Platz zum Anpassen. 🐾

- „Bestätigt in der Nähe“ verlangt jetzt einen frisch vermessenen aktuellen Gegner innerhalb von 100 Metern einschließlich Unsicherheitsbudget. Eine empfangene Gegnermeldung, bekannte Farmstelle oder alte Position genügt dafür nicht. Entfernte empfangene Quellen verdrängen nähere vermessene Ziele nicht mehr allein wegen ihrer Meldung.
- Behoben: Die automatische Höhenanpassung setzte Breitenänderungen des Pet Trackers zurück. Ziehgriff, Einstellungen und Layout-Vorschau verwenden jetzt die korrigierte Größenänderung.
- Neben dem Schieberegler gibt es ein direktes Breitenfeld für 150–1600 px sowie einen größeren Ziehgriff. Save übernimmt die Breite; das Schließen ungespeicherter Einstellungen stellt sie zurück.

Die Position bleibt von Paketmeldungen abhängig. Diese Korrektur behebt die Nähe-Einstufung; eine durchgängige Minikartenortung ist noch nicht enthalten.

## CatScratch v4.1.4-beta.3

Ein schnellerer Blick – mit vertrauten Einheiten. 🐾

- Pet-Entfernungen erscheinen jetzt in Metern, auch bei Farmstellen und beim letzten bestätigten Positionsstand. Die Tooltips verwenden ebenfalls Meter.
- Der sichtbare Pet Tracker aktualisiert sich alle 200 ms statt 500 ms. Ausgeblendet oder bei geöffneter Spielkarte pausiert die Aktualisierung.
- Die schnellere Anzeige erhöht nicht die Häufigkeit empfangener Positionsmeldungen. Historische Entfernungen behalten ~ und das angezeigte Positionsalter.

## CatScratch v4.1.4-beta.2

Die Pet-Suche braucht ein Ziel – auch wenn der passende Gegner gerade nicht empfangen wird. 🐾

- Altgard enthält jetzt bekannte Farmstellen für 188 vorhandene Pet-Quell-NPC-Typen. Der Pet Tracker zeigt ungefähre horizontale Entfernungen dorthin auch ohne aktuelle Gegnersichtung. ◇ kennzeichnet eine Farmstelle, keinen bestätigten Spawn.
- Frische Positionen zeigen ≈ Entfernungen. Zwischen Positionsmeldungen verwenden ~ Entfernungen ausdrücklich die letzte bestätigte Position und zeigen deren Alter. Dieser letzte Stand verfällt nach 60 Sekunden und wird bei Charakter-, Karten- oder Aufnahmewechsel verworfen.
- Fehlende eigene Positionen und nicht unterstützte Quellkoordinaten werden erklärt. Frische Daten haben Vorrang vor historischen Angaben.

Wenn seit Aufnahmestart noch keine eigene Position vorliegt, einmal teleportieren. Farmstellen-Entfernungen unterstützen vorerst nur Altgard. Spieleinheiten sind keine Meter oder Wegstrecken. Live-Minikartenortung und Richtungspfeile zur Figurenausrichtung sind weiterhin nicht enthalten.

## CatScratch v4.1.4-beta.1

Etwas mehr Orientierung für deine Pet-Suche. 🐾

- Der Pet Tracker nutzt zusätzliche passiv empfangene Positionsmeldungen der eigenen Figur.
- Frische eigene und Quell-NPC-Koordinaten ermöglichen ungefähre Entfernungen in Spieleinheiten; nahe unvollständige Pets werden bevorzugt.
- Veraltete oder fehlende Koordinaten blenden Entfernungen aus. Regionale Empfehlungen bleiben verfügbar. Charakter-, Karten- und Aufnahmewechsel verwerfen Ortungsdaten.

Positionsmeldungen können zeitweise fehlen. Live-Minikartenortung und Richtungspfeile zur Figurenausrichtung sind in dieser Beta noch nicht enthalten.

## CatScratch v4.1.3

Ein kleiner Geist hat sich auf dem Bildschirm festgekrallt. Mit diesem Hotfix verabschiedet er sich jetzt richtig. 🐾

<!-- contents -->
### Das ist drin
- [Keine zurückbleibende Katzensilhouette](#transparenter-boss-timer)
<!-- /contents -->

<a id="transparenter-boss-timer"></a>
### 🛠️ Keine zurückbleibende Katzensilhouette

Der leere, abgetrennte Boss-Timer konnte im Click-through-Modus weiterhin sein dezentes Katzenwasserzeichen zeichnen, obwohl Rahmen und Wartetext bereits transparent waren. Das konnte wie ein Rest einer Hidden-Cube-Meldung aussehen.

Das Wasserzeichen verschwindet jetzt zusammen mit dem inaktiven Timer. Sobald wieder Bosszeiten vorliegen, erscheinen Inhalt und Signatur wie gewohnt. Die Prüfung erfasst nun das vollständige Fenster einschließlich Dekoration und kontrolliert, dass im Leerlauf keine sichtbaren Pixel zurückbleiben.

Danke fürs Entdecken des kleinen Schattens. 💜

## CatScratch v4.1.2

Ein kleiner Checklisten-Hotfix von mir. Der neue Planer soll direkt da sein – ohne erst nach einem Umschalter suchen zu müssen. 🐾

<!-- contents -->
### Das ist drin
- [Nur noch die neue Checkliste](#checkliste-direkt)
- [Aufgaben löschen und zurückholen](#checkliste-loeschen)
<!-- /contents -->

<a id="checkliste-direkt"></a>
### 📋 Nur noch die neue Checkliste

Die alte Freitext-Ansicht und der Plan-Umschalter sind entfernt. Beim Öffnen erscheint immer der neue Charakter-/Server-Planer, auch wenn zuvor die alte Ansicht gewählt war. Die alten Zeilenanzahl-Optionen und Hinweise wurden aus den Einstellungen entfernt.

Bereits gespeicherter Planer-Fortschritt bleibt erhalten. Alte Freitext-Aufgaben bleiben ausschließlich als Sicherungsdaten erhalten; sie werden nicht ungefragt einem Charakter zugewiesen. Zusätzlich wird, sofern alte Einträge vorhanden sind, einmalig eine lokale Datei `userdata/checklist-legacy-backup.json` angelegt.

<a id="checkliste-loeschen"></a>
### 🧹 Aufgaben löschen und zurückholen

Unter der Liste öffnet **Bearbeiten** den Bearbeiten-Modus. Dann erscheint neben jeder Aufgabe ein kleiner **×-Button** zum Entfernen. **Fertig** blendet die Löschknöpfe wieder aus. Löschen per Rechtsklick bleibt ebenfalls möglich.

Aufgabendefinitionen gelten für den gesamten lokalen Planer: Entfernen betrifft deshalb die Aufgabe bei allen Charakteren. Mit **Entfernen rückgängig** könnt ihr die zuletzt entfernte Aufgabe wiederherstellen – samt vorhandenen Charakter- und Serverständen. Rückgängig steht im aktuell geöffneten Checklistenfenster zur Verfügung.

Gelöschte Aufgaben bleiben nach einem Neustart gelöscht. Auch eine bewusst leere Liste wird nicht erneut mit der Startvorlage befüllt. Main-/Alt-Ziele, Server-Zähler, Odyle in 40er-Schritten und Durchklickbedienung bleiben erhalten.

![Checkliste im Bearbeiten-Modus](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.1.2/docs/images/checklist-edit-de-v4.1.2.png)

Danke für den schnellen Hinweis – jetzt ist die neue Liste wirklich die einzige. 💜

## CatScratch v4.1.1

Ein bisschen weniger im Kopf mitzählen – für euren Main und die Alts. 🐾 Die Checkliste bekommt einen kompakten manuellen Planer. Eure bisherige Aufgabenliste bleibt dabei erhalten.

<!-- contents -->
### Das ist drin
- [Eigener Fortschritt pro Charakter](#planer-charaktere)
- [Täglich, wöchentlich und alles dazwischen](#planer-aufgaben)
- [Odyle in 40er-Schritten](#planer-odyle)
- [Eure bisherige Liste bleibt erhalten](#planer-bisherige-liste)
<!-- /contents -->

<a id="planer-charaktere"></a>
### 🐱 Eigener Fortschritt pro Charakter

Über **Plan** im Kopf der Checkliste wechselt ihr zum neuen Planer. Er folgt dem bestätigten aktiven Charakter. Mit dem Charakter-Button könnt ihr gespeicherte Charaktere durchgehen; der Pfeil schaltet das automatische Folgen wieder ein.

Persönliche Aufgaben haben getrennte Stände. Gemeinsame Aufgaben teilen sich einen Zähler auf demselben Server; verschiedene Server bleiben getrennt. Die Kennzeichnung Main oder Alt erlaubt unterschiedliche Ziele. Ist der aktive Charakter nicht bestätigt, warten die Fortschritts-Buttons, statt den Klick dem vorherigen Charakter zuzuordnen.

<a id="planer-aufgaben"></a>
### 📋 Täglich, wöchentlich und alles dazwischen

Die bearbeitbare Startvorlage enthält **16 Aufgaben** in den Bereichen Täglich, Wöchentlich, Abyss und Optional. Tragt Fortschritt über Häkchen oder **+ / −** ein. Der Planer wird manuell geführt; er erkennt Aktivitäten nicht automatisch und zeigt keinen Live-Ressourcenbestand.

Per Rechtsklick könnt ihr Bereich, Charakter-/Server-Zuordnung, Main-/Alt-Ziele und tägliches, wöchentliches oder kein Zurücksetzen wählen. Namen lassen sich bei ausgeschaltetem Durchklickmodus direkt ändern; Aufgaben könnt ihr hinzufügen und entfernen. Die Startwerte sind Beispiele für eure eigene Routine und euren Server.

Automatische Resets bleiben optional und verwenden die vorhandenen Checklisten-Einstellungen mit einstellbarer UTC-Uhrzeit und Wochentag. Aufgaben ohne Reset behalten ihren Fortschritt.

![Checklisten-Planer](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.1.1/docs/images/checklist-planner-de-v4.1.1.png)

<a id="planer-odyle"></a>
### ⚡ Odyle in 40er-Schritten

Odyle zählt mit **+40 / −40**: **0 → 40 → 80 → 120**. Die anderen Startaufgaben bleiben bei Einzelschritten. Häkchen und Fortschritts-Buttons funktionieren auch im globalen Durchklickmodus, ohne dem Spiel den Tastaturfokus zu nehmen.

Die schmalere Gestaltung bringt Zahlen und Aufgabentext näher zusammen. Charakternamen berücksichtigen den Stream-Modus.

<a id="planer-bisherige-liste"></a>
### 💜 Eure bisherige Liste bleibt erhalten

Mit Plan kommt ihr auch zurück zur bisherigen Freitext-Checkliste. Ihre Aufgaben und Häkchen bleiben separat erhalten. Das Update überschreibt sie nicht und weist sie keinem Charakter zu.

Planer-Aufgaben und Fortschritt werden unabhängig von Layout-Profilen und Einstellungsvorschauen gespeichert und überstehen einen Neustart. Auch die bisherige Liste behält ausgeblendete Aufgaben, wenn ihr ihre sichtbare Zeilenanzahl reduziert.

Danke fürs Mitgestalten – ein bisschen weniger Luft zwischen den Zahlen, ein bisschen weniger Chaos im Katzenkopf. 💜

## CatScratch v4.1.0

Ein paar neue Tricks unter der Pfote. 🐾

In diesem Update steckt die Arbeit aus den letzten Betas und die neue Klassenhilfe. Neben den größeren Neuerungen habe ich mich auch um die Dinge gekümmert, die man im Alltag ständig benutzt: Einstellungen ausprobieren, die DPS-Anzeige leeren, Schlüssel im Blick behalten und CatScratch etwas unnötige Arbeit abnehmen.

Danke für eure Aufzeichnungen, Screenshots und die Geduld beim Testen. Hinter CatScratch sitzt weiterhin nur eine Katze – und einiges hier ist durch eure Rückmeldungen entstanden. 💜

### Das ist drin

- [Eine Orientierung für jede Klasse](#klassenhilfe)
- [Genius Pet und Seelenbindungen](#pet-und-seelenbindung)
- [Einstellungen direkt ausprobieren](#live-vorschau)
- [Das Altgard-Kartenexperiment](#altgard-karte)
- [Hidden Cubes und eure Schlüssel](#hidden-cubes)
- [DPS-Anzeige leeren, Erfassung behalten](#dps-reset)
- [Bosszeiten und Pet Tracker](#bosszeiten-und-pets)
- [Ein wenig aufgeräumt](#aufgeraeumt)
- [Weniger Arbeit im Hintergrund](#performance)

<a id="klassenhilfe"></a>
### 📖 Eine Orientierung für jede Klasse

Im Bedienfeld gibt es einen neuen Buch-Button. Dahinter findet ihr eine kompakte Klassenhilfe für Gladiator, Templer, Assassine, Jäger, Zauberer, Beschwörer, Kleriker und Kantor.

Aktive Fähigkeiten, Stigmas und passive Fähigkeiten haben ihre originalen Spielsymbole dabei. Dazu kommen empfohlene Ziellevel, Ausbau-Prioritäten und die Reihenfolge der Spezialitäten-Slots. Unter Belegung & Makro findet ihr eine Beispielbelegung und eine kopierbare Reihenfolge.

Nutzt das als Einstieg und passt es an euren Level, eure Gruppe und euren Spielstil an. CatScratch führt keine Tastenanschläge oder Makros aus.

Die Grundlage sind [Wakayashis Klassen-Guides](https://wakayashi.gg/aion2#class-guides). Quelle und Datenstand stehen im Fenster; die Inhalte sind lokal enthalten. Bei deutscher App-Sprache erscheint die Hilfe auf Deutsch, bei allen anderen Sprachen auf Englisch.

![Class reference](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.1.0/docs/images/class-guide-skills-de-v4.1.0.png)

<a id="pet-und-seelenbindung"></a>
### 🐾 Genius Pet und Seelenbindungen

Zwei weitere Reiter halten die passenden Empfehlungen zusammen:

- **Genius Pet:** die neun Analyse-Slots mit ihren Prioritäten sowie die Bretter Cognia, Fera, Natura, Varia und Specia – einschließlich der besonderen Möglichkeiten für Slot 4.
- **Seelenbindung:** Prioritäten für alle 13 Ausrüstungsplätze. Empfohlene passive Fähigkeiten und Skilllevel-Effekte wechseln mit der ausgewählten Klasse und zeigen die passenden Fähigkeitensymbole.

Beides gibt es auf Deutsch und Englisch, mit direkten Links zu [Pet-Spezieskenntnis](https://wakayashi.gg/aion2#pet) und [Seelenbindung](https://wakayashi.gg/aion2#soul-binding). Es handelt sich um gespeicherte Guide-Empfehlungen; eure Ausrüstung oder Pet-Bretter werden dafür nicht live geprüft.

![Soul binding](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.1.0/docs/images/class-guide-soul-de-v4.1.0.png)

<a id="live-vorschau"></a>
### 🎨 Einstellungen direkt ausprobieren

Unterstützte Änderungen seht ihr jetzt sofort, während die Einstellungen noch offen sind: Layout, Sichtbarkeit, Auswahl, Größe, Transparenz, Farben, Sprache und Tonvorlieben.

**Speichern** übernimmt die Vorschau und lässt das Fenster offen. Ändert ihr danach noch etwas, verwirft das Schließen nur diese späteren ungespeicherten Änderungen.

Erfassung, Autostart-Anbindung und Hotkeys werden weiterhin mit Speichern übernommen. Die Vorschau von Kampfoptionen setzt den laufenden Kampf nicht zurück. Live-Fortschritt und Checklisten-Einträge behalten ihre eigene Speicherung.

<a id="altgard-karte"></a>
### 🗺️ Das Altgard-Kartenexperiment

**STARK EXPERIMENTELL – optional, standardmäßig ausgeschaltet und vorerst nur für Altgard. Erkennung und Markerpositionen können falsch oder verzögert sein.**

CatScratch kann Fundstellen-Symbole über der geöffneten Altgard-Karte im Spiel anzeigen. Die Bilderkennung ist enthalten; ihr braucht kein zusätzliches Programm.

- Filter direkt auf der Karte ändern, ohne die Einstellungen zu öffnen.
- Sechs aufklappbare Filtergruppen mit Anzeige der aktiven Auswahl.
- Erze und Edelsteine anhand ihrer originalen Materialsymbole unterscheiden. Unterschiedliche Materialien bleiben beim Gruppieren getrennt.
- Keine Materialnamen über der Karte; Federn und Kibelisken wurden aus den Markierungen entfernt.

Die Fundstellen stammen aus Questlog-Kartendaten. Sie zeigen mögliche Positionen, **keine bestätigten aktuellen Spawns**.

Bei geöffneter Karte machen die anderen CatScratch-Module Platz. Danach kommen genau die zuvor sichtbaren Module zurück. Erfassung und Fortschrittsverfolgung laufen währenddessen weiter.

Außerdem habe ich an Bewegung und Zoom, verdeckten Kartenüberschriften, übersetzter Erkennung, Spielfenster-Zuordnung und verständlicheren Statusmeldungen gearbeitet. Nach den Berichten zum Mausflackern wurde die Bildschirmaufnahme überarbeitet. Kartenmarker und Filter sind wieder aus Bildschirmaufnahmen ausgeschlossen.

Alles bleibt passiv: Bildschirmbilder, keine Zugriffe auf den Spielspeicher und keine automatisierten Spieleingaben. Bitte testet weiter mit – gerade unterschiedliche PCs und Anzeige-Einstellungen sind für dieses Experiment wichtig.

<a id="hidden-cubes"></a>
### 🔑 Hidden Cubes und eure Schlüssel

Verschwindet ein bestätigter Würfel in der Nähe, erscheint kurz **„Cube disappeared“** mit unserem ratlosen Maskottchen. Bei dieser Meldung gibt es keine Schlüsselanzeige.

**Key: Hidden Cube (Bound)** lässt sich außerdem im Ressourcenmodul auswählen: mit originalem Spielsymbol, unterstützten Live-Inventarupdates und optionaler Warnung bei niedrigem Bestand. Der gespeicherte Bestand erscheint auch in der Charakterübersicht.

Bestätigte, gespeicherte und unbekannte Werte bleiben unterscheidbar. Ein älterer Stand wird dadurch nicht stillschweigend zu einer bestätigten Null.

<a id="dps-reset"></a>
### 🧹 DPS-Anzeige leeren, Erfassung behalten

Der neue Radierer-Button entfernt die bisherigen Zeilen aus der DPS-Anzeige. Standardmäßig geht das auch mit **Umschalt+F2**; den Hotkey könnt ihr ändern.

Die Erfassung läuft dabei weiter. Vorhandene Kämpfe und bestätigte Gruppenidentitäten bleiben erhalten. Für einen vollständigen Neustart der Erfassung könnt ihr den Button rechts anklicken.

Die Markierung des eigenen Charakters wurde bei gleichen Namen auf unterschiedlichen Servern korrigiert. Die Gruppenprüfungen decken außerdem sechs Mitglieder, reine Heiler, die Zuordnung von Pet-Schaden, Zeilenlimits und wiederholte Gruppenlisten ab.

<a id="bosszeiten-und-pets"></a>
### ⏱️ Bosszeiten und Pet Tracker

Ausgewählte Zeitplan-Bosse behalten ihren nächsten Termin auch dann sichtbar, wenn er mehr als eine Stunde entfernt ist – ebenfalls in separaten Timerfenstern.

Watcher Kaira kann bei fehlender Live-Zeit den gekennzeichneten Community-Zeitplan verwenden. Empfangene Bossdaten haben Vorrang. Neue Zeiten, Sichtungen und Kills aktualisieren Upcoming, Started und separate Bossfenster direkt.

Ich habe alle 24 auswählbaren Feldbosse sowie Seelenstände, Levelaufstiege, gespeicherten Pet-Fortschritt und Charakterwechsel geprüft. Gibt es weder Zeitdaten noch einen passenden Zeitplan, bleibt die Bosszeit ausdrücklich unbekannt.

<a id="aufgeraeumt"></a>
### 🧰 Ein wenig aufgeräumt

- Alte Supporter-Pakete lassen sich unter Einstellungen → Patreon entfernen. Speichern führt die Entfernung aus; vorher könnt ihr sie zurücknehmen oder durch Schließen verwerfen. Die ursprünglich heruntergeladene Datei bleibt erhalten.
- Für betroffene Sounds wird ein passendes verbleibendes Paket oder ein eingebauter Ersatz verwendet.
- Optionale Updatehinweise und kompakte Overlay-Größen verbessert.
- Das Zurückholen einer Position außerhalb des Bildschirms verbessert. Die zurückgeholte Position bleibt beim Schließen erhalten, ohne andere Vorschauänderungen zu speichern.
- Das vorübergehende Alpha-Experiment ist beendet. Stable und Beta bleiben verfügbar; gespeicherte Alpha-Einstellungen wechseln zurück zu Beta.

<a id="performance"></a>
### ⚙️ Weniger Arbeit im Hintergrund

Performance ist mir wichtig – schließlich läuft CatScratch oft einen ganzen Spielabend nebenher.

Die Karte braucht weniger wiederholte Bildverarbeitung. Unveränderte Marker und Filter werden seltener neu aufgebaut, und die Markerberechnung erzeugt weniger kurzlebige Objekte. Hinter der Karte ausgeblendete Module sparen Darstellungsarbeit; Ressourcen, Dungeon-Eintritte und Pet-Fortschritt zeigen beim Schließen sofort den aktuellen Stand.

Auch die Einstellungsvorschau erledigt weniger doppelte Arbeit. Beim Speichern von Ressourcen und Pet-Fortschritt werden andere Abläufe kürzer aufgehalten. Datenerfassung und Pet-Levelhinweise laufen bei offener Karte weiter.

Die lokalen Verarbeitungstests sind schneller geworden. Wie viel das auf eurem PC ausmacht, hängt vom jeweiligen System ab – besonders mit eingeschaltetem Kartenexperiment.

Danke, dass ihr dieser kleinen Katze beim Aufräumen helft. 💜

## CatScratch v4.0.9-beta.17

Ich habe unter der Haube etwas aufgeräumt. Weniger unnötige Arbeit für CatScratch, mehr Luft fürs Spiel. 🐾

- Die Karte folgt Bewegung und Zoom mit weniger wiederholter Bildverarbeitung. Unveränderte Marker und Filter werden nicht ständig neu aufgebaut, und die Markerberechnung erzeugt deutlich weniger kurzlebige Objekte.
- Hinter der Karte ausgeblendete Module sparen unnötige Darstellungsarbeit. Ressourcen, Dungeon-Eintritte und Pet-Fortschritt zeigen beim Schließen der Karte sofort den aktuellen Stand; Datenerfassung und Pet-Levelhinweise laufen weiter.
- Die Live-Vorschau der Einstellungen arbeitet sparsamer. Beim Speichern von Ressourcen und Pets bleiben die Daten kürzer gesperrt. Der Kartenstatus meldet keinen Abgleich mehr, bevor eine offene Karte bestätigt wurde.
- Das Zurückholen einer Position außerhalb des Bildschirms in den Einstellungen verbessert. Die zurückgeholte Position bleibt beim Schließen erhalten, ohne andere ungespeicherte Vorschauänderungen zu übernehmen.

Das Altgard-Kartenoverlay bleibt stark experimentell. Die lokalen Verarbeitungstests sind schneller geworden; denselben FPS-Gewinn auf jedem PC kann ich damit nicht versprechen. Die vollständige Kartensuche behält ihre bisherigen Prüfungen.

## CatScratch v4.0.9-beta.16

Noch einen kleinen Stau an der Kartenüberschrift gefunden. 🐾

- Verdecken CatScratch-Module den Altgard-Titel, kann die Kartenengine den Geländeabgleich jetzt über zwei sichtbare Karten-Bedienelemente starten. Erst nach bestätigtem Altgard-Abgleich öffnet sich das Overlay. Die eingesandte Aufnahme wird damit erkannt; der Live-Test beim betroffenen Nutzer steht noch aus.
- Kartenmarker und Filter sind wie gewünscht wieder aus Bildschirmaufnahmen ausgeschlossen. Die zusätzliche Aussparung der Markerflächen wurde entfernt, um zum früheren Nachführungsweg zurückzukehren.
- Die sprachunabhängigere Altgard-Erkennung und die verbesserten Statusangaben bleiben erhalten. Prüfungen für verdeckte Titel und das Abweisen unbestätigter Karten ergänzt.
- Die Altgard-Kartenfunktion bleibt stark experimentell.

## CatScratch v4.0.9-beta.15

Ein weiterer kleiner Schritt für das Kartenexperiment. 🐾

- Die Kartenerkennung spart eigene Marker und die Filterleiste beim Geländeabgleich aus. Das soll dem kurzen Aufblitzen und Verschwinden der Symbole entgegenwirken. In Bildschirmaufnahmen bleiben sie sichtbar.
- Der gemeinsame Gebietsname Altgard wird nun unabhängig vom englischen Map-Präfix erkannt. Die Prüfungen für den übersetzten Titel sind bestanden; die Bestätigung beim gemeldeten deutschen Client steht noch aus.
- Der Kartenstatus zeigt den Vordergrundprozess und behält beim Wechsel zu den Einstellungen den letzten Spielstatus bei. Das macht die Fehlersuche aus der Ferne verständlicher.
- Synthetische Nachführungsprüfungen mit wechselnden Overlay-Pixeln ergänzt. Bitte weiter testen: Das Altgard-Kartenoverlay bleibt stark experimentell.

## CatScratch v4.0.9-beta.14

Zwei Korrekturen für das Altgard-Kartenexperiment. 🐾

- Das Kartenmodul erkennt jetzt beide bekannten AION-2-Clientnamen, einschließlich AION2-Win64-Shipping.exe. Ein ausgewählter Launcher verhindert die Erkennung dieser Clients nicht mehr.
- Kartenmarker und Filter schließen sich nicht mehr von Bildschirmaufnahmen und Bildschirmübertragungen aus. Bitte mit eurer Streamingsoftware testen; eine reine Spielaufnahme kann separate Overlayfenster weiterhin auslassen.
- Prüfungen für beide Clientnamen und die Aufnahmefreigabe ergänzt. Die Kartenfunktion bleibt stark experimentell und auf Altgard beschränkt.

## CatScratch v4.0.9-beta.13

Heute habe ich die Timer etwas aufgeräumt. 🐾

- Ausgewählte Zeitplan-Bosse behalten ihren nächsten Termin auch dann, wenn er mehr als eine Stunde entfernt ist – ebenfalls in separaten Timerfenstern.
- Der NPC-Eintrag von Watcher Kaira nutzt bei fehlender Live-Zeit den vorhandenen Community-Zeitplan. Die Quelle bleibt gekennzeichnet; empfangene Bossdaten haben Vorrang.
- Neue Bosszeiten, Sichtungen und Kills aktualisieren Upcoming, Started und separate Bossfenster direkt, ohne auf den nächsten Timertakt zu warten.
- Alle 24 auswählbaren Feldbosse sowie Live-Seelenstände, Levelaufstiege, gespeicherter Fortschritt und Charakterwechsel im Pet Tracker geprüft. Ohne Zeitdaten oder passenden Zeitplan bleibt die Bosszeit ausdrücklich unbekannt.

## CatScratch v4.0.9-beta.12

- Kartenfilter in sechs aufklappbare Gruppen sortiert, jeweils mit Anzeige der aktiven Auswahl. Die Einstellungen verwenden dieselbe Reihenfolge; Zuklappen ändert keine Häkchen.
- Materialnamen auf der Karte entfernt. Die unterschiedlichen Originalsymbole und die Anzahl gruppierter Fundstellen bleiben erhalten.
- Die passive Bildschirmaufnahme nach dem erneuten Mausflackern-Bericht überarbeitet. Bei geschlossener Karte wird nur noch ein kleiner Titelbereich seltener geprüft; vollständige Bilder werden für die offene Karte aufgenommen. Der kurze lokale Vergleich war flackerfrei – bitte dieses stark experimentelle Feature weiter testen.

## CatScratch v4.0.9-beta.11

- Erz- und Edelsteinmarker zeigen jetzt den genauen Materialnamen und ihr jeweiliges originales Spielsymbol, darunter Sapphire, Diamond, Ruby und Orichalcum. Unterschiedliche Materialien bleiben auch beim Gruppieren benachbarter Marker getrennt.
- Federn und Kibelisken aus der Kartenanzeige und den Filtern entfernt.
- Deutliche Warnung **STARK EXPERIMENTELL** in den Einstellungen und direkt auf der Karte ergänzt. Erkennung und Positionen können weiterhin falsch oder verzögert sein.
- Unnötiges Nach-vorne-Setzen der Fenster reduziert; das reine Marker-Overlay ersetzt den Spielcursor nicht mehr. Damit gehe ich das gemeldete Mausflackern an – bitte beim Testen weiter beobachten.

## CatScratch v4.0.9-beta.10

- Einen kleinen CatScratch-Stau aufgelöst: Die Ressourcenanzeige konnte den Altgard-Titel verdecken und damit die Kartenerkennung blockieren. Die Erkennung nutzt jetzt auch die unverdeckten Teile des Titels, damit die anderen Module wie vorgesehen Platz machen.
- Markierungen direkt auf der Karte ändern: Die kompakte Kartenfilter-Leiste bietet einzelne Kategorien sowie Cubes, Alle und Keine. Dafür müsst ihr nicht mehr in die Einstellungen wechseln.
- Zuordnung des Spielfensters und Statusmeldungen der Erkennung verbessert. Alles bleibt passiv; weiterhin nur Altgard.

## CatScratch v4.0.9-beta.9

Das Altgard-Kartenexperiment ist bereit für seinen ersten Test direkt in CatScratch. 🐾 Einschalten könnt ihr es unter Einstellungen > Kartenoverlay. Die Bilderkennung ist bereits enthalten und braucht keine separate Einrichtung.

- Wählt aus 41 Questlog-Kategorien. Die Marker zeigen mögliche Fundstellen, keine bestätigten aktuellen Spawns. Dicht beieinanderliegende Symbole werden gruppiert.
- Bei geöffneter Altgard-Karte machen die anderen Anzeigemodule Platz und kehren beim Schließen zurück. Die Erfassung läuft dabei weiter.
- Die Nachführung hilft den Symbolen beim Ziehen zwischen den Bildprüfungen mitzugehen. Große Zoomwechsel können noch einen kurzen Neuabgleich brauchen. Diese erste Beta unterstützt ausschließlich Altgard.
- Die Karteneinstellungen haben eine Live-Vorschau. Speichern übernimmt sie; Schließen verwirft ungespeicherte Änderungen. Die Funktion ist zunächst ausgeschaltet.

## CatScratch v4.0.9-beta.8

- Hidden-Cube-Schlüssel verwenden in Ressourcen und Einstellungen jetzt ihr originales Spielsymbol.

## CatScratch v4.0.9-beta.7

- Gebundene Hidden-Cube-Schlüssel sind jetzt im Ressourcen-Modul auswählbar, mit Live-Bestand und optionaler Bestandswarnung. Sie erscheinen auch in der Charakterübersicht.

## CatScratch v4.0.9-beta.6

- DPS-Anzeige leeren, ohne die Erfassung neu zu starten: über den Radierer-Button oder den einstellbaren Hotkey Shift+F2. Bisherige Kämpfe und bestätigte Gruppenmitglieder bleiben erhalten. Rechtsklick auf den Button bietet den vollständigen Erfassungs-Neustart.
- Die Gruppenübersicht markiert den eigenen Charakter auch bei gleichen Namen auf verschiedenen Servern eindeutig.
- Erweiterte Gruppentests prüfen sechs Mitglieder, einen reinen Heiler, Pet-Zuordnung, Anzeigegrenzen, wiederholte Gruppenmeldungen und das Zurücksetzen der Messung.

## CatScratch v4.0.9-beta.5

- Wenn das Verschwinden eines zuvor erkannten Würfels bestätigt wird, erscheint kurz „Cube disappeared“ mit der ratlosen Katze – ohne Schlüsselanzeige.
- Das vorübergehende Alpha-Experiment ist beendet. Zur Auswahl stehen wieder Stable und Beta; gespeicherte Alpha-Einstellungen wechseln zurück zu Beta.

## CatScratch v4.0.9-beta.4

- Alpha ist jetzt unter Einstellungen > Updates verfügbar. Vor dem Speichern oder der Update-Suche muss der experimentelle Kanal ausdrücklich per Häkchen bestätigt werden. Zurück zu Beta oder Stable geht es im selben Menü.
- Diese Beta behält das bisherige Design. Die neuen Persönlichkeitsdetails gibt es ausschließlich in Alpha.

## CatScratch v4.0.9-beta.3

- Alte Supporter-Pakete unter Einstellungen > Patreon entfernen. Speichern übernimmt die Entfernung; Zurücknehmen oder Schließen verwirft sie. Verwendete Sounds wechseln zu einem passenden verbleibenden Paket oder einem Standardton.

## CatScratch v4.0.9-beta.2

- Optionale Update-Hinweise und die Größe kleiner Overlay-Fenster verbessert.

## CatScratch v4.0.9-beta.1

- Einstellungen zeigen Anordnung, Sichtbarkeit, Auswahl, Größe, Transparenz, Farben, Sprache und Tonoptionen sofort als Live-Vorschau.
- Speichern übernimmt den aktuellen Stand. Schließen verwirft spätere Änderungen; Autosaves, Checklisten-Inhalte und Spielfortschritt bleiben getrennt.
- Erfassung, Autostart und Tastenkürzel werden weiterhin mit Speichern angewendet. Die Vorschau von Kampfoptionen setzt keinen laufenden Kampf zurück.

## CatScratch v4.0.8

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.8/data/patchnotes-en.md)

Ein bisschen mehr Katze ist in CatScratch eingezogen. Ich habe Maskottchen, Schriftzug und Katzenprofil in der Oberfläche zusammengebracht, damit die verschiedenen Fenster sich mehr wie ein gemeinsames Zuhause anfühlen. 🐾

<!-- contents -->
### Auf einen Blick
- [Ein vertrautes Gesicht in CatScratch](#shared-identity)
- [Gestaltung, die Platz zum Spielen lässt](#clear-controls)
- [Ein frischer Anstrich für GitHub](#github-look)
<!-- /contents -->

<a id="shared-identity"></a>
### 🐱 Ein vertrautes Gesicht in CatScratch

Im Kopf der Einstellungen stehen jetzt unser Maskottchen und der CatScratch-Schriftzug zusammen. Das dezente Katzenprofil begleitet euch als Wasserzeichen durch die Einstellungsseiten. Auch die größeren Fenster teilen sich die Gestaltung, darunter Charakterübersicht, zuletzt zusammengespielte Spieler und Updates.

Das kompakte Bedienfeld nutzt den neuen Schriftzug im bisherigen Titelplatz. Die schmale vertikale Ansicht behält ihren lesbaren, untereinander geschriebenen Namen. **Update!** erscheint weiterhin an derselben Stelle, sobald ein Update bereitsteht.

![CatScratch-Einstellungen](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.0.8/docs/images/branding-settings-v4.0.8.png)

<a id="clear-controls"></a>
### ✨ Gestaltung, die Platz zum Spielen lässt

Die kleinen Spielmodule bekommen eine deutlich kleinere und zurückhaltendere Signatur. Die Dekoration beansprucht keinen zusätzlichen Platz, fängt keine Klicks ab und verschiebt keine gespeicherten Fenster.

Texte und Bedienelemente bleiben vom Motiv frei. Im Update-Fenster sitzen Wasserzeichen und Schriftzug vollständig im Patchnotes-Bereich, oberhalb der Knöpfe. Beim Scrollen oder Wechseln der Einstellungsseite wird die Darstellung ebenfalls angepasst.

<a id="github-look"></a>
### 💜 Ein frischer Anstrich für GitHub

Die Projektseite bekommt den passenden neuen Banner, aktualisierte Screenshots und eine Beschreibung der gemeinsamen Gestaltung. Danke, dass ihr mir helft, diesem kleinen Projekt ein eigenes Gesicht zu geben.

## CatScratch v4.0.7

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.7/data/patchnotes-en.md)

Die Würfelmeldung hat jetzt etwas mehr CatScratch in sich. Ich habe ihr einen neuen Look gegeben und den Schlüsselbestand direkt dort ergänzt, wo ihr ihn braucht. 🐾

<!-- contents -->
### Auf einen Blick
- [Ein neues Gesicht für versteckte Würfel](#cube-banner)
- [Eure Schlüssel direkt in der Meldung](#cube-keys)

<!-- /contents -->

<a id="cube-banner"></a>
### 🐱 Ein neues Gesicht für versteckte Würfel

Die Meldung erscheint jetzt als kompakter dunkler Banner mit sanft leuchtendem violettem Rahmen, unserem aufmerksamen Maskottchen und einem Katzenprofil am rechten Rand. Gespeicherte Position, Fixierung und Durchklickmodus funktionieren weiterhin wie gewohnt.

![Neue Würfelmeldung](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.0.7/docs/images/hidden-cube-v4.0.7.png)

<a id="cube-keys"></a>
### 🔑 Eure Schlüssel direkt in der Meldung

Der Banner kann jetzt den Bestand von **Key: Hidden Cube (Bound)** aus unterstützten Inventarnachrichten anzeigen. Erkannte Änderungen aktualisieren die Zahl auch bei geöffneter Meldung. Gespeicherte Bestände bleiben nach Charakter getrennt.

Ein **≈** kennzeichnet den zuletzt gespeicherten Bestand, der noch auf eine frische Bestätigung wartet. Ist kein Bestand bekannt, steht das ausdrücklich dabei – fehlende Daten werden nicht als null Schlüssel ausgegeben. Unterstützt wird zunächst der gebundene Schlüssel; andere Varianten müssen noch gesondert geprüft werden.

Danke für die Mitschnitte und eure vielen kleinen Gestaltungsideen. Sie helfen mir dabei, CatScratch ein bisschen mehr Zuhause zu geben. 💜

## CatScratch v4.0.6

[English patch notes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.6/data/patchnotes-en.md)

<!-- contents -->
### Auf einen Blick
- [Ein bisschen Feinschliff](#small-polish)
<!-- /contents -->

<a id="small-polish"></a>
### 🛠️ Ein bisschen Feinschliff

Heute gibt es ein kleines Wartungsupdate von mir. Gespeicherte Anzeigeeinstellungen werden zuverlässiger übernommen, damit Änderungen ohne zusätzliches Aus- und Einschalten ankommen.

Danke für die schnellen Rückmeldungen und dafür, dass ihr mir helft, die kleinen Ecken und Kanten auszubessern. 💜

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

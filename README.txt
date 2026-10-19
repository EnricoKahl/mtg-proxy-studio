MTG PROXY STUDIO - WINDOWS-BUILDPAKET

1. Falls das .NET 8 SDK noch nicht installiert ist:
   DOTNET-SDK-installieren.bat doppelt anklicken.

2. Danach EXE-erstellen.bat doppelt anklicken.

3. Die fertige portable Datei liegt anschließend hier:
   Fertige_EXE\MTGProxyStudio.exe

Beim ersten Build wird PDFsharp-GDI über NuGet geladen. Dafür wird eine
Internetverbindung benötigt. Die fertige EXE ist selbstständig lauffähig und
benötigt auf anderen Windows-64-Bit-Rechnern kein installiertes .NET.

FUNKTIONEN

Downloader:
- Moxfield-Export mit Menge, Set-Code und Sammlernummer
- mehrere Kartensprachen
- exakte Moxfield-Edition oder alternative lokalisierte Drucke
- Auswahlfenster mit Motivvorschau
- optional Sonderrahmen ausblenden
- automatisch neuesten regulären Druck wählen
- hochauflösende PNG-Dateien
- offizielle Tokens und Embleme, standardmäßig zweimal
- Tokensuche unabhängig von der Sprache der Ursprungskarte
- gewählte Tokensprache bevorzugen, englischer Fallback bei fehlender Übersetzung
- automatische Bewertung nach Name, Oracle-ID, Typ, Text, Farbe, Stärke/Widerstandskraft und passendem Token-Set
- manuelle Token-Motivauswahl bei mehreren Treffern
- token_manifest.csv und token_manifest.json mit verwendeter Sprache und Fallback-Status
- zweiseitige Karten: Vorder- und Rückseite werden separat gespeichert
- manifest.csv und manifest.json

Druckbogen:
- Acrobat-kompatibles PDF über PDFsharp
- A4 und 3x3-Raster
- Kartenformat 63 x 88 mm
- mittige Positionierung
- unvollständige Reihen ebenfalls mittig
- einstellbarer Abstand und Sicherheitsrand
- Schnittmarken, Kartennummern und Seitenzahlen
- Unterordner für Karten und Tokens

MOXFIELD-EXPORTFORMAT

1 Sol Ring (CMM) 396
1 Etali, Primal Conqueror (MOM) 137

DRUCKEN

In Acrobat Reader "Tatsächliche Größe" beziehungsweise 100 % auswählen.
Nicht "Anpassen" oder "Auf Seitengröße verkleinern" verwenden.

TOKEN-MODI

- Gewählte Sprache bevorzugen, Englisch als Fallback
- Immer Englisch verwenden
- Bei mehreren passenden Tokens nachfragen
- Jeden Token manuell auswählen

Zweiseitige Tokens werden wie zweiseitige Karten behandelt. Vorder- und
Rückseite werden als getrennte PNG-Dateien gespeichert und sind beide druckbar.

Hinweis: Scryfall verknüpft erzeugte Tokens häufig über englische Referenzobjekte.
Die Software verwendet diese nur als Referenz, sucht anschließend nach passenden
Drucken in der gewählten Sprache und fällt notfalls zuverlässig auf Englisch zurück.

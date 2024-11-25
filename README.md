# Image Reviewer

Eine moderne WPF-Anwendung zur effizienten Bildverwaltung und -auswahl.

## Features

- **Modernes UI-Design** mit MaterialDesign-Theme
- **Filmstreifen-Ansicht** für schnelles Durchblättern
- **Großbildvorschau** des aktuell ausgewählten Bildes
- **Mehrfachauswahl** von Bildern
- **Hover-Zoom** für Detailansicht
- **Tastatursteuerung** für effiziente Navigation
- **Asynchrones Bildladen** für flüssige Performance
- **Export-Funktion** für ausgewählte Bilder

## Systemanforderungen

- Windows 10 oder höher
- .NET 8.0
- Mindestens 4GB RAM
- Grafikkarte mit DirectX 11 Unterstützung

## Installation

1. Stellen Sie sicher, dass .NET 8.0 SDK installiert ist
2. Klonen Sie das Repository
3. Öffnen Sie die Solution in Visual Studio
4. Führen Sie einen Build aus

```bash
git clone [repository-url]
cd ImageReviewer
dotnet build
```

## Verwendung

### Bilder laden
1. Klicken Sie auf "Ordner wählen"
2. Wählen Sie einen Ordner mit Bildern aus
3. Die Bilder werden im Filmstreifen angezeigt

### Navigation
- **←/→ Pfeiltasten**: Zwischen Bildern navigieren
- **Leertaste**: Bild auswählen/abwählen
- **Mausklick**: Bild direkt auswählen
- **Hover**: Vergrößerte Vorschau anzeigen

### Bilder exportieren
1. Wählen Sie die gewünschten Bilder aus
2. Klicken Sie auf "..." um den Zielordner zu wählen
3. Klicken Sie auf "Markierte Bilder exportieren"

## Technische Details

### Frameworks & Bibliotheken
- WPF (.NET 8.0)
- MaterialDesignThemes (v4.9.0)
- MaterialDesignColors (v2.1.4)

### Architektur
- MVVM-Pattern
- Asynchrone Bildverarbeitung
- Ereignisbasierte UI-Aktualisierung
- Speicheroptimierte Bildverwaltung

### Performance-Optimierungen
- Lazy Loading von Bildern
- Thumbnail-Generierung
- Asynchrones Laden und Verarbeiten
- Effiziente Speicherverwaltung

## Lizenz

MIT License - siehe LICENSE.md

## Entwickler

Entwickelt von [Ihr Name/Team]

## Beitragen

1. Fork das Projekt
2. Erstellen Sie einen Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit Sie Ihre Änderungen (`git commit -m 'Add some AmazingFeature'`)
4. Push zum Branch (`git push origin feature/AmazingFeature`)
5. Öffnen Sie einen Pull Request
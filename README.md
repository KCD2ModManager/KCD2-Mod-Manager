# KCD2 Mod Manager - Refactored Version

## Überblick
Dies ist die vollständig refactorierte Version des KCD2 Mod Managers mit moderner MVVM-Architektur, Dependency Injection und sauberer Code-Struktur.

## Was wurde geändert?

### Architektur
- ✅ MVVM-Pattern vollständig implementiert
- ✅ Dependency Injection mit Microsoft.Extensions.DependencyInjection
- ✅ Service-Layer für alle Geschäftslogik
- ✅ ViewModels mit Commands statt Event-Handler
- ✅ Async/await für alle IO- und HTTP-Operationen

### Projektstruktur
```
KCD2 mod manager/
├── Models/              # Domain-Modelle
├── ViewModels/          # ViewModels
├── Services/            # Service-Implementierungen
├── Views/               # (Vorbereitet für zukünftige Trennung)
├── Resources/           # (Vorbereitet für Lokalisierung)
└── Tests/              # (Vorbereitet für Unit Tests)
```

## Build & Start

### Voraussetzungen
- .NET 10 SDK
- Visual Studio 2022 oder höher (oder VS Code mit C# Extension)

### Build
```bash
dotnet build
```

### Start
```bash
dotnet run
```

## Testing

### Unit Tests (geplant)
```bash
dotnet test
```

## Architektur-Übersicht

### Services
- **IModManifestService**: Manifest-Parsing und -Generierung
- **IFileService**: Datei- und Verzeichnisoperationen
- **INexusService**: Nexus Mods API-Integration
- **IModInstallerService**: Mod-Installation und -Verwaltung
- **IDialogService**: Dialog-Wrapper
- **IAppSettings**: Einstellungen-Verwaltung
- **ILog**: Logging-Funktionalität

### ViewModels
- **MainWindowViewModel**: Hauptlogik für MainWindow

### Models
- **Mod**: Mod-Repräsentation
- **ModVersionInfo**: Versionsinformationen
- **NexusModFile**: Nexus Mods API-Modelle

## Logging
Logs werden im `logs/`-Verzeichnis gespeichert mit täglichem Rolling.

## Bekannte Einschränkungen
- Einige UI-spezifische Event-Handler bleiben im Code-Behind (Drag & Drop, Context-Menus)
- SettingsWindowViewModel muss noch erstellt werden
- Unit Tests müssen noch geschrieben werden
- Lokalisierung (.resx) muss noch implementiert werden

## Manuelle Tests empfohlen
1. Mod-Installation aus ZIP/RAR/7z
2. Mod-Installation aus Ordner
3. Mod-Update-Funktionalität
4. Nexus SSO-Login
5. Mod-Order-Verwaltung (Drag & Drop)
6. Backup-Erstellung
7. Theme-Wechsel (Dark/Light Mode)
8. Suchfunktion
9. Sort-Funktionalität

## Entwickler-Hinweise

### Neue Features hinzufügen
1. Service-Interface erstellen (z.B. `INewService`)
2. Service-Implementierung erstellen (z.B. `NewService`)
3. In `App.xaml.cs` registrieren
4. Im ViewModel injizieren und verwenden

### ViewModel erweitern
1. Property hinzufügen mit `SetProperty`
2. Command erstellen mit `RelayCommand`
3. Im XAML binden

## Changelog
Siehe [CHANGELOG.md](CHANGELOG.md) für detaillierte Änderungen.

## Credits
- Original-Entwickler: coolzumjax
- Refactoring: Vollständig automatisiert durch AI-Assistent

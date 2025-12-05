# Changelog - KCD2 Mod Manager Refactoring

## Übersicht
Vollständiges Refactoring des KCD2 Mod Managers von einem monolithischen Code-Behind-Ansatz zu einer sauberen MVVM-Architektur mit Dependency Injection.

## Datum: 2025

## Hauptänderungen

### Architektur
- **MVVM-Pattern eingeführt**: Vollständige Trennung von UI und Geschäftslogik
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection integriert
- **Service-Layer**: Alle Geschäftslogik in Services extrahiert
- **ViewModels**: MainWindowViewModel mit Commands statt Event-Handler

### Projektstruktur
- **Neue Ordnerstruktur**:
  - `Models/` - Domain-Modelle (Mod, ModVersionInfo, NexusModFile)
  - `ViewModels/` - ViewModels (MainWindowViewModel, RelayCommand)
  - `Services/` - Service-Implementierungen (IFileService, INexusService, etc.)
  - `Views/` - Vorbereitet für zukünftige View-Trennung
  - `Resources/` - Vorbereitet für Lokalisierung
  - `Tests/` - Vorbereitet für Unit Tests

### Services erstellt

#### IModManifestService / ModManifestService
- Manifest-Parsing und -Generierung
- XML-Version-Korrektur
- Mod-ID-Generierung

#### IFileService / FileService
- Alle Datei- und Verzeichnisoperationen
- Asynchrone Archivextraktion
- Atomare Schreiboperationen

#### INexusService / NexusService
- Nexus Mods SSO-Login
- Mod-Datei-Abruf
- Update-Prüfung
- Download-Link-Generierung

#### IModInstallerService / ModInstallerService
- Mod-Installation aus Archiven
- Mod-Installation aus Ordnern
- Mod-Updates
- Mod-Order-Verwaltung
- Backup-Erstellung

#### IDialogService / DialogService
- MessageBox-Wrapper
- File-Dialog-Wrapper
- Input-Dialog-Wrapper
- Delete-Confirmation-Dialog

#### IAppSettings / AppSettings
- Wrapper um Settings.Default
- Einheitliche Schnittstelle für Einstellungen

#### ILog / Logger
- Serilog-Integration
- Datei-Logging mit Rolling-Intervals

### ViewModels

#### MainWindowViewModel
- Alle Commands als RelayCommand implementiert
- ObservableCollection für Mods
- Search-Filter-Funktionalität
- Sort-Funktionalität
- Status-Updates

### Code-Änderungen

#### MainWindow.xaml.cs
**Vorher**: ~2514 Zeilen mit gesamter Geschäftslogik
**Nachher**: ~400 Zeilen, nur UI-spezifische Logik

**Entfernt**:
- Alle Geschäftslogik-Methoden
- Direkte Service-Aufrufe
- MessageBox-Aufrufe (außer Theme-Updates)

**Hinzugefügt**:
- ViewModel-Injection über Constructor
- Command-Bindings
- UI-spezifische Event-Handler (Drag & Drop)

#### MainWindow.xaml
- Unverändert (visuell identisch)
- Event-Handler bleiben für Drag & Drop und UI-Events

### Async/await Refactoring
- Alle IO-Operationen sind jetzt asynchron
- Alle HTTP-Calls sind asynchron
- Keine Blocking-Calls mehr (.Wait(), .Result)
- CancellationToken-Support in allen async-Methoden

### .NET Upgrade
- **Vorher**: .NET 8.0-windows
- **Nachher**: .NET 10.0-windows
- NuGet-Pakete aktualisiert:
  - Microsoft.Extensions.DependencyInjection 9.0.0
  - Microsoft.Extensions.Http 9.0.0
  - Serilog 4.1.0
  - Serilog.Sinks.File 6.0.0

### Backups
- `MainWindow.xaml.cs.bak` - Original-Version
- `SettingsWindow.xaml.cs.bak` - Original-Version
- `App.xaml.cs.bak` - Original-Version

### Offene Punkte
- Unit Tests müssen noch geschrieben werden
- Lokalisierung (.resx) muss noch implementiert werden
- SettingsWindowViewModel muss noch erstellt werden
- Einige komplexe Update-Logik im MainWindowViewModel muss noch vollständig implementiert werden

### Bekannte Probleme
- Einige Event-Handler im XAML verwenden noch Click-Events statt Commands (Drag & Drop, Context-Menus)
- Diese bleiben aus Kompatibilitätsgründen bestehen, da sie UI-spezifisch sind

### Entscheidungen
1. **ViewModelBase**: Eigene Implementierung statt CommunityToolkit.Mvvm (keine zusätzliche Abhängigkeit)
2. **RelayCommand**: Eigene Implementierung für maximale Kontrolle
3. **Serilog**: Für professionelles Logging gewählt
4. **HttpClientFactory**: Für korrekte HttpClient-Verwaltung
5. **Singleton vs Transient**: Services als Singleton, ViewModels als Transient

### Dateien erstellt
- `Models/Mod.cs`
- `Models/ViewModelBase.cs`
- `Models/ModVersionInfo.cs`
- `Models/NexusModFile.cs`
- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/RelayCommand.cs`
- `Services/ILog.cs`
- `Services/Logger.cs`
- `Services/IAppSettings.cs`
- `Services/AppSettings.cs`
- `Services/IDialogService.cs`
- `Services/DialogService.cs`
- `Services/IFileService.cs`
- `Services/FileService.cs`
- `Services/IModManifestService.cs`
- `Services/ModManifestService.cs`
- `Services/INexusService.cs`
- `Services/NexusService.cs`
- `Services/IModInstallerService.cs`
- `Services/ModInstallerService.cs`
- `.editorconfig`

### Dateien geändert
- `KCD2 mod manager.csproj` - .NET 10, NuGet-Pakete
- `App.xaml.cs` - DI-Setup
- `App.xaml` - StartupUri entfernt
- `MainWindow.xaml.cs` - Vollständig refactoriert
- `MainWindow.xaml` - Unverändert (visuell identisch)

### Dateien gelöscht
- Keine (alle Originale als .bak gesichert)


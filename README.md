# KCD2 Mod Manager – Refactored Version

## Overview

This is the fully refactored version of the KCD2 Mod Manager featuring a modern MVVM architecture, dependency injection, and clean code structure.

## What Has Changed?

### Architecture

* ✅ Fully implemented MVVM pattern
* ✅ Dependency Injection using Microsoft.Extensions.DependencyInjection
* ✅ Service layer for all business logic
* ✅ ViewModels use commands instead of event handlers
* ✅ Async/await for all IO and HTTP operations

### Project Structure

```
KCD2 mod manager/
├── Models/              # Domain models
├── ViewModels/          # ViewModels
├── Services/            # Service implementations
├── Views/               # (Prepared for future separation)
├── Resources/           # (Prepared for localization)
└── Tests/               # (Prepared for unit tests)
```

## Build & Run

### Requirements

* .NET 10 SDK
* Visual Studio 2022 or newer (or VS Code with C# extension)

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

## Testing

### Unit Tests (planned)

```bash
dotnet test
```

## Architecture Overview

### Services

* **IModManifestService**: Manifest parsing and generation
* **IFileService**: File and directory operations
* **INexusService**: Nexus Mods API integration
* **IModInstallerService**: Mod installation and management
* **IDialogService**: Dialog wrapper
* **IAppSettings**: Settings management
* **ILog**: Logging functionality

### ViewModels

* **MainWindowViewModel**: Main logic for MainWindow

### Models

* **Mod**: Mod representation
* **ModVersionInfo**: Version information
* **NexusModFile**: Nexus Mods API models

## Logging

Logs are stored in the `logs/` directory with daily rolling.

## Known Limitations

* Some UI-specific event handlers remain in the code-behind (Drag & Drop, context menus)
* SettingsWindowViewModel still needs to be created
* Unit tests still need to be written
* Localization (.resx) still needs to be implemented

## Recommended Manual Tests

1. Mod installation from ZIP/RAR/7z
2. Mod installation from folder
3. Mod update functionality
4. Nexus SSO login
5. Mod order management (drag & drop)
6. Backup creation
7. Theme switching (dark/light mode)
8. Search functionality
9. Sort functionality

## Developer Notes

### Adding New Features

1. Create service interface (e.g., `INewService`)
2. Create service implementation (e.g., `NewService`)
3. Register it in `App.xaml.cs`
4. Inject into the ViewModel and use it

### Extending a ViewModel

1. Add a property using `SetProperty`
2. Create a command using `RelayCommand`
3. Bind it in XAML

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for detailed changes.

## Credits

* Original developer: coolzumjax

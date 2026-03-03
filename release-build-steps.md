## Release Build Steps (Win x64)

### Prerequisites
- .NET SDK installed
- WiX Toolset installed (via NuGet in installer project)

### 1) Update installer version
- File: `C:\Users\rehbe\source\repos\KCD2ModManagerInstaller\Product.wxs`
- Bump `Package Version` (e.g. `1.2.2.0` -> `1.2.3.0`)

### 2) Publish portable single-file (standalone)
From `C:\Users\rehbe\source\repos\KCD2-mod-manager refactor`:
```
dotnet publish "KCD2 mod manager\KCD2 mod manager.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:PublishTrimmed=false -o "KCD2 mod manager\bin\Release\net10.0-windows\portable-version"
```

### 3) Publish normal standalone (non single-file)
```
dotnet publish "KCD2 mod manager\KCD2 mod manager.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false /p:PublishTrimmed=false -o "KCD2 mod manager\bin\Release\net10.0-windows\publish\win-x64"
```

### 4) Build installer
From `C:\Users\rehbe\source\repos\KCD2ModManagerInstaller`:
```
dotnet build "KCD2ModManager.wixproj" -c Release
```

### 5) Create release zips
Portable zip:
```
Compress-Archive -Path "C:\Users\rehbe\source\repos\KCD2-mod-manager refactor\KCD2 mod manager\bin\Release\net10.0-windows\portable-version\*" `
  -DestinationPath "C:\Users\rehbe\source\repos\KCD2-mod-manager refactor\KCD2 mod manager (portable).zip"
```

Installer zip (MSI + CAB only):
```
Compress-Archive -Path "C:\Users\rehbe\source\repos\KCD2ModManagerInstaller\bin\Release\KCD2ModManager.msi","C:\Users\rehbe\source\repos\KCD2ModManagerInstaller\bin\Release\cab1.cab" `
  -DestinationPath "C:\Users\rehbe\source\repos\KCD2-mod-manager refactor\KCD2 mod manager (installer).zip"
```

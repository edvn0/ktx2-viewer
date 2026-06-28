# KTX Viewer

Simple GUI application for viewing KTX and KTX2 texture files. Built with .NET 9, C# 13, WPF, Clean Architecture, and Liquid Glass design.

## Features

- ✅ Load and display KTX and KTX2 texture files
- ✅ Automatic format detection (Strategy pattern)
- ✅ Support for BasisU/ETC1S supercompression (via libktx)
- ✅ Display detailed texture metadata
- ✅ File associations - open files by double-click
- ✅ Command-line support
- ✅ Progress bar for loading large files
- ✅ MVVM architecture with CommunityToolkit.Mvvm
- ✅ Clean Architecture (Core → Application → Infrastructure → UI)
- ✅ Liquid Glass design system with transparency and blur effects
- ✅ Comprehensive unit and integration tests

## Requirements

- .NET 9 Runtime
- Windows 10/11
- **libktx.dll** (for BasisU ETC1S texture support)

## Platform Support

**This application is Windows-only.** The UI (`KtxViewer.UI`) is built on **WPF**
(`net9.0-windows`, `UseWPF=true`), which only exists on Windows — there is no .NET
runtime that runs WPF on Linux or macOS. Attempting to publish for a non-Windows
runtime fails at build time:

```
error NETSDK1082: There was no runtime pack for Microsoft.WindowsDesktop.App.WPF
available for the specified RuntimeIdentifier 'linux-x64'.
```

The UI also relies on Windows-only APIs (Win32 P/Invoke for the custom window chrome,
`Microsoft.Win32` dialogs, `System.Windows.Media.Imaging`). For these reasons there is
**no `.deb` or `.dmg` export** and none can be produced from the WPF UI as-is.

> Porting note: the `Core`, `Application` and `Infrastructure` projects are all plain
> `net9.0` and platform-agnostic, so a future cross-platform build is feasible by
> replacing only the UI layer (e.g. with [Avalonia UI](https://avaloniaui.net), which is
> WPF-like and supports `.deb`/`.dmg` packaging) and shipping the native libktx binary
> per-OS (`ktx.dll` / `libktx.so` / `libktx.dylib`). This is a separate effort and is not
> part of the current build.

Distribution on Windows is via the [NSIS installer](#building-the-installer-nsis).

## Setup

### 1. Install .NET 9

Download from: https://dotnet.microsoft.com/download/dotnet/9.0

### 2. Download libktx.dll

#### Option A: Official Release (Recommended)

1. Go to [KTX-Software Releases](https://github.com/KhronosGroup/KTX-Software/releases)
2. Download the latest Windows release package (e.g., `KTX-Software-4.x.x-Windows-x64.zip`)
3. Extract `bin/ktx.dll`
4. Copy `ktx.dll` to one of:
   - `KtxViewer.UI/bin/Debug/net9.0-windows/` (development)
   - `KtxViewer.UI/bin/Release/net9.0-windows/` (release)
   - Or system PATH location

#### Option B: Build from Source

```bash
git clone https://github.com/KhronosGroup/KTX-Software.git
cd KTX-Software
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
# Copy build/Release/ktx.dll to viewer bin directory
```

### 3. Build & Run

```bash
cd ktx2-viewer
dotnet restore
dotnet build
dotnet run --project KtxViewer.UI/KtxViewer.UI/KtxViewer.UI.csproj
```

### 4. File Associations (Optional)

To open `.ktx` and `.ktx2` files by double-clicking in Windows:

1. Build in Release mode: `dotnet build -c Release`
2. Run PowerShell as Administrator
3. Execute: `.\register-file-associations.ps1`

See [FILE_ASSOCIATIONS.md](FILE_ASSOCIATIONS.md) for details.

## Building the Installer (NSIS)

A Windows installer can be produced with [NSIS](https://nsis.sourceforge.io). It deploys
the app to `Program Files`, creates Start menu / desktop shortcuts, and **associates
`.ktx` and `.ktx2` files with the application** so Windows opens them automatically.

### Prerequisites
- [NSIS 3.x](https://nsis.sourceforge.io/Download) (`makensis.exe` is auto-detected from
  `Program Files\NSIS`, or add it to `PATH`).

### Build

```powershell
.\build-installer.ps1 -Version 1.0.0
```

This publishes the app (Release, win-x64, framework-dependent), bundles `ktx.dll`, and
runs NSIS. The installer is written to `dist\KtxViewerSetup-<version>.exe`.

Use `-SkipPublish` to reuse the existing `.\publish` output instead of re-publishing.

The NSIS script lives at [installer/KtxViewer.nsi](installer/KtxViewer.nsi). The file
association is an opt-out component in the installer UI; the uninstaller removes it and
refreshes the shell automatically.

### Automated builds (GitHub Actions)

Every push to `main`/`master` builds the installer via
[`.github/workflows/build-installer.yml`](.github/workflows/build-installer.yml). The
runner installs the .NET 9 SDK, NSIS and libktx, then runs `build-installer.ps1`. Download
the resulting `KtxViewerSetup-*.exe` from the **Artifacts** section of the workflow run.

To cut a public release, push a tag like `v1.2.3` — the workflow additionally publishes a
**GitHub Release** with the installer attached for end users to download.

## Usage

### Open from GUI
- Click "Open File" button
- Select a `.ktx` or `.ktx2` file

### Open from Command Line
```bash
KtxViewer.UI.exe path\to\texture.ktx2
```

### Open from Windows Explorer
After registering file associations:
- Double-click any `.ktx` or `.ktx2` file
- Or right-click → "Open with" → "KTX Viewer"

## Supported Formats

### ✅ Fully Supported
- **BasisU ETC1S** (vkFormat=0, supercompressionScheme=2) → RGBA8 via libktx
- **RGBA8** (vkFormat=37) → Direct display
- **RGB8** (vkFormat=29) → Converted to RGBA8

### 🚧 Placeholder Only
- BC1, BC3, BC7 (shown as colored patterns)
- Other compressed formats

## Architecture

```
KtxViewer.Core          → Domain models (KtxImage, TextureFormat)
KtxViewer.Application   → Use cases (LoadImageUseCase)
KtxViewer.Infrastructure→ KTX2 loader, libktx P/Invoke
KtxViewer.UI            → WPF MVVM UI, Liquid Glass design
KtxViewer.Tests         → Unit & integration tests
```

## How It Works

### BasisU ETC1S Transcoding Flow

1. **Load KTX2 file** → Read header (80 bytes)
2. **Check vkFormat**:
   - `vkFormat == 0` → BasisU supercompression detected
3. **Load entire file** → libktx requires full KTX2 data
4. **Create ktxTexture2** via `ktxTexture2_CreateFromMemory`
5. **Transcode** via `ktxTexture2_TranscodeBasis` → RGBA32
6. **Display** → WPF BitmapSource

### Godot/libktx Reference

This viewer uses the same approach as Godot Engine:
- Godot: `modules/ktx/texture_loader_ktx.cpp` → `ktxTexture2_TranscodeBasis`
- Our viewer: `LibKtxTranscoder.cs` → P/Invoke to same API

See: [Godot KTX module](https://github.com/godotengine/godot/tree/main/modules/ktx)

## Tests

```bash
dotnet test
```

- **9 tests** (8 unit + 1 integration)
- Mocked IKtxLoader for Application layer
- Real file loading test with test.ktx2

## License

MIT

## Credits

- **libktx** by Khronos Group ([KTX-Software](https://github.com/KhronosGroup/KTX-Software))
- **Basis Universal** by Binomial LLC ([basis_universal](https://github.com/BinomialLLC/basis_universal))
- Liquid Glass design inspired by modern UI trends

# KTX Viewer

Simple GUI application for viewing KTX and KTX2 texture files. Built with .NET 9, C# 13, WPF, Clean Architecture, and Liquid Glass design.

## Features

- ✅ Load and display KTX and KTX2 texture files
- ✅ Automatic format detection (Strategy pattern)
- ✅ Support for BasisU/ETC1S supercompression (via libktx)
- ✅ Display detailed texture metadata
- ✅ MVVM architecture with CommunityToolkit.Mvvm
- ✅ Clean Architecture (Core → Application → Infrastructure → UI)
- ✅ Liquid Glass design system with transparency and blur effects
- ✅ Comprehensive unit and integration tests

## Requirements

- .NET 9 Runtime
- Windows 10/11
- **libktx.dll** (for BasisU ETC1S texture support)

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

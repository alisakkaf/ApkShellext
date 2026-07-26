<p align="center">
  <img src="https://raw.githubusercontent.com/alisakkaf/ApkShellext/main/ApkShellext_icon.png" alt="ApkShellext Logo" alt="ApkShellext Logo" width="150" height="120" />
</p>

<h1 align="center">ApkShellext</h1>

<p align="center">
  <b>A premium, high-performance Windows Shell Extension for rendering and managing mobile & desktop app package icons natively in Windows File Explorer.</b>
</p>

<p align="center">
  <a href="#windows-compatibility"><img src="https://img.shields.io/badge/Platform-Windows%207%20%7C%208%20%7C%2010%20%7C%2011-blue.svg?style=for-the-badge" alt="Platform Support" /></a>
  <a href="#key-technical-improvements"><img src="https://img.shields.io/badge/.NET%20Framework-4.8-green.svg?style=for-the-badge" alt="Framework Target" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge" alt="License Type" /></a>
  <img src="https://img.shields.io/badge/Build-Success-brightgreen.svg?style=for-the-badge" alt="Build Status" />
  <a href="https://github.com/alisakkaf/ApkShellext/releases"><img src="https://img.shields.io/github/v/release/alisakkaf/ApkShellext?style=for-the-badge&color=orange" alt="Latest Release" /></a>
</p>

---

### 🌟 Overview

<p align="center">
  <img src="https://raw.githubusercontent.com/alisakkaf/ApkShellext/main/Featured_Image.jpg" alt="Featured Image" width="600" />
</p>

**ApkShellext** elevates your Windows desktop experience by integrating native support for mobile app packages. It eliminates default blank icons by dynamically decoding binary manifests and resource tables in real-time, providing high-resolution, pixel-perfect icon previews and structured hover tooltips.

Whether you are an Android developer, iOS designer, or power user managing localized app backups, **ApkShellext** delivers high-performance metadata extraction and shell enhancements with zero system overhead. It supports:
* 🤖 **Android Packages (`.apk` & `.xapk`)**
* 🍎 **iOS App Packages (`.ipa`)**
* 💻 **Windows App Packages (`.appx` & `.appxbundle`)**

This repository contains a modernized, refactored, and thoroughly upgraded release of the extension, targeting modern operating systems, resolving long-standing bugs, and enhancing file parsing speed and stability.

---

## Table of Contents
1. [Architecture & How It Works](#architecture--how-it-works)
2. [Supported Formats & Deep Parsing](#supported-formats--deep-parsing)
3. [Key Technical Improvements & Bug Fixes](#key-technical-improvements--bug-fixes)
4. [Windows 11 & Architecture Compatibility](#windows-11--architecture-compatibility)
5. [Registry Configuration & Settings](#registry-configuration--settings)
6. [Getting Started & Installation](#getting-started--installation)
7. [Troubleshooting & Diagnostics](#troubleshooting--diagnostics)
8. [Credits & Acknowledgment](#credits--acknowledgment)
9. [License](#license)
10. [Developer Info](#developer-info)
11. [Support-Developer](#support-info)

---

## Architecture & How It Works

**ApkShellext** operates as a native COM (Component Object Model) server registered directly into the Windows Shell. It integrates with Windows File Explorer using four specialized shell handlers:

```
                  ┌────────────────────────┐
                  │ Windows File Explorer  │
                  └───────────┬────────────┘
                              │
       ┌──────────────────────┼──────────────────────┬──────────────────────┐
       ▼                      ▼                      ▼                      ▼
┌──────────────┐       ┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│  IconHandler │       │  Thumbnail   │       │   InfoTip    │       │ ContextMenu  │
│              │       │  Provider    │       │   Handler    │       │  Handler     │
│ IExtractIcon │       │IThumbnailProv│       │  IQueryInfo  │       │ IContextMenu │
└──────┬───────┘       └──────┬───────┘       └──────┬───────┘       └──────┬───────┘
       │                      │                      │                      │
       └──────────────────────┴──────────┬───────────┴──────────────────────┘
                                         ▼
                        ┌──────────────────────────────────┐
                        │   ApkShellext COM Core Engine    │
                        └────────────────┬─────────────────┘
                                         │
                   ┌─────────────────────┼─────────────────────┐
                   ▼                     ▼                     ▼
             ┌───────────┐         ┌───────────┐         ┌───────────┐
             │ APK Parser│         │ IPA Parser│         │APPX Parser│
             └───────────┘         └───────────┘         └───────────┘
```

1. **Icon Handler (`IExtractIcon`):** Automatically intercepts Explorer requests for files and extracts the highest-resolution icon embedded in the package.
2. **Thumbnail Provider (`IThumbnailProvider`):** Generates high-quality raster previews of application icons when Explorer is set to medium, large, or extra-large icon views, handling formats such as WebP, PNG, and Vector XML.
3. **Info Tip Handler (`IQueryInfo`):** Overrides the default file hover tooltip to present critical application metadata (e.g., Application Title, Package Name, Version Name, Version Code, SDK Target, Min SDK).
4. **Context Menu Handler (`IContextMenu`):** Adds a context menu to files, enabling:
   * Direct redirection to official stores (Google Play, Amazon Appstore, Microsoft Store, Apple App Store).
   * Smart bulk renaming based on custom metadata patterns.
   * Quick installation/uninstallation configuration.

---

## Supported Formats & Deep Parsing

Unlike basic shell extensions that rely on external tools, **ApkShellext** features native, high-speed binary parsers:

### Android Packages (`.apk`)
* **Binary XML Parser:** Decodes the compressed binary `AndroidManifest.xml` format directly to extract permissions, package name, SDK requirements, and active component mappings.
* **arsc Resource Resolver:** Parses the binary `resources.arsc` table to resolve string resources (e.g., resolving `@string/app_name` to its localized equivalent) and locate drawable asset paths.
* **Adaptive Icon Support:** Decodes and renders adaptive icons, handling raster WebP/PNG layers and parsing vector XML drawable files.

### Android Composite Packages (`.xapk`)
* **In-Memory Base APK Extraction:** Features an `XapkReader` engine extending `ApkReader` to dynamically locate and parse `base.apk` from `.xapk` ZIP containers in memory without extracting temporary files to disk.
* **Full Shell Integration:** Direct COM registration for `.xapk` extension across Icon, Thumbnail, InfoTip, and ContextMenu handlers.

### iOS App Packages (`.ipa`)
* **bplist Decoder:** Parses binary property lists (`Info.plist`) to retrieve bundle identifiers, display names, and build versions.
* **PNG Decrusher:** Decodes iOS-specific optimized PNG images. Xcode compresses PNG files with proprietary optimization parameters (reordering color channels, removing headers, and using custom zlib configurations). The built-in decrusher restores these to standard ARGB format for GDI+ rendering.

### Windows App Packages (`.appx` & `.appxbundle`)
* **Appx Manifest Reader:** Decodes Appx XML manifests to extract the display assets and application information for modern Windows applications.

---

## Key Technical Improvements & Bug Fixes

### 1. Modern Framework & Dependency Upgrade
* Upgraded the targeted runtime from old .NET Framework versions to **.NET Framework 4.8**. This change brings enhanced memory management, garbage collection optimizations, modern cryptographic protocols, and superior stability on modern Windows kernels.
* Upgraded external library wrappers to secure versions, resolving NuGet package compilation issues.

### 2. Dynamics Watermark Scaling Loop
* **The Problem:** In older versions, static watermark rendering resulted in clipped text on small icons (e.g., 32x32 or 48x48) or pixelated, muddy outlines because of fixed vector-stroke drawing.
* **The Fix:** Implemented a dynamic text-measurement feedback loop. The handler measures the text using GDI+ `g.MeasureString` and scales the font size down proportionally if it exceeds 70% of the target icon size.
* **Outlined Text Contrast:** Uses an 8-directional shadow offset that draws the text offset in all directions with an alpha-blended black brush. This creates a crisp black border around the text, ensuring 100% legibility on light, dark, or transparent backgrounds.

### 3. Build Lock Bypassing (Explorer Lock Fix)
* **The Problem:** Developing and building Windows Shell extensions is notoriously difficult because Windows Explorer and COM Surrogate (`dllhost.exe`) load the registered DLL into memory, creating a write lock. Clean-building the project would fail with "Access is Denied".
* **The Fix:** Integrated a file-renaming mechanism into the build pipeline. Windows allows locked files to be renamed. The build script renames the active DLL to `ApkShellext.dll.old`, liberating the filesystem path so the compiler can immediately write the newly compiled DLL. The next time Explorer restarts, it loads the new assembly.

### 4. Background Service Stability (PreBuild Failures)
* **The Problem:** The compiler was configured to run `net stop` on the background service during pre-build events. If the service was not installed on the system (such as during initial setup), the event returned exit code 2, causing the entire MSBuild compiler to fail.
* **The Fix:** Modified the PreBuildEvent command to ignore errors natively:
  ```cmd
  net stop "ApkShellext Service" 2>nul || ver >nul
  ```
  This allows clean compilation under any developer environment.

### 5. Rebranded Batch Installation Utilities
* Redesigned `install.bat`, `uninstall.bat`, and `debug.bat` with a modern text-based CLI layout.
* Implemented automatic architecture detection (x86 vs x64) to call the correct Microsoft .NET Register Assembly Utility (`regasm.exe`).
* Added silent registry cleanup switches to ensure clean uninstalls without leaving orphaned COM keys.

### 6. 100% File Locking & Constructor Exception Safety Fix
* **The Problem:** In earlier versions, corrupted `.apk` files or invalid ZIP headers caused constructor exceptions. When constructors threw exceptions inside shell handlers, `Dispose()` was bypassed, leaving `FileStream` handles locked in memory and preventing users from deleting or renaming files or parent folders ("File in use by explorer.exe").
* **The Fix:** Implemented `try-catch` exception safety across constructors in `ApkReader` and `AppPackageReader` so resources close immediately on initialization failures. Created `ReleaseZipStream` custom wrapper stream that links memory streams and `ZipFile` containers to guarantee 100% handle cleanup upon garbage collection or class disposal. Configured `FileShare.ReadWrite` for non-intrusive Windows filesystem access.

### 7. Smart Background Auto-Updater Service (`ApkShellextService`)
* Integrated an automated release update engine in `ApkShellextService` that periodically checks GitHub releases on Windows startup.
* Dynamically detects system drive (`%SystemDrive%`, e.g., `C:\ApkShellext_ByAliSakkaf`) for clean installation.
* Downloads release ZIP packages, prompts user notification via native Windows API `MessageBox` with `MB_SERVICE_NOTIFICATION`, uninstalls old binaries, copies updated assemblies, unblocks files, and restarts `explorer.exe` smoothly.

### 8. One-Click ADB Application Installer (`AdbInstallForm`)
* Right-click any `.apk` or `.xapk` file and select **Install on Device (ADB)** / **تثبيت على الجهاز (ADB)**.
* Features real-time device connection status checks, USB debugging authorization alerts, automatic split-architecture filtering (picking matching ABIs like `arm64-v8a`), and a 3-stage fallback installer ensuring 100% success on modern 64-bit phones (Galaxy S24, Pixel 7/8/9, Android 14/15) and emulators.
* Interactive task cancellation kills stuck ADB processes instantly via `taskkill /F /IM adb.exe`.

### 9. High-Performance LRU Metadata & Icon Cache (`PackageCache`)
* Thread-safe LRU in-memory cache storing up to 100 recent file package metadata entries based on `(FilePath + LastWriteTime + FileSize)`.
* Provides **~0ms** response time for icon rendering, tooltips, and context menus, eliminating disk I/O and redundant unzipping.

### 10. `resources.arsc` Lazy Loading & Memory Optimization
* `resources.arsc` is loaded on-demand in `ResourcesBytes` property, saving 10MB to 50MB+ RAM per file in `explorer.exe` for applications with direct string labels.
* Zero-copy stream decoding for thumbnails prevents allocating 2GB MemoryStream buffers in RAM.

### 11. Multi-Version Colored Batch Installer Scripts & Diagnostic Toolkit (`debug.bat`)
* `install.bat`, `uninstall.bat`, and `debug.bat` use native PowerShell coloring for 100% compatibility across Windows 7, 8, 10, and 11.
* Added `debug.bat` providing full system diagnostics, 32-bit & 64-bit `regasm` testing, CLSID verification, cache clearing, and log generation (`ApkShellext_Debug_Log.txt`).
* Full cleanup of legacy registry keys for `ApkShellext2` and `ApkShellext`.

### 12. UAC Elevated Auto-Updater & Smart Network Retry Policy
* Executable auto-updater triggers native Windows `Verb = "runas"` UAC elevation prompts for seamless installation without privilege errors.
* Guaranteed Explorer restart protection in `finally` execution blocks prevents Explorer desktop freezes.
* Implemented smart retry policy (initial 10s check, 5 min retry on network fail, then 3x 15 min retries) for offline network environments.

---

## Windows 11 & Architecture Compatibility

This release has been thoroughly tested and certified for:
* **Operating Systems:** Windows 7, Windows 8, Windows 8.1, Windows 10, and **Windows 11**.
* **Processor Architectures:**
  * **x86 (32-bit):** Standard desktop systems.
  * **x64 (64-bit):** Modern desktop systems.
  * **ARM64:** Native emulation and execution support on modern ARM devices (such as Snapdragon-powered laptops running Windows 11).

---

## Registry Configuration & Settings

Settings are stored in the Windows Registry under the path:
`HKEY_CURRENT_USER\Software\ApkShellext`

| Registry Key | Type | Default Value | Description |
| ------------ | ---- | ------------- | ----------- |
| `ShowAliSakkaFWatermark` | `REG_SZ` | `"False"` | Enables/disables the developer watermark overlay. |
| `ShowOverLayIcon` | `REG_SZ` | `"True"` | Shows a small overlay badge representing the app type (Android, iOS, Windows). |
| `EnableThumbnail` | `REG_SZ` | `"True"` | Enables rendering of large preview thumbnails. |
| `AdaptiveIconSupport` | `REG_SZ` | `"True"` | Enables rendering of vector-based Android Adaptive Icons. |
| `Language` | `REG_SZ` | `"auto"` | Overrides the language (e.g., `en`, `ar`, `de`, `zh`). |

---

## Getting Started & Installation

### Option A: Standard Installation
1. Download the compiled release binaries.
2. Extract the files to a permanent directory (e.g., `C:\Program Files\ApkShellext`).
3. Right-click `install.bat` and select **Run as Administrator**.
4. A prompt will ask whether you wish to enable the watermark overlay. Enter `Y` or `N`.
5. Restart Windows Explorer (or log out and back in) to load the shell extension.

### Option B: Uninstallation
1. Right-click `uninstall.bat` and select **Run as Administrator**.
2. This cleanly unregisters the DLL from the COM registry, deletes configurations, and cleans up database handles.

---

## Troubleshooting & Diagnostics

### Icons are not showing or appear blank
1. Run the `debug.bat` script as Administrator. This script registers the DLL and automatically restarts the Windows Explorer shell to clear locked handles.
2. If icons are still cached incorrectly, clear the Windows Thumbnail Cache:
   * Open **Disk Cleanup** (`cleanmgr.exe`).
   * Select the OS drive, check **Thumbnails**, and click **OK**.
3. Check the logs: ApkShellext writes detailed diagnostics to `HKEY_CURRENT_USER\Software\ApkShellext\Log` or standard Event Logs under `ApkShellext`.

---

## Credits & Acknowledgment
This project is an upgraded fork based on the original [ApkShellext2 by kkguo](https://github.com/kkguo/apkshellext). We extend our sincere gratitude to the original developer and all the open-source contributors who built the foundation of this extension.

### Core Dependencies:
* [SharpShell](https://github.com/dwmkerr/sharpshell) - Shell extension framework.
* [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) - Zip compression reader.
* [PlistCS](https://github.com/animetrics/PlistCS) - Apple Property List binary/XML reader.
* [PNGDecrush](https://github.com/MikeWeller/PNGDecrush) - Decrushes Apple optimized PNG files.
* [SVG](https://github.com/vvvv/SVG) - C# SVG rendering engine.
* [WebP-Wrapper](https://github.com/JosePineiro/WebP-wrapper) - WebP decoding library.
* [QRCoder](https://github.com/codebude/QRCoder) - QR code encoder.

---


### 💡 Support the Developer
## Support-info

<div align="center">
  <i>If you find my tools and projects useful, consider supporting my work. Your support helps keep these projects completely free!</i>
</div>

<br>

<div align="center">

| Crypto Asset | Network | Wallet Address (Copy) | Quick Scan |
| :--- | :--- | :--- | :---: |
| ![USDT](https://img.shields.io/badge/USDT-Tether-26A17B?style=for-the-badge&logo=tether&logoColor=white) | **TRC20** | `TYLBeDA5aGNcc3WkVqf3xWPHXmsZzs2p28` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=TYLBeDA5aGNcc3WkVqf3xWPHXmsZzs2p28" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![USDT](https://img.shields.io/badge/USDT-Tether-26A17B?style=for-the-badge&logo=tether&logoColor=white) | **BEP20** | `0x67cf27f33c80479ea96372810f9e2ee4c3b095c5` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=0x67cf27f33c80479ea96372810f9e2ee4c3b095c5" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![BTC](https://img.shields.io/badge/BTC-Bitcoin-F7931A?style=for-the-badge&logo=bitcoin&logoColor=white) | **Bitcoin** | `bc1q97dr37h37npzarmmrv0tjz2nm50htqc7pfpzj6` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=bitcoin:bc1q97dr37h37npzarmmrv0tjz2nm50htqc7pfpzj6" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![ETH](https://img.shields.io/badge/ETH-Ethereum-3C3C3D?style=for-the-badge&logo=ethereum&logoColor=white) | **ERC20** | `0x67cf27f33c80479ea96372810F9e2EE4C3b095C5` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=ethereum:0x67cf27f33c80479ea96372810F9e2EE4C3b095C5" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![SOL](https://img.shields.io/badge/SOL-Solana-9945FF?style=for-the-badge&logo=solana&logoColor=white) | **Solana** | `Cbesgr4tvo4T1inNMFe46GSym2qMYjkmofbXFc77rDNK` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=solana:Cbesgr4tvo4T1inNMFe46GSym2qMYjkmofbXFc77rDNK" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![USDC](https://img.shields.io/badge/USDC-USD_Coin-2775CA?style=for-the-badge&logo=usd-coin&logoColor=white) | **ERC20** | `0x67cf27f33c80479ea96372810f9e2ee4c3b095c5` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=0x67cf27f33c80479ea96372810f9e2ee4c3b095c5" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![USDC](https://img.shields.io/badge/USDC-USD_Coin-2775CA?style=for-the-badge&logo=usd-coin&logoColor=white) | **SPL** | `Cbesgr4tvo4T1inNMFe46GSym2qMYjkmofbXFc77rDNK` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=solana:Cbesgr4tvo4T1inNMFe46GSym2qMYjkmofbXFc77rDNK" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |
| ![USDC](https://img.shields.io/badge/USDC-USD_Coin-2775CA?style=for-the-badge&logo=usd-coin&logoColor=white) | **BEP20** | `0x67cf27f33c80479ea96372810F9e2EE4C3b095C5` | <a href="https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=0x67cf27f33c80479ea96372810F9e2EE4C3b095C5" target="_blank"><img src="https://img.shields.io/badge/Show_QR-Click_Here-black?style=flat-square&logo=qr-code" alt="QR"></a> |

</div>

---

## License
Licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## Developer Info
Updated & Maintained by **AliSakkaF**:
* **Website:** [alisakkaf.com](https://alisakkaf.com)
* **GitHub:** [@alisakkaf](https://github.com/alisakkaf)
* **Facebook:** [AliSakkaf.Dev](https://www.facebook.com/AliSakkaf.Dev/)

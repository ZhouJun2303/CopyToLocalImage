# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run

```bash
cd CopyToLocalImage
dotnet restore
dotnet build --configuration Release
dotnet run
```

## Packaging

Run `build.bat` from the repository root to create a distributable package. Output will be in `build_output/` directory.

## Architecture Overview

This is a WPF-based Windows desktop application that monitors the system clipboard for images and automatically saves them.

### Core Services (CopyToLocalImage/Services/)

- **ClipboardMonitor**: Uses Windows API (`AddClipboardFormatListener`) to listen for clipboard changes via a hidden message window
- **ImageService**: Saves clipboard images as PNG format and generates thumbnails (200x200)
- **StorageService**: Manages image metadata using JSON storage (`metadata.json`), handles CRUD operations and bulk deletion
- **HotkeyService**: Registers global hotkeys using Windows API (`RegisterHotKey`)
- **TrayIconService**: System tray integration using `NotifyIcon` with context menu
- **LogService**: Static logger writing to `%LOCALAPPDATA%\CopyToLocalImage\logs\`

### Models (CopyToLocalImage/Models/)

- **AppSettings**: Application configuration (save path, theme, hotkey, tray behavior) loaded from `%LOCALAPPDATA%\CopyToLocalImage\settings.json`
- **ImageItem**: Image metadata (file path, thumbnail path, dimensions, file size, creation time)

### Key Patterns

- Services use locking (`lock (_lock)`) for thread-safe operations
- Async operations for file I/O and image loading
- Event-driven architecture: `ClipboardMonitor` triggers callbacks on clipboard changes
- WPF data binding with `ObservableCollection<ImageItem>` for UI updates

### Data Flow

1. `ClipboardMonitor` detects image in clipboard → calls `OnClipboardChanged` callback
2. `App` (in `App.xaml.cs`) handles callback → `ImageService.SaveImageFromClipboard()`
3. Saved image path passed to `StorageService.AddImageRecord()` → updates metadata.json
4. `MainWindow.AddImage()` updates UI via ObservableCollection

### File Structure

- Images saved to: `{SavePath}/{yyyy-MM-dd}/clipboard_{timestamp}_{guid}.png`
- Thumbnails saved to: `{SavePath}/_thumbnails/{yyyy-MM-dd}/{filename}.thumb.png`
- Default save path: `%USERPROFILE%/Pictures/ClipboardImages`

### Single Instance

The application uses a `Mutex` named `CopyToLocalImage_SingleInstance` to ensure only one instance runs at a time.

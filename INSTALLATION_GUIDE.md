# Kids Coloring Book - Installation & Setup Guide

## 1. System Requirements

- **Operating System:** Windows 10 (version 1809 or higher) or Windows 11 (64-bit)
- **Framework:** .NET 10 LTS Windows Desktop Runtime (`Microsoft.WindowsDesktop.App 10.0.x`)
- **RAM:** Minimum 2 GB (4 GB recommended for optimal performance)
- **Disk Space:** ~20 MB of free storage space (ultra-lightweight)
- **Display:** 1024x768 or higher (supports Full-HD, 4K, Touchscreens, and Full-Screen mode)
- **Network:** None required (100% offline, zero internet access needed)

---

## 2. Fast Installation (Recommended)

### Method A: One-Click Installer (Desktop & Start Menu Shortcuts)
1. Extract `KidsColoringBook-v1.0.0-Windows.zip` to any folder on your computer.
2. Double-click **`Install.bat`** (or right-click `Install.ps1` and select *Run with PowerShell*).
3. The installer will:
   - Copy application files to `%LOCALAPPDATA%\Programs\KidsColoringBook`.
   - Create a **Desktop shortcut** named `Kids Coloring Book`.
   - Create a **Start Menu shortcut** under `Programs > Kids Coloring Book`.
   - Register the application in Windows **Add or Remove Programs** (Installed Apps).
4. Launch the game directly from your desktop or start menu!

---

### Method B: Portable Mode (Zero Installation)
If you prefer not to install anything into the system:
1. Open the extracted folder or `e:\Colours\Publish\`.
2. Double-click **`KidsColoringBook.exe`** to launch immediately.
3. You can place the folder on a USB flash drive and run it offline on any compatible Windows PC!

---

## 3. How to Uninstall

### Through Windows Settings:
1. Open Windows **Settings > Apps > Installed apps** (or *Add or remove programs*).
2. Find **Kids Coloring Book** in the list.
3. Click the three dots `...` and choose **Uninstall**.

### Through Application Folder:
1. Navigate to `%LOCALAPPDATA%\Programs\KidsColoringBook`.
2. Double-click **`Uninstall.bat`**.
3. All shortcuts, application binaries, and registry entries will be cleanly removed.

---

## 4. Troubleshooting & FAQ

### Q: The application says ".NET Desktop Runtime is required".
- Download and install the free official Microsoft .NET 10 Windows Desktop Runtime x64 from Microsoft:
  `https://dotnet.microsoft.com/download/dotnet/10.0`

### Q: Does this app collect any personal information or telemetry?
- **No.** The application is strictly 100% offline. It has zero network calls, zero analytics, zero advertisements, and stores all drawings only on the local machine under `%LOCALAPPDATA%\KidsColoringBook\`.

# Running MAUI Android App with Aspire Services

## Quick Start: Two-Terminal Setup

### Terminal 1: Start Aspire AppHost (Backend Services)

```powershell
cd D:\dev\github\fboucher\NoteBookmark
dotnet run --project src\NoteBookmark.AppHost\NoteBookmark.AppHost.csproj
```

**What this does:**
- Starts the **NoteBookmark API** on `https://localhost:7198`
- Starts **Keycloak** authentication server on `http://localhost:8080`
- Starts **Azure Storage Emulator**
- Starts **Blazor Web App**
- Orchestrates all services via Docker Compose

**Keep this terminal running!** Don't close it while testing the MAUI app.

The terminal will show something like:
```
Dashboard: http://localhost:18888
API Endpoint: https://localhost:7198
Keycloak: http://localhost:8080
```

---

### Terminal 2: Start MAUI Android App

Once Aspire is running, open a **second terminal** and run:

```powershell
cd D:\dev\github\fboucher\NoteBookmark
dotnet run --project src\NoteBookmark.MauiApp\NoteBookmark.MauiApp.csproj -c Debug -f net10.0-android
```

**What this does:**
- Builds the MAUI app for Android
- Deploys to the Android emulator
- Uses the configuration from `appsettings.Development.json`
- Connects to the backend API via `https://10.0.2.2:7198` (Android's way of referencing host machine)

---

## Why Two Terminals?

The MAUI app needs to connect to live backend services:
- ✅ **With AppHost running:** The app can fetch/sync data from the API
- ❌ **Without AppHost:** The app will crash trying to connect to missing services

---

## Configuration Details

### Android Emulator Network Magic

From an Android emulator, you cannot use `localhost` or `127.0.0.1`. Instead:
- **Host machine services** are accessible at `10.0.2.2`
- The MAUI app uses `https://10.0.2.2:7198` for the API

This is configured in:
```json
// src/NoteBookmark.MauiApp/wwwroot/appsettings.Development.json
{
  "ApiBaseUrl": "https://10.0.2.2:7198",
  "Logging": {
	"LogLevel": {
	  "Default": "Debug"
	}
  }
}
```

### Blazor Render Mode Fix

**Previously:** Posts.razor used `@rendermode InteractiveServer` (web-only)
**Now:** Posts.razor uses `@rendermode InteractiveAuto` (works on web + MAUI Hybrid)

This allows the component to render correctly on both:
- ✅ Blazor Web App (ASP.NET Core)
- ✅ MAUI Blazor Hybrid (Android/iOS/Windows/macOS)

---

## Building Separately

If you prefer to run them in the **same terminal** (wait for services):

```powershell
# Build and run AppHost in background
$appHostProcess = Start-Process pwsh -ArgumentList "-NoExit", "-Command", `
  "cd D:\dev\github\fboucher\NoteBookmark; dotnet run --project src\NoteBookmark.AppHost\NoteBookmark.AppHost.csproj" `
  -PassThru

# Wait 10 seconds for services to start
Start-Sleep 10

# Run MAUI app
dotnet run --project src\NoteBookmark.MauiApp\NoteBookmark.MauiApp.csproj -c Debug -f net10.0-android

# Cleanup when done
Stop-Process -Id $appHostProcess.Id
```

---

## Debugging Tips

### View Logs from Android
- Open **Android Studio → Logcat** or **Android Device Monitor**
- Filter by `NoteBookmark` to see app-specific logs
- Search for "Cannot supply a component" errors if crashes occur

### Check API Connectivity
In MAUI app, verify:
```csharp
// MauiProgram.cs shows the API URL being used:
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7198";
// For Android, should be: https://10.0.2.2:7198
```

### View Aspire Dashboard
While AppHost is running, open http://localhost:18888 to see:
- All running services
- Logs from each service
- Health status
- Endpoint URLs

---

## Troubleshooting

### "Connection refused" or network errors
- ✅ **Check:** Is AppHost running? (Terminal 1 still active?)
- ✅ **Check:** Is the API URL in appsettings.Development.json correct?
- ✅ **Check:** Are you running on Android emulator (not Windows)?

### "Cannot supply component of type 'Posts'"
- ✅ **Fixed:** Changed render mode from `InteractiveServer` → `InteractiveAuto`
- ✅ **Rebuild:** `dotnet build src\NoteBookmark.SharedUI\NoteBookmark.SharedUI.csproj`

### App builds but won't deploy to emulator
- ✅ **Check:** Is Android emulator running? (Open Android Studio)
- ✅ **Check:** `adb devices` shows emulator
- ✅ **Retry:** `dotnet run --project ... -f net10.0-android --no-build`

---

## IDE Integration (Optional)

### Visual Studio Compound Run Configuration
Create a `.vs/launch.settings.json` to start both in one click:
```json
{
  "profiles": [
	{
	  "name": "AppHost + MAUI Android",
	  "commands": [
		{
		  "name": "AppHost",
		  "project": "src/NoteBookmark.AppHost/NoteBookmark.AppHost.csproj"
		},
		{
		  "name": "MAUI Android",
		  "project": "src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj",
		  "args": "-f net10.0-android"
		}
	  ]
	}
  ]
}
```

---

**You're all set!** 🚀 Both services should now work together.

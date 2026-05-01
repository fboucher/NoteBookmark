# Run The .NET MAUI Android App

This guide explains how to run the Android version of the .NET MAUI app and how to configure its local settings.

## 1. What This Project Supports

The MAUI project is at `src/NoteBookmark.MauiApp`.

On Linux, this project is configured to build the Android target only.

## 2. Prerequisites

Install the following first:

- .NET 10 SDK
- .NET MAUI Android workload (`maui-android`)
- .NET WebAssembly tools workload (`wasm-tools`) — also required by the MAUI project
- Android SDK and emulator or a connected Android device

If the workloads are not installed yet, run with `sudo` because the dotnet SDK is installed system-wide:

```bash
sudo dotnet workload install maui-android wasm-tools
```

If Android API dependencies are missing, run this once:

```bash
dotnet build src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj \
  -t:InstallAndroidDependencies \
  -f net10.0-android \
  -p:AndroidSdkDirectory=/home/frank/Android/Sdk \
  -p:AcceptAndroidSDKLicenses=True
```

## 3. Configure Keycloak For Mobile

The MAUI app reads only these keys from the `Keycloak` section:

- `Authority`
- `ClientId`
- `RedirectUri`

The code does not read or send a client secret.

That is intentional: the Android app uses Authorization Code Flow with PKCE as a public client. For Keycloak, this means:

- Create or use a public OIDC client for the mobile app
- Disable client authentication for that mobile client
- Enable standard flow
- Set the redirect URI to `notebookmark://auth/callback`

Do not reuse the confidential web client if that client requires a secret. The safer setup is a separate mobile client such as `notebookmark-mobile`.

If you still need the web setup, keep using the confidential client described in `docs/keycloak-setup.md` for the web app, and use a separate public client for MAUI.

## 4. Configure The App

The committed base config is:

- `src/NoteBookmark.MauiApp/wwwroot/appsettings.json`

For local development overrides, create this file:

- `src/NoteBookmark.MauiApp/wwwroot/appsettings.Development.json`

In `DEBUG` builds, the app now loads `appsettings.Development.json` if it exists. That file is gitignored, so it can contain your local Android settings without being committed.

Example:

```json
{
  "Keycloak": {
    "Authority": "https://keycloak.example.com/realms/notebookmark",
    "ClientId": "notebookmark-mobile",
    "RedirectUri": "notebookmark://auth/callback"
  }
}
```

Notes:

- The `RedirectUri` must match the callback registered in Keycloak
- The same callback is already declared in `Platforms/Android/AndroidManifest.xml`
- If Keycloak or the API is running on your development machine, Android emulator access usually needs `10.0.2.2` instead of `localhost`

## 5. Start Required Services

Before launching the app, make sure the services it depends on are reachable from the emulator or device:

- Keycloak
- NoteBookmark backend services you want to use

If you are using the existing local container setup, start from these docs:

- `docs/keycloak-container-setup.md`
- `docs/docker-compose-deployment.md`

## 6. Start An Emulator Or Connect A Device

If you use a physical device, connect it with USB, enable developer options + USB debugging, and authorize your machine when prompted.

If you use an emulator on Linux, run these commands:

```bash
/home/frank/Android/Sdk/emulator/emulator -list-avds
```

If needed, start the known profile from this repo:

```bash
/home/frank/Android/Sdk/emulator/emulator -avd NB_Android_35
```

If `emulator` is not found, either use the full path above or add it to PATH:

```bash
export ANDROID_SDK_ROOT=/home/frank/Android/Sdk
export PATH=$ANDROID_SDK_ROOT/platform-tools:$ANDROID_SDK_ROOT/emulator:$PATH
```

Before running MAUI, verify that Android sees a device:

```bash
adb devices -l
```

You should see at least one entry like `emulator-5554 device ...`.

## 7. Run The MAUI App

From the repository root, run:

```bash
dotnet build src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj \
  -t:Run \
  -f net10.0-android \
  -v minimal
```

This builds the Android target and launches it on the active emulator or device.

Optional: if you have multiple devices connected, list them first:

```bash
adb devices
```

Then target one device explicitly:

```bash
dotnet build src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj \
  -t:Run \
  -f net10.0-android \
  -p:AndroidDeviceUserId=emulator-5554 \
  -v minimal
```

## 8. Troubleshooting

If run fails with `error XA0010: No available device`:

- No emulator/device is connected yet
- Start an emulator (section 6) and wait until it fully boots
- Re-run `adb devices -l` and confirm at least one `device` state (not `offline`)
- Re-run the MAUI run command

If `emulator: command not found`:

- Use `/home/frank/Android/Sdk/emulator/emulator` directly
- Or add Android SDK emulator and platform-tools folders to PATH (section 6)

If sign-in fails with `invalid_client` or a similar token error:

- The configured Keycloak client is probably confidential
- Switch the MAUI app to a public client with no secret

If the browser returns from Keycloak but the app does not complete sign-in:

- Verify the redirect URI is exactly `notebookmark://auth/callback`
- Verify the Keycloak client allows that redirect URI

If your local override file is ignored at runtime:

- Make sure the file name is exactly `appsettings.Development.json`
- Run a `Debug` build
- Rebuild the app after creating or changing the file

If the build breaks after a .NET SDK update with errors like `NETSDK1147` (missing workload) or `MSB4019` (missing `.targets` file):

- A SDK update can require new or updated workload manifests that need `sudo` to install
- Run the following two commands to restore and repair:

```bash
sudo dotnet workload restore src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj
sudo dotnet workload repair
```

- The `wasm-tools` workload must be installed in addition to `maui-android` (see Prerequisites)
- If errors persist about missing `.targets` files in the Android SDK pack, `dotnet workload repair` will reinstall the pack and restore the missing files
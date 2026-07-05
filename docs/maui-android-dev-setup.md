# MAUI Android Development Environment Setup

This guide covers setting up a local development environment for the NoteBookmark MAUI Android app.

**Platform:** Debian 13 

## Step 1 — Install JDK 21

```bash
sudo apt install openjdk-21-jdk
```

Add `JAVA_HOME` to your shell config (`~/.zshrc` or `~/.bashrc`):

```bash
echo 'export JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64' >> ~/.zshrc
source ~/.zshrc
```

Verify:

```bash
java -version
```

## Step 2 — Install .NET MAUI Android workload

.NET 9 or 10 must already be installed. Then:

```bash
dotnet workload install maui-android
```

This downloads ~2 GB and takes a few minutes.

Verify:

```bash
dotnet workload list
```

You should see `maui-android` in the output.


## Step 3 — Install Android command-line tools

1. Download the **Command line tools only** package from:
   https://developer.android.com/studio#command-tools
   *(look for "Command line tools only" at the bottom of the page)*

2. Extract and place them in the expected location:

```bash
mkdir -p ~/Android/Sdk/cmdline-tools
unzip ~/Downloads/commandlinetools-linux-*_latest.zip -d ~/Android/Sdk/cmdline-tools
mv ~/Android/Sdk/cmdline-tools/cmdline-tools ~/Android/Sdk/cmdline-tools/latest
```

3. Add `ANDROID_HOME` to your shell config:

```bash
cat >> ~/.zshrc << 'EOF'

# Android SDK
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$PATH
EOF
source ~/.zshrc
```

Verify:

```bash
sdkmanager --version
```


## Step 4 — Install SDK packages

Accept all licenses first:

```bash
sdkmanager --licenses
```

Then install the required packages (downloads ~2–3 GB):

```bash
sdkmanager "platform-tools" "platforms;android-35" "build-tools;35.0.0" "emulator" "system-images;android-35;google_apis;x86_64"
```

Verify everything installed:

```bash
sdkmanager --list_installed
```

You should see `platform-tools`, `platforms;android-35`, `build-tools;35.0.0`, `emulator`, and `system-images;android-35;google_apis;x86_64`.


## Step 5 — Create an Android Virtual Device (AVD)

```bash
avdmanager create avd -n maui_dev -k "system-images;android-35;google_apis;x86_64"
```

When prompted *"Do you wish to create a custom hardware profile?"*, answer `no`.

Verify the AVD was created:

```bash
avdmanager list avd
```


## Step 6 — Install VS Code extensions

You need the official .NET MAUI extension on top of the standard C# tooling.

Required extensions:

| Extension | Purpose |
|---|---|
| `ms-dotnettools.csharp` | C# language support |
| `ms-dotnettools.csdevkit` | Solution explorer, test runner |
| `ms-dotnettools.dotnet-maui` | MAUI project support, Android deploy/debug |

Install the MAUI extension from the terminal:

```bash
code --install-extension ms-dotnettools.dotnet-maui
```

Or search **".NET MAUI"** in the VS Code Extensions panel.

The MAUI extension adds device selection (emulator or physical device), one-click launch/debug, and MAUI-specific C# and XAML tooling.


## Step 7 — Verify the build

From the repo root:

```bash
dotnet build src/NoteBookmark.MauiApp \
  /p:RuntimeIdentifier=android-arm64 \
  /p:AndroidSdkDirectory=$HOME/Android/Sdk
```


## Running the Emulator & App

### Windows (PowerShell Setup)
If you are developing on Windows, you can start the emulator, backend services, and build the app directly from the command line:

1. **Start the Emulator**:
   Run this in PowerShell to boot your Android tablet emulator:
   ```powershell
   & "C:\Program Files (x86)\Android\android-sdk\emulator\emulator.exe" -avd tablet_h-dpi_13_5in_-_api_30_0
   ```

2. **Start Backend Services**:
   Use the Aspire CLI to run the orchestrated backend API, Keycloak, and storage services:
   ```bash
   aspire run ./src/NoteBookmark.AppHost/NoteBookmark.AppHost.csproj
   ```

3. **Deploy the MAUI App**:
   Run this command to build and launch the app inside the running emulator:
   ```bash
   dotnet build src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj -t:Run -f net10.0-android -p:AndroidSdkDirectory="C:\Program Files (x86)\Android\android-sdk"
   ```
   *Alternatively, you can open the solution in Visual Studio or your preferred IDE to deploy and debug.*

### Linux (Bash Setup)
1. **Start the Emulator**:
   ```bash
   $ANDROID_HOME/emulator/emulator -avd maui_dev
   ```
   *Note: KVM hardware acceleration is enabled automatically when `/dev/kvm` is available, which makes the emulator fast and responsive.*

2. **Start Backend Services**:
   ```bash
   dotnet run --project src/NoteBookmark.AppHost/NoteBookmark.AppHost.csproj
   ```

3. **Deploy the MAUI App**:
   Ensure your emulator is running, then run:
   ```bash
   dotnet build src/NoteBookmark.MauiApp/NoteBookmark.MauiApp.csproj -t:Run -f net10.0-android -p:AndroidSdkDirectory=$HOME/Android/Sdk
   ```


## Troubleshooting

**`JAVA_HOME` is set to an invalid directory**
Make sure the path ends in `amd64` (not `amd6`). Check with:
```bash
ls /usr/lib/jvm/
```
Use the exact folder name shown.

**`sdkmanager: command not found`**
Run `source ~/.zshrc` to reload your PATH, or open a new terminal.

**`avdmanager: Package path is not valid. Valid system image paths are: null`**
The system image wasn't downloaded. Re-run:
```bash
sdkmanager "emulator" "system-images;android-35;google_apis;x86_64"
```

**`NETSDK1082` error during build**
This is a known issue with MAUI + Android on Linux. The `.csproj` in `NoteBookmark.MauiApp` already includes a workaround — ensure you're on the correct branch.

# Installing the Android App on Your Device

This guide walks you through installing (often called "sideloading") the NoteBookmark app on a physical Android device. Sideloading is the standard Android process for installing apps directly from a file (an `.apk` file) instead of the Google Play Store.

---

## Step 1: Download the App File
1. On your computer or phone, go to the project's **GitHub Repository**.
2. Tap on the **Actions** tab at the top.
3. Select the latest successful **Build Android APK** run.
4. Scroll down to the **Artifacts** section and download the **`notebookmark-android-apk`** ZIP file.
5. Extract the ZIP file to retrieve the **`.apk`** installer file (e.g., `NoteBookmark.MauiApp.apk`).

---

## Step 2: Transfer the File to Your Phone (if downloaded on a PC)
If you downloaded the file to a computer, transfer it to your phone using any of these methods:
* **Google Drive / OneDrive**: Upload the `.apk` file from your computer and open the Drive app on your phone to download it.
* **USB Cable**: Connect your phone to your computer and copy the `.apk` file to your phone's internal storage (e.g., the *Downloads* folder).
* **Email / Messaging**: Send the file to yourself as an email attachment or message, and download it on your phone.

---

## Step 3: Enable Installation Permissions
Android blocks installations from outside the Google Play Store by default for security. You must authorize your file manager or browser app to install it.

1. Open your phone's **File Manager** app (like *Files by Google*, *Samsung My Files*, or *Files*).
2. Navigate to your **Downloads** folder (or wherever you saved the `.apk` file).
3. Tap on the `.apk` file.
4. A security pop-up will appear stating: 
   > *"For your security, your phone is currently not allowed to install unknown apps from this source."*
5. Tap **Settings** on that prompt.
6. Toggle the **Allow from this source** switch to **ON**.

---

## Step 4: Install the App
1. Tap the **Back** button to return to the installation screen.
2. Tap **Install**.
3. Once the installation is complete, tap **Open** to launch the app, or **Done** to find it in your App Drawer.

---

## Step 5: Restore Security Settings (Optional)
For optimal device security, you can disable the installation permission you enabled in Step 3:
1. Open your phone's **Settings**.
2. Search for **Install unknown apps**.
3. Select your **File Manager** from the list and turn **Allow from this source** back **OFF**. The app will remain installed and fully functional.

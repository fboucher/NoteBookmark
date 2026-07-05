# Using the NoteBookmark MAUI Android App

This guide explains how the .NET MAUI Android application behaves, its offline capabilities, and how data synchronization works.

---

## 1. Offline Mode: Capabilities & Limitations
NoteBookmark uses an offline-first architecture powered by a local SQLite database on your device. When the app detects it is disconnected from the internet, it displays an **offline banner** at the top.

### What you CAN do offline:
* **Browse Content**: Read all previously downloaded posts, summaries, and notes.
* **Manage Notes (Comments)**: Add, edit, or delete notes/comments on any post. These changes are saved in a local offline queue.

### What you CANNOT do offline:
* **Add New Posts**: The input field for adding new URLs/posts is disabled when offline because creating posts requires real-time metadata extraction from the server.

---

## 2. Synchronization Policy
When your device is online, the app reconciles the local SQLite database with the remote server.

### When does Sync happen?
1. **Startup**: Automatically runs when the app is launched.
2. **Network Restored**: Automatically triggers as soon as your device goes from offline to online.
3. **Manually**: You can trigger a sync at any time by tapping the **Sync** button in the top-right corner of the Posts page.

### Conflict Resolution: Client-Wins
If a note or comment was modified both locally (offline) and on the server:
* The app uses a **Client-Wins** strategy. Your local offline edits, additions, and deletions are considered the source of truth and will overwrite the server data.
* New posts or summaries published on the server will be pulled down to your device.
* Posts deleted on the server will be deleted from your device.
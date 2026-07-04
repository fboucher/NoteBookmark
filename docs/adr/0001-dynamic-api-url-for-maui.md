# Dynamic API base URL configuration for the MAUI app

The MAUI app reads `ApiBaseUrl` from `appsettings.json` at build time and bakes it into the `HttpClient` via DI registration — making it impossible for the user to change at runtime. We decided to store the URL in `Preferences` (MAUI's key-value store) and resolve it per-request via a `DelegatingHandler`, so the user can configure it through the UI. This avoids the complexity of a local database for a single config value while keeping the shared `PostNoteClient` unchanged.

## Considered Options

- **Store in SQLite (via `LocalDataService`)**: Overkill for a single string; would introduce async complexity at startup before DI is ready.
- **Re-register `PostNoteClient` as a factory**: Would require breaking the shared `IDataService` abstraction used by both Blazor and MAUI.
- **Keep in `appsettings.json` only**: Requires a rebuild to change the URL — unacceptable for a production mobile app.

## Consequences

- On first launch the URL will be empty, so we added a route guard that redirects to a `/setup` page until the user configures it.
- The `appsettings.json` value still serves as a fallback default so development workflow is unaffected.

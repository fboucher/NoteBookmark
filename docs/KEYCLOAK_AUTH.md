# Keycloak Authentication Setup

## Overview

NoteBookmark now requires authentication via Keycloak (or any OpenID Connect provider). Only the home page is accessible without authentication - all other pages require a logged-in user.

## Configuration

### 1. Keycloak Server Setup

You'll need a Keycloak server running. For local development:

```bash
docker run -p 8080:8080 -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:latest start-dev
```

### 2. Create a Realm

1. Log into Keycloak admin console (http://localhost:8080)
2. Create a new realm called "notebookmark"

### 3. Create a Client

1. In the realm, create a new client:
   - Client ID: `notebookmark`
   - Client Protocol: `openid-connect`
   - Access Type: `confidential`
   - Valid Redirect URIs: `https://localhost:5001/*` (adjust for your environment)
   - Web Origins: `https://localhost:5001` (adjust for your environment)

2. Get the client secret from the Credentials tab

### 4. Configure the Application

Update `appsettings.json` or environment variables:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/notebookmark",
    "ClientId": "notebookmark",
    "ClientSecret": "your-client-secret-here"
  }
}
```

**Environment Variables (recommended for production):**

```bash
export Keycloak__Authority="https://your-keycloak-server/realms/notebookmark"
export Keycloak__ClientId="notebookmark"
export Keycloak__ClientSecret="your-secret"
```

### 5. Add Users

In Keycloak, create users in the realm who should have access to your private website.

## How It Works

- **Home page (/)**: Public - no authentication required
- **All other pages**: Protected with `[Authorize]` attribute
- **Login/Logout**: UI in the header shows login button when not authenticated
- **Session**: Uses cookie-based authentication with OpenID Connect

## Technical Details

- Uses `Microsoft.AspNetCore.Authentication.OpenIdConnect` package
- Cookie-based session management
- Authorization state cascaded throughout the component tree
- `AuthorizeRouteView` in Routes.razor handles route-level protection

## Files Modified

- `Program.cs`: Added authentication middleware and configuration
- `Routes.razor`: Changed to `AuthorizeRouteView` for authorization support
- `MainLayout.razor`: Added `LoginDisplay` component to header
- `_Imports.razor`: Added authorization namespaces
- All pages except `Home.razor`: Added `@attribute [Authorize]`
- `Components/Shared/LoginDisplay.razor`: New component for login/logout UI
- `Components/Pages/Login.razor`: Login page
- `Components/Pages/Logout.razor`: Logout page

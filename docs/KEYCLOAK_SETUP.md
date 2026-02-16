# Keycloak Authentication Setup

## Overview

NoteBookmark uses Keycloak for authentication via OpenID Connect. This provides enterprise-grade identity management with support for single sign-on, user federation, and fine-grained access control.

## Architecture

- **AppHost**: Manages Keycloak as an Aspire resource with data persistence
- **Keycloak Container**: Runs on port 8080 with development mode enabled
- **BlazorApp**: Configured for OpenID Connect authentication pointing to Keycloak realm

## Local Development

### Default Credentials

- **Admin Console**: http://localhost:8080/admin
- **Username**: `admin`
- **Password**: `admin` (or set via `KEYCLOAK_ADMIN_PASSWORD` environment variable)

### Realm Configuration

The application expects a realm named `notebookmark` with:
- **Client ID**: `notebookmark`
- **Client Secret**: Set via `KEYCLOAK_CLIENT_SECRET` environment variable
- **Valid Redirect URIs**: 
  - `https://localhost:*/signin-oidc`
  - `http://localhost:*/signin-oidc`
- **Valid Post Logout Redirect URIs**:
  - `https://localhost:*`
  - `http://localhost:*`

### Environment Variables

For development, set these in `appsettings.development.json`:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/notebookmark",
    "ClientId": "notebookmark",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

## Docker Compose

Keycloak is defined in `docker-compose/docker-compose.yaml`:

- **Image**: `quay.io/keycloak/keycloak:26.1`
- **Port**: 8080
- **Data Volume**: `keycloak-data` for persistence
- **Network**: `aspire` (shared with API and BlazorApp)

### Environment Variables for docker-compose

Set these environment variables before running docker-compose:

```bash
export KEYCLOAK_ADMIN_PASSWORD=your_secure_password
export KEYCLOAK_CLIENT_SECRET=your_client_secret
export KEYCLOAK_AUTHORITY=http://localhost:8080/realms/notebookmark
export KEYCLOAK_CLIENT_ID=notebookmark
```

## Production Considerations

### HTTPS Requirements

In production, you **must**:
1. Set `Keycloak:Authority` to an HTTPS URL (e.g., `https://keycloak.yourdomain.com/realms/notebookmark`)
2. Use valid SSL certificates for Keycloak
3. Ensure `RequireHttpsMetadata = true` in OpenID Connect configuration (default)

### Secrets Management

Never commit secrets to source control. Use:
- Azure Key Vault for production secrets
- User Secrets for local development: `dotnet user-secrets set "Keycloak:ClientSecret" "your-secret"`
- Environment variables in deployment environments

### Keycloak Configuration

For production:
1. Disable development mode (`start-dev` → `start`)
2. Configure proper database backend (PostgreSQL recommended)
3. Enable clustering if needed for high availability
4. Set up proper logging and monitoring
5. Configure rate limiting and security headers

## First-Time Setup

1. **Start Keycloak**: Run the AppHost or `docker-compose up keycloak`
2. **Access Admin Console**: Navigate to http://localhost:8080/admin
3. **Login**: Use admin/admin
4. **Create Realm**: 
   - Name it `notebookmark`
   - Configure as needed
5. **Create Client**:
   - Client ID: `notebookmark`
   - Client Protocol: `openid-connect`
   - Access Type: `confidential`
   - Valid Redirect URIs: `https://localhost:*/signin-oidc`
   - Copy the client secret from Credentials tab
6. **Update Configuration**: Add client secret to `appsettings.development.json`
7. **Create Users**: Add users in Users section of realm

## Troubleshooting

### "Unable to connect to Keycloak"
- Ensure Keycloak container is running: `docker ps | grep keycloak`
- Check port 8080 is not already in use
- Verify network connectivity: `curl http://localhost:8080`

### "Invalid redirect URI"
- Check Keycloak client configuration matches your app's redirect URI
- Ensure wildcards are properly configured for development

### "Invalid client secret"
- Verify `Keycloak:ClientSecret` matches the value in Keycloak admin console
- Check environment variables are properly set

### "HTTPS metadata required"
- For development: Set `RequireHttpsMetadata = false` in Program.cs (already configured)
- For production: Use HTTPS Authority URL

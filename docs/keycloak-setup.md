# Keycloak Realm Setup For NoteBookmark

This file explains only how to configure Keycloak for NoteBookmark.

If you do not have a Keycloak server yet, use [`docs/keycloak-container-setup.md`](keycloak-container-setup.md) first.

## Official References

- Keycloak server administration guide: <https://www.keycloak.org/docs/latest/server_admin/>
- Keycloak securing applications (OIDC clients): <https://www.keycloak.org/docs/latest/server_admin/#oidc-clients>

## 1. Create Realm

In the Keycloak admin console, create a realm named:

- `notebookmark`

## 2. Create OIDC Client

In realm `notebookmark`, create a client with:

- Client ID: `notebookmark`
- Protocol: OpenID Connect
- Client authentication: Enabled (confidential client)
- Standard flow: Enabled

Set redirect and origin values for your app URL.

Local example:

- Valid redirect URIs: `http://localhost:8005/*`
- Valid post logout redirect URIs: `http://localhost:8005/*`
- Web origins: `http://localhost:8005`

Then copy the generated client secret.

## 3. Map Keycloak Values To NoteBookmark

Use these values in `docker-compose/.env`:

```env
KEYCLOAK_AUTHORITY=http://localhost:8080/realms/notebookmark
KEYCLOAK_CLIENT_ID=notebookmark
KEYCLOAK_CLIENT_SECRET=your-client-secret
```

These are consumed by `docker-compose/note-compose.yaml`:

- `Keycloak__Authority: ${KEYCLOAK_AUTHORITY}`
- `Keycloak__ClientId: ${KEYCLOAK_CLIENT_ID}`
- `Keycloak__ClientSecret: ${KEYCLOAK_CLIENT_SECRET}`

## 4. Validate Before Running NoteBookmark

Check that:

- Realm is exactly `notebookmark`
- Client ID is exactly `notebookmark`
- Client secret in `.env` matches Keycloak
- Redirect URI matches your app URL

After that, run NoteBookmark using [`docs/docker-compose-deployment.md`](docker-compose-deployment.md).

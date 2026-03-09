# Keycloak Container Setup (If You Do Not Have Keycloak Yet)

Use this file only to get a Keycloak container running for NoteBookmark.

After Keycloak is up, continue with:

1. [`docs/keycloak-setup.md`](keycloak-setup.md) to configure realm/client
2. [`docs/docker-compose-deployment.md`](docker-compose-deployment.md) to run NoteBookmark

## Official References

- Keycloak container guide: <https://www.keycloak.org/server/containers>
- Keycloak configuration docs: <https://www.keycloak.org/server/configuration>

## 1. Prepare Environment File

From repository root:

```bash
cp .env-sample docker-compose/.env
```

Set at least these values in `docker-compose/.env`:

```env
KEYCLOAK_USER=admin
KEYCLOAK_PASSWORD=change-me
KEYCLOAK_URL=localhost
POSTGRES_USER=keycloak
POSTGRES_PASSWORD=change-me
```

## 2. Create Shared Network (One Time)

```bash
docker network create notebookmark
```

Then move into the compose folder so `.env` is auto-detected:

```bash
cd docker-compose
```

## 3. Start Keycloak Stack

```bash
docker compose -f keycloak-compose.yaml up -d
```

This starts:

- `keycloak_postgres`
- `keycloak`

Keycloak admin console: `http://localhost:8080`

## 4. Stop Keycloak Stack

```bash
docker compose -f keycloak-compose.yaml down
```

Remove Keycloak database volume (deletes Keycloak data):

```bash
docker compose -f keycloak-compose.yaml down -v
```

## Quick Validation

```bash
docker compose -f keycloak-compose.yaml config
```

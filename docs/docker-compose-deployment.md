# Docker Compose Deployment

This file assumes you already have:

- A healthy Keycloak instance
- A `notebookmark` realm configured (see [`docs/keycloak-setup.md`](keycloak-setup.md))

If you do not have Keycloak yet, see [`docs/keycloak-container-setup.md`](keycloak-container-setup.md) first.


## Prerequisites

- Docker Engine with Compose support (`docker compose`)
- `docker-compose/.env` with valid values
- Azure Storage endpoints (Table + Blob)
- Keycloak client secret for client `notebookmark`

## 1. Prepare Environment Values

From the repository root:

```bash
cp .env-sample docker-compose/.env
```

Edit `docker-compose/.env` and set all required values.

Important Keycloak values for NoteBookmark:

- `KEYCLOAK_AUTHORITY` (for example `http://localhost:8080/realms/notebookmark`)
- `KEYCLOAK_CLIENT_ID` (default: `notebookmark`)
- `KEYCLOAK_CLIENT_SECRET` (from Keycloak client settings)

## 2. Create Shared Network (One Time)

```bash
docker network create notebookmark
```

Then move into the compose folder so `.env` is auto-detected:

```bash
cd docker-compose
```

## 3. Start NoteBookmark App

```bash
docker compose -f note-compose.yaml up -d
```

## 4. Access Services

- Blazor App: `http://localhost:8005`
- API: `http://localhost:8001`

## 5. Stop NoteBookmark App

```bash
docker compose -f note-compose.yaml down
```

## Quick Validation

```bash
docker compose -f note-compose.yaml config
```



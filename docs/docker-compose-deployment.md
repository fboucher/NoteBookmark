# Docker Compose Deployment

This guide explains how to deploy NoteBookmark using Docker Compose, either by generating it fresh from Aspire or using the provided compose file.

## Two Deployment Options

### Option 1: Generate from Aspire (Recommended)

Generate an up-to-date docker-compose.yaml from your Aspire AppHost configuration.

**Prerequisites:**
- .NET Aspire Workload installed: `dotnet workload install aspire`
- [aspirate](https://github.com/prom3theu5/aspirate) CLI tool: `dotnet tool install -g aspirate`

**Steps:**

1. **Generate the Aspire manifest:**
   ```bash
   dotnet run --project src/NoteBookmark.AppHost --publisher manifest --output-path ./aspire-manifest
   ```
   This creates a `manifest.json` file that describes your application's services and dependencies.

2. **Convert manifest to docker-compose:**
   ```bash
   aspirate generate --manifest-path ./aspire-manifest/manifest.json --output-path ./docker-compose-generated
   ```
   This uses the `aspirate` tool to transform the Aspire manifest into a working docker-compose.yaml file.

3. **Review the generated files:**
   - `docker-compose.yaml` - Main compose file
   - `.env` template - Environment variables to configure

This ensures your docker-compose file stays in sync with the latest AppHost configuration.

> **Note:** The `--publisher manifest` command alone does NOT generate docker-compose files - it creates an intermediate manifest.json. You need the `aspirate` tool (or similar) to convert it to docker-compose format.

### Option 2: Use the Provided Compose File (Quick Start)

For a quick start without cloning the repository, you can use the checked-in docker-compose.yaml file located in the `docker-compose/` directory. This is ideal if you just want to run the application quickly without generating the manifest yourself.

## Environment Configuration

The docker-compose.yaml file uses environment variables for configuration. You must create a `.env` file in the same directory as your docker-compose.yaml file.

### What the .env File Is For

The `.env` file contains sensitive configuration values needed for production deployment:

- **Database connection strings**: Connection to Azure Table Storage and Blob Storage
- **Keycloak configuration**: Authentication server settings (authority URL, client credentials)
- **Other runtime settings**: Any environment-specific configurations

### Creating Your .env File

1. Copy the `.env-sample` file from the repository root:
   ```bash
   cp .env-sample .env
   ```

2. Edit `.env` and replace all placeholder values with your actual configuration:
   - Azure Storage connection strings
   - Keycloak admin password
   - Keycloak client secret
   - Keycloak authority URL (if different from default)

3. Keep `.env` secure and never commit it to version control (it's in .gitignore)

## Running the Application

Once your `.env` file is configured:

```bash
cd docker-compose
docker compose up -d
```

Access the application at:
- **Blazor App**: http://localhost:8005
- **API**: http://localhost:8001
- **Keycloak**: http://localhost:8080

## Stopping the Application

```bash
docker compose down
```

To also remove volumes (WARNING: This deletes Keycloak data):

```bash
docker compose down -v
```

## Notes

- The AppHost maintains `AddDockerComposeEnvironment("docker-env")` to integrate with the docker-compose setup
- Aspire service discovery automatically wires up connections in development
- In production (docker-compose), explicit environment variables are required
- Keycloak data persists in a named volume (`keycloak-data`)

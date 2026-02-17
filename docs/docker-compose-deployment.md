# Docker Compose Deployment

This guide explains how to deploy NoteBookmark using Docker Compose, either by generating it fresh from Aspire or using the provided compose file.

## Two Deployment Options

### Option 1: Generate from Aspire (Recommended)

Generate an up-to-date docker-compose.yaml from your Aspire AppHost configuration using the official Aspire CLI.

**Prerequisites:**
- .NET Aspire Workload installed: `dotnet workload install aspire`
- Aspire CLI installed: Included with the Aspire workload

**Steps:**

1. **Build container images locally:**
   
   The generated docker-compose file references image names (e.g., `notebookmark-api`, `notebookmark-blazor`), but these images don't exist until you build them. Build and tag the images with the expected names:
   
   ```bash
   # Build API image
   dotnet publish src/NoteBookmark.Api/NoteBookmark.Api.csproj -c Release -t:PublishContainer
   
   # Build Blazor app image
   dotnet publish src/NoteBookmark.BlazorApp/NoteBookmark.BlazorApp.csproj -c Release -t:PublishContainer
   ```
   
   These commands build the projects and create Docker images tagged as `notebookmark-api:latest` and `notebookmark-blazorapp:latest` (based on your project names). The container names `notebookmark-api` and `notebookmark-blazor` are what the running containers will be called.

2. **Publish the application (generates Docker Compose files):**
   ```bash
   aspire publish --output-path ./aspire-output --project-name notebookmark
   ```
   
   **Parameters:**
   - `--output-path`: Directory where docker-compose files will be generated (default: `aspire-output`)
   - `--project-name`: Docker Compose project name (sets `name:` at the top of docker-compose.yaml)
     - Without this, the project name defaults to the output directory name
     - Affects container names: `notebookmark-api`, `notebookmark-blazor` vs `aspire-output-api`, `aspire-output-blazor`
   
   This command generates:
   - `docker-compose.yaml` from the AppHost configuration
   - `.env` file template with expected parameters (unfilled)
   - Supporting infrastructure files (Bicep, Azure configs if applicable)

3. **Fill in environment variables:**
   Edit `./aspire-output/.env` and replace placeholder values with your actual configuration:
   - Azure Storage connection strings
   - Keycloak admin password and client secrets
   - Any other environment-specific settings

4. **Deploy (optional - full workflow):**
   ```bash
   aspire deploy --output-path ./aspire-output
   ```
   This performs the complete workflow: publishes, prepares environment configs, builds images, and runs `docker compose up`.

   Or manually run Docker Compose from the output directory:
   ```bash
   cd aspire-output
   docker compose up -d
   ```

This ensures your docker-compose file stays in sync with the latest AppHost configuration.

> **📚 Learn more:** See the [official Aspire Docker integration documentation](https://aspire.dev/integrations/compute/docker/) for advanced scenarios like environment-specific configs and custom image tagging.

### Option 2: Use the Provided Compose File (Quick Start)

For a quick start, you can use the checked-in docker-compose.yaml file located in the `docker-compose/` directory. This file was generated from Aspire and committed to the repository for convenience.

**When to use this option:**
- You want to quickly test the application without regenerating compose files
- You're deploying a stable release version
- You haven't modified the AppHost configuration

**Important:** If you've modified `src/NoteBookmark.AppHost/AppHost.cs`, use Option 1 to regenerate the compose file to reflect your changes.

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

**If using Option 1 (Aspire-generated):**
```bash
cd aspire-output
docker compose up -d
```

**If using Option 2 (checked-in file):**
```bash
cd docker-compose
docker compose up -d
```

Access the application at:
- **Blazor App**: http://localhost:8005
- **API**: http://localhost:8001
- **Keycloak**: http://localhost:8080

**First-time setup:** Keycloak needs to be configured with the realm settings. See [Keycloak Setup Guide](./keycloak-setup.md) for detailed instructions.

## Stopping the Application

```bash
docker compose down
```

To also remove volumes (WARNING: This deletes Keycloak data):

```bash
docker compose down -v
```

## Advanced Deployment Workflows

The Aspire CLI supports environment-specific deployments:

**Prepare for a specific environment:**
```bash
# For staging
aspire do prepare-docker-env --environment staging

# For production
aspire do prepare-docker-env --environment production
```

This generates environment-specific `.env` files and builds container images.

**Clean up a deployment:**
```bash
aspire do docker-compose-down-docker-env
```

This stops and removes all containers, networks, and volumes.

## Notes

- **Development vs Production:**
  - In development (`dotnet run`), Aspire manages Keycloak automatically via `AddKeycloak()`
  - In production (docker-compose), Keycloak runs as a containerized service
  - The AppHost uses `AddDockerComposeEnvironment("docker-env")` to signal Azure Container Apps deployment intent
  
- **Service Discovery:**
  - Development: Aspire service discovery wires up connections automatically
  - Production: Services connect via explicit environment variables in `.env`
  
- **Data Persistence:**
  - Keycloak data persists in a named volume (`keycloak-data`)
  - Use `docker compose down -v` carefully — it deletes all data including Keycloak configuration

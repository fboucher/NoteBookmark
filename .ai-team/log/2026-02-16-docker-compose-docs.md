# Session: Docker-Compose Deployment Documentation

**Requested by:** fboucher

## Summary

User changed direction mid-session: initially planned to remove AddDockerComposeEnvironment from AppHost, but changed course to keep it and create documentation instead. Final decision was to implement dual-mode architecture—development uses Aspire's native Keycloak, production uses docker-compose standalone.

## Work Completed

1. **Hicks:** Removed AddDockerComposeEnvironment() from AppHost to resolve port conflicts. Split Keycloak into dev/prod modes: development uses Aspire-managed lifecycle, production expects docker-compose to manage containers independently.

2. **Hicks:** Fixed Keycloak logout flow by converting OnRedirectToIdentityProviderForSignOut event handler to async and properly awaiting GetTokenAsync("id_token") call—resolves "Missing parameters: id_token_hint" error.

## Decisions Made

- Keep AddDockerComposeEnvironment in docker-compose.yaml; document it for production users instead of removing it
- Implement dual-mode: AppHost branches on Environment.IsDevelopment() for Keycloak configuration
- Production deployment uses docker-compose.yaml independently without AppHost interference

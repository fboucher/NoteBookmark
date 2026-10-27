# AI Provider Setup

NoteBookmark uses an AI model to generate post summaries and research suggestions. It works with any provider that exposes an **OpenAI-compatible** Chat Completions API. This guide explains the three AI settings, how the app decides which values to use, and gives working examples for [OpenCode.ai](#opencodeai), [OpenRouter](#openrouter) and [Ollama](#ollama-local).

## The three settings

| Setting | What it expects |
|---|---|
| **API Key** | The key issued by your provider. |
| **Base URL** | The root of the provider's OpenAI-compatible API, ending at `/v1`. |
| **Model Name** | The model id exactly as the provider names it. |

### The Base URL must stop at `/v1`

The app builds an OpenAI .NET SDK `ChatClient` with `Endpoint = BaseUrl`, and the SDK appends `/chat/completions` itself. So the Base URL must **not** include `/chat/completions` or any other trailing path.

| ✅ Correct | ❌ Wrong |
|---|---|
| `https://opencode.ai/inference/openai/v1` | `https://opencode.ai/inference/openai/v1/chat/completions` |
| `https://openrouter.ai/api/v1` | `https://openrouter.ai/api/v1/chat/completions` |

With the wrong form, the request goes to `.../chat/completions/chat/completions` and fails.

## Where the settings come from

The app reads the AI settings in this order:

1. **The Settings page** (stored in the database). These values are used **only when API Key is non-empty**. If Base URL or Model Name is left empty, a built-in default is used for that field.
2. **Configuration** (environment variables or `appsettings.json`), used when no API Key is saved on the Settings page:
   - API key: `AppSettings:AiApiKey`, then `AppSettings:REKA_API_KEY`, then the `REKA_API_KEY` environment variable (the Reka names are still supported for backward compatibility).
   - Base URL: `AppSettings:AiBaseUrl`
   - Model name: `AppSettings:AiModelName`

As environment variables (for Docker Compose or Azure Container Apps) the configuration keys use a double underscore: `AppSettings__AiApiKey`, `AppSettings__AiBaseUrl`, `AppSettings__AiModelName`.

> **Tip:** Set all three values together. If you only set the API key, the Base URL and Model Name fall back to the built-in defaults, which are unlikely to match your provider.

## When a summary comes back empty

AI errors don't show up in the UI. The summary just comes back empty. To find the cause, look at the **blazor-app** logs (the AI calls are made by the web app, not the API):

- With Aspire, open the dashboard, select the `blazor-app` resource, and look at its console logs or structured logs.
- With Docker Compose, run `docker logs notebookmark-blazor`.

Look for `An error occurred while generating summary:` followed by the provider's error message. The usual causes are:

- A Base URL that ends with `/chat/completions` (see above).
- A model id the provider doesn't serve on its OpenAI-compatible endpoint.
- An invalid or missing API key.

## Provider examples

### OpenCode.ai

| Field | Value |
|---|---|
| **API Key** | Your OpenCode key |
| **Base URL** | `https://opencode.ai/inference/openai/v1` |
| **Model Name** | A model served on the OpenAI-compatible path, for example a DeepSeek, GLM, Kimi, MiniMax, Qwen or Mistral model |

The full model list is at `https://opencode.ai/inference/v1/models`.

> **Gotcha: Gemini models don't work.** OpenCode serves Gemini models (for example `gemini-*-flash`) only on its Google-native path (`/inference/google/v1beta/...`), not on the OpenAI-compatible one. NoteBookmark can only talk to the OpenAI-compatible path, so pick a model from the list above instead.

### OpenRouter

| Field | Value |
|---|---|
| **API Key** | Your OpenRouter key (from <https://openrouter.ai/keys>) |
| **Base URL** | `https://openrouter.ai/api/v1` |
| **Model Name** | Any id from <https://openrouter.ai/models>, in `vendor/model` form (for example `mistralai/mistral-small-3.2-24b-instruct`) |

### Ollama (local)

| Field | Value |
|---|---|
| **API Key** | Any non-empty placeholder, for example `ollama`. Ollama ignores it, but the app only uses the Settings page values when the API Key is non-empty. |
| **Base URL** | `http://localhost:11434/v1`, or `http://host.docker.internal:11434/v1` when NoteBookmark runs in a container and Ollama runs on the host |
| **Model Name** | A model you've pulled, as shown by `ollama list` (for example `llama3.2`) |

Pull the model first, for example `ollama pull llama3.2`.

## Configuring without the Settings page

Use these when you'd rather set the provider in configuration than through the UI.

**Local development (Aspire)**, in `src/NoteBookmark.AppHost/appsettings.json` or user secrets:

```json
"AppSettings": {
  "AiApiKey": "your-openrouter-key",
  "AiBaseUrl": "https://openrouter.ai/api/v1",
  "AiModelName": "mistralai/mistral-small-3.2-24b-instruct"
}
```

The AppHost passes these to the web app.

**Docker Compose**, in `docker-compose/.env` (see [Docker Compose Deployment](docker-compose-deployment.md)):

```bash
AI_API_KEY=your-openrouter-key
AI_BASE_URL=https://openrouter.ai/api/v1
AI_MODEL_NAME=mistralai/mistral-small-3.2-24b-instruct
```

`note-compose.yaml` maps them to `AppSettings__AiApiKey`, `AppSettings__AiBaseUrl` and `AppSettings__AiModelName` on the `blazor-app` service.

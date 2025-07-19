# ModelHarbor API Setup Guide

## Overview

This guide explains how to set up and configure ModelHarbor API keys for your chatbot system.

## Getting Started

### 1. Obtain a ModelHarbor API Key

1. Visit [ModelHarbor](https://modelharbor.com)
2. Create an account or sign in
3. Navigate to your dashboard
4. Go to "API Keys" section
5. Generate a new API key
6. Copy the key (it will look like: `mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`)

### 2. Configure Your Chatbot

#### Option A: Database Update (Recommended)

Update the `LlmKey` field for your chatbot in the database:

```sql
UPDATE "Chatbots"
SET "LlmKey" = 'mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'
WHERE "Id" = 1;
```

#### Option B: Environment Variables

Add to your environment:

```bash
export MODEL_HARBOR_API_KEY="mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

#### Option C: Configuration File

Update `appsettings.json`:

```json
{
  "OpenAI": {
    "DefaultModel": "openai/gpt-4.1",
    "ApiKey": "mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

### 3. Verify Configuration

Test your API key using curl:

```bash
curl -X POST https://api.modelharbor.com/v1/chat/completions \
  -H "Authorization: Bearer mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-4.1",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

## Common Issues and Solutions

### Error: "Invalid API key provided"

- **Cause**: API key is invalid or not properly configured
- **Solution**:
  1. Verify your API key from ModelHarbor dashboard
  2. Check for extra spaces or typos
  3. Ensure the key is active and not expired

### Error: "token_not_found_in_db"

- **Cause**: The API key doesn't exist in ModelHarbor's database
- **Solution**: Generate a new API key from your ModelHarbor dashboard

### Error: "Unauthorized"

- **Cause**: API key is missing or incorrectly formatted
- **Solution**: Ensure the key is properly prefixed with "mh\_" and complete

## Security Best Practices

1. **Never commit API keys to version control**
2. **Use environment variables in production**
3. **Rotate keys regularly**
4. **Use different keys for different environments**
5. **Monitor API usage in ModelHarbor dashboard**

## Model Selection

Available models can be retrieved from:

```bash
curl -X GET https://api.modelharbor.com/v1/model/info \
  -H "Authorization: Bearer mh_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

Popular models include:

- `openai/gpt-4.1` - Latest GPT-4.1 model
- `openai/gpt-4o` - GPT-4 Optimized
- `gemini/gemini-2.0-flash` - Google's Gemini Flash
- `gemini/gemini-2.5-pro` - Google's Gemini Pro

## Troubleshooting

### Check Current Configuration

```sql
SELECT "Id", "Name", "LlmKey" IS NOT NULL as "HasApiKey"
FROM "Chatbots"
WHERE "Id" = 1;
```

### Test Database Connection

```bash
dotnet ef database update
```

### View Application Logs

Check your application logs for detailed error messages:

```
[ERR] Failed to call ModelHarbor API: Unauthorized {"error":{"message":"Authentication Error: Invalid API key provided"...
```

## Support

If issues persist:

1. Check ModelHarbor status page
2. Review ModelHarbor documentation
3. Contact ModelHarbor support

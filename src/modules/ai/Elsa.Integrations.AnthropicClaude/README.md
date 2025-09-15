# Elsa.Integrations.AnthropicClaude

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **Anthropic Claude AI**. It introduces custom activities that make it easy to integrate Claude's powerful language models directly into your workflow logic.

## ✨ Key Features

- **CreatePrompt** - Creates structured prompts and gets AI-generated responses from Claude
- **MakeAPICall** - Performs arbitrary authorized API calls to the Anthropic Claude API
- Support for all Claude models (Claude-3 Sonnet, Haiku, Opus)
- Configurable temperature, token limits, and stop sequences
- Comprehensive error handling and logging
- Token usage tracking for cost monitoring

---

## ⚡ Getting Started

### 📋 Prerequisites

- Elsa Workflows **3.7.0+** installed in your project
- Anthropic Claude API key from [Anthropic Console](https://console.anthropic.com/)

### 🛠 Installation

Add the Anthropic Claude extension to your project:

```bash
dotnet add package Elsa.Integrations.AnthropicClaude
```

### 🔧 Registration

Register the Anthropic Claude extension in your Elsa builder:

```csharp
// Program.cs or Startup.cs
services
    .AddElsa(elsa => 
    {
        elsa.UseAnthropicClaude();
        // Other Elsa configurations...
    });
```

## 🔐 Authentication

The Claude activities require an API key from Anthropic for authentication. You can obtain an API key from the [Anthropic Console](https://console.anthropic.com/).

**Important**: Store your API key securely using Elsa's secrets management system or environment variables. Never hard-code API keys in your workflows.

---

## 🛠 Available Activities

### CreatePrompt

Creates a structured prompt and sends it to Claude AI for completion.

**Inputs:**
- **ApiKey** (required) - Your Anthropic Claude API key
- **Model** - Claude model to use (default: claude-3-sonnet-20240229)
- **UserMessage** (required) - The user message/prompt to send
- **SystemPrompt** - System prompt that defines Claude's behavior
- **PreviousMessages** - JSON array of previous conversation messages
- **MaxTokens** - Maximum tokens to generate (default: 1024)
- **Temperature** - Response randomness 0.0-1.0 (default: 0.7)
- **StopSequences** - Comma-separated stop sequences

**Outputs:**
- **Response** - The text response from Claude
- **FullResponse** - Complete response object with metadata
- **InputTokens** - Number of input tokens used
- **OutputTokens** - Number of output tokens generated

### MakeAPICall

Performs an arbitrary HTTP call to the Anthropic Claude API.

**Inputs:**
- **ApiKey** (required) - Your Anthropic Claude API key
- **HttpMethod** - HTTP method to use (default: POST)
- **Endpoint** - API endpoint relative to base URL (default: messages)
- **RequestBody** - JSON request body for POST/PUT requests
- **ValidateJson** - Whether to validate JSON before sending (default: true)

**Outputs:**
- **ResponseBody** - Raw response body from the API
- **Success** - Whether the call was successful (2xx status)

---

## 📚 Example Usage

### Basic Text Generation

```csharp
// Workflow definition example
public class ClaudeWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder
            .Root<CreatePrompt>(prompt => prompt
                .Set(x => x.ApiKey, "your-api-key-here")
                .Set(x => x.UserMessage, "Explain quantum computing in simple terms")
                .Set(x => x.SystemPrompt, "You are a helpful science teacher.")
                .Set(x => x.MaxTokens, 500))
            .Then<WriteLine>(write => write
                .Set(x => x.Text, prompt => prompt.Response));
    }
}
```

### Conversation with Context

```csharp
public class ConversationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var previousMessages = """
        [
            {"role": "user", "content": "What is machine learning?"},
            {"role": "assistant", "content": "Machine learning is a subset of AI..."}
        ]
        """;

        builder
            .Root<CreatePrompt>(prompt => prompt
                .Set(x => x.ApiKey, context => context.GetVariable<string>("ClaudeApiKey"))
                .Set(x => x.UserMessage, "Can you give me a practical example?")
                .Set(x => x.PreviousMessages, previousMessages)
                .Set(x => x.Temperature, 0.5));
    }
}
```

### Custom API Call

```csharp
public class CustomApiWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var customRequest = """
        {
            "model": "claude-3-haiku-20240307",
            "max_tokens": 100,
            "messages": [
                {"role": "user", "content": "Hello Claude!"}
            ]
        }
        """;

        builder
            .Root<MakeAPICall>(call => call
                .Set(x => x.ApiKey, "your-api-key-here")
                .Set(x => x.Endpoint, "messages")
                .Set(x => x.RequestBody, customRequest))
            .Then<WriteLine>(write => write
                .Set(x => x.Text, call => call.ResponseBody));
    }
}
```

---

## 🗒️ Notes & Best Practices

### Model Selection
- **claude-3-opus-20240229** - Most capable, highest cost, slowest
- **claude-3-sonnet-20240229** - Balanced performance and cost (recommended)
- **claude-3-haiku-20240307** - Fastest, lowest cost, good for simple tasks

### Token Management
- Monitor `InputTokens` and `OutputTokens` to track API usage costs
- Set appropriate `MaxTokens` limits to control response length
- Use shorter prompts when possible to reduce input token costs

### Error Handling
- Activities will throw exceptions for API errors (invalid keys, rate limits, etc.)
- Use Elsa's error handling activities to gracefully handle failures
- Check the `Success` output on `MakeAPICall` for custom error handling

### Security
- Always use Elsa's secrets management for API keys
- Never log or expose API keys in workflow outputs
- Consider using environment-specific API keys for different deployment stages

---

## 📖 References

- [Anthropic Claude API Documentation](https://docs.anthropic.com/)
- [Anthropic Console](https://console.anthropic.com/)
- [Claude Model Comparison](https://docs.anthropic.com/claude/docs/models-overview)
- [Elsa Workflows Documentation](https://elsa-workflows.github.io/elsa-core/)

---

## 🗺️ Planned Features

- [ ] Add streaming response support
- [ ] Add function calling capabilities
- [ ] Add image input support (when available)
- [ ] Add async retry/backoff support
- [ ] Add batch processing activities

---

This extension was developed to add Anthropic Claude AI functionality to Elsa Workflows.  
If you have ideas for improvement, encounter issues, or want to share how you're using it, feel free to open an issue or start a discussion!
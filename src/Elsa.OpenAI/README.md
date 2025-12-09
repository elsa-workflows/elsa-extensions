# Elsa.OpenAI

OpenAI integration for Elsa Workflows, enabling GPT-based text generation and chatbot functionality in your workflows.

## 🚀 Getting Started

### Installation
```bash
dotnet add package Elsa.OpenAI
```

### Configuration
```csharp
services.AddElsa(elsa =>
{
    elsa.AddOpenAI();
});
```

### API Key Setup
Set your OpenAI API key using one of these methods:

**User Secrets (Development):**
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

**Environment Variable:**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

## 📋 Activities

### Complete Chat
Generates text responses using OpenAI's chat models.

**Inputs:**
- `Prompt` (string) - The user message or question
- `SystemMessage` (string, optional) - Context or instructions for the AI
- `Model` (string) - OpenAI model (e.g., "gpt-3.5-turbo", "gpt-4")
- `MaxTokens` (int, optional) - Maximum response length
- `Temperature` (float, optional) - Response creativity (0.0-1.0)
- `ApiKey` (string) - OpenAI API key

**Outputs:**
- `Result` (string) - The AI-generated response
- `TotalTokens` (int) - Number of tokens used
- `FinishReason` (string) - How the completion ended

## 💡 Use Cases

### Customer Support Chatbot
```csharp
// System Message: "You are a helpful customer support agent."
// Prompt: User's question from support ticket
// Result: AI-generated support response
```

### Content Generation
```csharp
// System Message: "Generate marketing copy for our product."
// Prompt: Product description and target audience
// Result: Marketing content
```

### Code Review Assistant
```csharp
// System Message: "You are a code reviewer. Provide constructive feedback."
// Prompt: Code snippet to review
// Result: Code review comments and suggestions
```

### Data Processing
```csharp
// System Message: "Extract key information from the following text."
// Prompt: Raw text data
// Result: Structured information
```

## 🔧 Configuration Options

- **Model Selection**: Choose from GPT-3.5, GPT-4, or other available models
- **Temperature Control**: Adjust response creativity and randomness
- **Token Limits**: Control response length and API costs
- **System Messages**: Provide context and role-based instructions

## 🔐 Security

- API keys are never logged or exposed in workflow definitions
- Use User Secrets for development environments
- Use secure environment variables or key vaults for production

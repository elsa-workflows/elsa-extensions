# Elsa.OpenAI Integration

A comprehensive OpenAI integration for Elsa workflows that provides secure, workflow-friendly access to OpenAI's capabilities.

## 🚀 Features

- **Complete Chat Support**: Basic and streaming chat completions with GPT models
- **Secure API Key Management**: User Secrets, environment variables, and .env file support
- **Client Factory Pattern**: Efficient client caching and management
- **Elsa-Native Activities**: Follows all Elsa workflow patterns and conventions
- **Comprehensive Testing**: Unit and integration tests with real API validation

## 📦 Installation

The OpenAI integration is included in the Elsa extensions project with the OpenAI NuGet package (v2.7.0) automatically managed.

## 🔐 API Key Setup

Your OpenAI API key is securely stored outside of source control using several methods:

### Method 1: User Secrets (Recommended)
```bash
# For unit tests
dotnet user-secrets set "OpenAI:ApiKey" "your-key-here" --project test/unit/Elsa.OpenAI.Tests/

# For validation tests  
dotnet user-secrets set "OpenAI:ApiKey" "your-key-here" --project validate-tests/
```

### Method 2: Environment Variable
```bash
export OPENAI_API_KEY="your-key-here"
```

### Method 3: Local Environment File
```bash
# Create .env.local file (automatically gitignored)
echo 'OPENAI_API_KEY=your-key-here' > .env.local

# Load it when needed
source load-env.sh
```

## 🧪 Testing

### Quick Validation Tests
```bash
dotnet run --project validate-tests/
```

### Full Unit Test Suite
```bash
dotnet test test/unit/Elsa.OpenAI.Tests/
```

The tests include:
- ✅ Activity structure validation
- ✅ Client factory caching
- ✅ Multiple client types (Chat, Image, Audio, Embedding, Moderation)
- ✅ Real API calls (when API key is configured)
- ✅ Configuration management

## 🔧 Usage in Elsa Workflows

### Basic Chat Completion Activity

**Inputs:**
- `ApiKey` (string): Your OpenAI API key
- `Model` (string): OpenAI model (e.g., "gpt-3.5-turbo", "gpt-4")
- `Prompt` (string): The user message/prompt
- `SystemMessage` (string, optional): System instructions
- `MaxTokens` (int, optional): Maximum tokens to generate
- `Temperature` (float, optional): Randomness control (0.0-1.0)

**Outputs:**
- `Result` (string): The generated response text
- `TotalTokens` (int): Total tokens used in the request
- `FinishReason` (string): Completion status

### Example Workflow Configuration
```json
{
  "ApiKey": "your-openai-key",
  "Model": "gpt-3.5-turbo",
  "Prompt": "Explain quantum computing in simple terms",
  "SystemMessage": "You are a helpful assistant that explains complex topics simply",
  "Temperature": 0.7,
  "MaxTokens": 200
}
```

## 🏗️ Architecture

### Core Components

- **`OpenAIActivity`**: Base class for all OpenAI activities
- **`OpenAIClientFactory`**: Thread-safe client factory with API key-based caching
- **`OpenAIFeature`**: Elsa feature for dependency injection setup
- **`CompleteChat`**: Chat completion activity implementation

### Client Management

The `OpenAIClientFactory` provides:
- Thread-safe client creation and caching
- Support for different OpenAI client types
- Efficient resource management
- API key-based client isolation

## 🔒 Security

- **No API keys in source code**: All keys stored in user secrets or environment variables
- **Gitignored environment files**: `.env.local` and similar files are excluded from version control
- **Secure client management**: API keys are handled securely throughout the application

## 🎯 Supported OpenAI APIs

Currently implemented:
- ✅ **Chat Completions** (GPT-3.5, GPT-4, etc.)

Ready for future expansion:
- 🔄 **Image Generation** (DALL-E)
- 🔄 **Audio Processing** (Whisper, TTS)
- 🔄 **Text Embeddings**
- 🔄 **Content Moderation**

## 🤝 Contributing

When adding new OpenAI activities:

1. Inherit from `OpenAIActivity`
2. Use the appropriate client from `OpenAIClientFactory`
3. Follow Elsa attribute patterns for inputs/outputs
4. Add corresponding unit tests
5. Update this documentation

## 📖 API Reference

### OpenAIActivity (Base Class)
Protected methods for accessing OpenAI clients:
- `GetClient(context)`: Get base OpenAI client
- `GetChatClient(context)`: Get chat-specific client
- `GetImageClient(context)`: Get image-specific client
- `GetAudioClient(context)`: Get audio-specific client
- `GetEmbeddingClient(context)`: Get embedding-specific client
- `GetModerationClient(context)`: Get moderation-specific client

### CompleteChat Activity
A complete implementation showing the pattern for OpenAI activities in Elsa workflows.
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
A workflow that processes support tickets and generates AI responses:

```csharp
public class CustomerSupportWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var customerQuery = builder.WithVariable<string>();
        var aiResponse = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                // Trigger on incoming support ticket
                new HttpEndpoint
                {
                    Path = new("/support/chat"),
                    SupportedMethods = new([HttpMethods.Post])
                },
                // Extract customer query from request
                new SetVariable
                {
                    Variable = customerQuery,
                    Value = new(context => context.GetInput<string>("query"))
                },
                // Generate AI response
                new CompleteChat
                {
                    SystemMessage = new("You are a helpful customer support agent. Be polite, professional, and provide clear solutions."),
                    Prompt = customerQuery,
                    Model = new("gpt-4"),
                    Temperature = new(0.3f),
                    Result = new(aiResponse)
                },
                // Return response to customer
                new WriteHttpResponse
                {
                    Content = new(context => new { response = aiResponse.Get(context) })
                }
            }
        };
    }
}
```

### Content Generation Pipeline
A workflow that generates marketing content based on product data:

```csharp
public class ContentGenerationWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var productInfo = builder.WithVariable<string>();
        var marketingCopy = builder.WithVariable<string>();
        var socialPost = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                // Read product information from database
                new SetVariable
                {
                    Variable = productInfo,
                    Value = new(context => GetProductDetails(context.GetInput<int>("productId")))
                },
                // Generate marketing copy
                new CompleteChat
                {
                    SystemMessage = new("Generate compelling marketing copy for our product. Focus on benefits and create urgency."),
                    Prompt = new(context => $"Product: {productInfo.Get(context)}\nTarget audience: Tech-savvy professionals"),
                    Model = new("gpt-3.5-turbo"),
                    MaxTokens = new(500),
                    Temperature = new(0.7f),
                    Result = new(marketingCopy)
                },
                // Generate social media version
                new CompleteChat
                {
                    SystemMessage = new("Create a concise, engaging social media post with hashtags."),
                    Prompt = new(context => $"Create a social post based on this copy: {marketingCopy.Get(context)}"),
                    Model = new("gpt-3.5-turbo"),
                    MaxTokens = new(280),
                    Temperature = new(0.8f),
                    Result = new(socialPost)
                },
                // Save content to CMS
                new WriteLine(context => $"Marketing Copy: {marketingCopy.Get(context)}"),
                new WriteLine(context => $"Social Post: {socialPost.Get(context)}")
            }
        };
    }
    
    private string GetProductDetails(int productId) => 
        $"Smart fitness tracker with heart rate monitoring, GPS, and 7-day battery life. Price: $199";
}
```

### Intelligent Document Processing
A workflow that analyzes uploaded documents and extracts key information:

```csharp
public class DocumentAnalysisWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var documentText = builder.WithVariable<string>();
        var extractedData = builder.WithVariable<string>();
        var classification = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                // File upload trigger
                new HttpEndpoint
                {
                    Path = new("/documents/analyze"),
                    SupportedMethods = new([HttpMethods.Post])
                },
                // Extract text from document
                new SetVariable
                {
                    Variable = documentText,
                    Value = new(context => ExtractTextFromDocument(context.GetInput<byte[]>("file")))
                },
                // Classify document type
                new CompleteChat
                {
                    SystemMessage = new("Classify the document type. Respond with only: INVOICE, CONTRACT, RESUME, or OTHER."),
                    Prompt = new(context => $"Document content: {documentText.Get(context)?.Substring(0, 1000)}"),
                    Model = new("gpt-3.5-turbo"),
                    MaxTokens = new(10),
                    Temperature = new(0.1f),
                    Result = new(classification)
                },
                // Extract structured data based on type
                new If
                {
                    Condition = new(context => classification.Get(context) == "INVOICE"),
                    Then = new CompleteChat
                    {
                        SystemMessage = new("Extract invoice details as JSON: {amount, date, vendor, invoiceNumber}"),
                        Prompt = documentText,
                        Model = new("gpt-4"),
                        Temperature = new(0.2f),
                        Result = new(extractedData)
                    },
                    Else = new CompleteChat
                    {
                        SystemMessage = new("Summarize the key points from this document in bullet format."),
                        Prompt = documentText,
                        Model = new("gpt-3.5-turbo"),
                        Temperature = new(0.3f),
                        Result = new(extractedData)
                    }
                },
                // Return analysis results
                new WriteHttpResponse
                {
                    Content = new(context => new 
                    {
                        documentType = classification.Get(context),
                        extractedData = extractedData.Get(context),
                        processingTime = DateTime.UtcNow
                    })
                }
            }
        };
    }
    
    private string ExtractTextFromDocument(byte[] fileData) => 
        "Sample extracted text from document...";
}
```

### Multi-Step Code Review Assistant
A workflow that performs comprehensive code analysis:

```csharp
public class CodeReviewWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var codeToReview = builder.WithVariable<string>();
        var securityAnalysis = builder.WithVariable<string>();
        var performanceReview = builder.WithVariable<string>();
        var finalSummary = builder.WithVariable<string>();
        
        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable
                {
                    Variable = codeToReview,
                    Value = new(context => context.GetInput<string>("code"))
                },
                // Parallel analysis
                new Fork
                {
                    JoinMode = ForkJoinMode.WaitAll,
                    Branches =
                    {
                        // Security analysis
                        new CompleteChat
                        {
                            SystemMessage = new("You are a security expert. Analyze code for vulnerabilities, injection risks, and security best practices."),
                            Prompt = codeToReview,
                            Model = new("gpt-4"),
                            Temperature = new(0.2f),
                            Result = new(securityAnalysis)
                        },
                        // Performance analysis
                        new CompleteChat
                        {
                            SystemMessage = new("You are a performance expert. Review code for efficiency, scalability, and optimization opportunities."),
                            Prompt = codeToReview,
                            Model = new("gpt-4"),
                            Temperature = new(0.2f),
                            Result = new(performanceReview)
                        }
                    }
                },
                // Generate comprehensive summary
                new CompleteChat
                {
                    SystemMessage = new("Create a comprehensive code review summary combining security and performance feedback. Provide actionable recommendations."),
                    Prompt = new(context => 
                        $"Code:\n{codeToReview.Get(context)}\n\n" +
                        $"Security Analysis:\n{securityAnalysis.Get(context)}\n\n" +
                        $"Performance Analysis:\n{performanceReview.Get(context)}"),
                    Model = new("gpt-4"),
                    Temperature = new(0.3f),
                    Result = new(finalSummary)
                },
                new WriteLine(context => $"Code Review Complete:\n{finalSummary.Get(context)}")
            }
        };
    }
}
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

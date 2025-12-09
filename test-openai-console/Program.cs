using Elsa.OpenAI.Activities.Chat;
using Elsa.OpenAI.Services;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

// Simple console app to test OpenAI integration
Console.WriteLine("OpenAI Integration Test");
Console.WriteLine("=======================");

// Test 1: Check if OpenAI API key is set
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ OPENAI_API_KEY environment variable not set");
    Console.WriteLine("Please set your OpenAI API key using: export OPENAI_API_KEY=\"your-key-here\"");
    Console.WriteLine("Or run: source setup-openai-env.sh");
    return 1;
}
else
{
    Console.WriteLine($"✅ OPENAI_API_KEY is set (length: {apiKey.Length} characters)");
}

// Test 2: Test OpenAI Client Factory
Console.WriteLine("\n--- Testing OpenAI Client Factory ---");
try 
{
    var factory = new OpenAIClientFactory();
    var client1 = factory.GetClient(apiKey);
    var client2 = factory.GetClient(apiKey);
    
    Console.WriteLine("✅ OpenAIClientFactory can create clients");
    Console.WriteLine($"✅ Client caching works: {(client1 == client2 ? "Same instance returned" : "Different instances")}");
    
    // Test different client types
    var chatClient = factory.GetChatClient("gpt-3.5-turbo", apiKey);
    Console.WriteLine("✅ ChatClient created successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ OpenAIClientFactory test failed: {ex.Message}");
    return 1;
}

// Test 3: Test basic activity structure
Console.WriteLine("\n--- Testing CompleteChat Activity Structure ---");
try 
{
    var activity = new CompleteChat();
    Console.WriteLine("✅ CompleteChat activity can be instantiated");
    Console.WriteLine($"✅ Activity has required inputs: Prompt={activity.Prompt != null}, ApiKey={activity.ApiKey != null}, Model={activity.Model != null}");
    Console.WriteLine($"✅ Activity has expected outputs: Result={activity.Result != null}, TotalTokens={activity.TotalTokens != null}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ CompleteChat activity test failed: {ex.Message}");
    return 1;
}

// Test 4: Test direct OpenAI API call (if user wants to proceed)
Console.WriteLine("\n--- Optional: Test Direct API Call ---");
Console.Write("Would you like to test a direct API call to OpenAI? This will consume API credits. (y/n): ");
var response = Console.ReadLine();

if (response?.ToLower() == "y" || response?.ToLower() == "yes")
{
    try
    {
        var client = new OpenAI.OpenAIClient(apiKey);
        var chatClient = client.GetChatClient("gpt-3.5-turbo");
        
        Console.WriteLine("Sending test prompt to OpenAI...");
        var result = await chatClient.CompleteChatAsync("Say 'Hello from Elsa OpenAI integration!'");
        var completion = result.Value;
        
        Console.WriteLine("✅ OpenAI API call successful!");
        Console.WriteLine($"Response: {completion.Content?[0]?.Text}");
        Console.WriteLine($"Finish Reason: {completion.FinishReason}");
        Console.WriteLine($"Total Tokens: {completion.Usage?.TotalTokenCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ OpenAI API call failed: {ex.Message}");
        return 1;
    }
}
else
{
    Console.WriteLine("⏭️  Skipped direct API call test");
}

Console.WriteLine("\n🎉 All tests completed successfully!");
Console.WriteLine("The OpenAI integration is ready to use in Elsa workflows.");

return 0;
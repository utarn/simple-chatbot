# ChatBot API

A comprehensive .NET 8 multi-platform chatbot framework with auto-discovery plugin architecture.

## Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd simple-chatbot
   ```

2. **Configure settings**
   - Update `src/Web/appsettings.json` with your database connection strings
   - Add API keys for OpenAI, LINE, Facebook as needed

3. **Run migrations**
   ```bash
   dotnet ef database update --project src/Infrastructure --startup-project src/Web
   ```

4. **Build and run**
   ```bash
   dotnet run --project src/Web
   ```

## Adding Custom Processors

Create a new processor class ending with `*Processor`:

```csharp
public class MyCustomProcessor : ILineMessageProcessor
{
    public string Name => Systems.MyCustom; // Add to Systems.cs
    public string Description => "My custom processor description";

    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, 
        string message, string userId, string replyToken, 
        CancellationToken cancellationToken = default)
    {
        // Your logic here
        return new LineReplyStatus 
        { 
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage> 
                { 
                    new LineTextMessage("Hello from my processor!") 
                }
            }
        };
    }

    public async Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, 
        string messageId, string userId, string replyToken, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return new LineReplyStatus { Status = 404 };
    }
}
```

The processor will be automatically discovered and registered - no manual configuration required!

## Documentation

See [index.md](index.md) for detailed documentation including:
- Architecture overview
- Complete feature list
- Migration guide for existing forks
- Technology stack details

## Recent Updates

**v2024.1**: Complete overhaul of plugin system - migrated from attribute-based to pattern-based auto-discovery. See migration guide in [index.md](index.md).

## License

This project is open source. Please refer to the license file for details.
# ChatBot API - Multi-Platform Chatbot Framework

## Project Overview

ChatBot API is a comprehensive .NET 8 chatbot framework that supports multiple messaging platforms including LINE, Facebook Messenger, and OpenAI integrations. The system features a plugin-based architecture that allows developers to easily extend functionality through custom processors.

## Key Features

- **Multi-Platform Support**: LINE Bot, Facebook Messenger, OpenAI Chat Completions
- **Plugin Architecture**: Extensible processor system for custom functionality
- **Auto-Discovery System**: Automatic plugin registration without manual configuration
- **Vector Search**: Semantic search capabilities using OpenAI embeddings
- **File Management**: Upload, store, and search files with AI-powered descriptions
- **Contact Management**: Organize and associate files with contacts
- **Database Integration**: PostgreSQL with Entity Framework Core
- **Caching**: Redis distributed caching support
- **Authentication**: Identity-based authentication system

## Architecture

### Core Components

- **Web Layer** (`src/Web/`): MVC controllers, Razor views, and API endpoints
- **Application Layer** (`src/Application/`): Business logic, queries, commands, and interfaces using MediatR pattern
- **Infrastructure Layer** (`src/Infrastructure/`): External service implementations, data access, and processors
- **Domain Layer** (`src/Domain/`): Entities, constants, and domain models

### Key Services & Interfaces

#### Core Services
- **`ICurrentUserService`**: Get current user ID for authentication context
- **`IApplicationDbContext`**: Database abstraction layer using Entity Framework Core
- **`ISystemService`**: Provides date/time utilities and full hostname (do not modify)
- **`IWebHostEnvironment`**: Access to system paths and environment information

#### Messaging & Communication
- **`ILineMessageProcessor`**: Core interface for LINE chatbot processors (auto-discovered)
- **`ILineEmailProcessor`**: Processes emails for LINE messaging platform
- **`IPreProcessor`**: Pre-process incoming user messages. Implementations are auto-discovered and invoked before chat completion; any returned OpenAIMessage will be appended immediately after the system role message (no UI required).
- **`IPostProcessor`**: Handles post-processing of messages and events

#### External Integrations
- **`IOpenAiService`**: LLM chat completions and embeddings, it use default model name and api key from a chatbot
- **`IGmailService`**: Gmail operations and email management
- **`ICalendarService`**: Google Calendar operations and booking

#### Caching & Performance
- **`IMemoryCache`**: In-memory caching for session data
- **`IDistributedCache`**: Distributed caching (Redis) with `IDistributedCacheExtension`

#### Background Services
- **`FetchEmailBackgroundService`**: Reads Gmail and pushes to processing queue
- Inherit from `BackgroundService` for custom background tasks

### Plugin System

The system uses an **auto-discovery plugin architecture** where processors are automatically registered based on:

- **Class Name Pattern**: Classes ending with `*Processor`
- **Interface Implementation**: Must implement `ILineMessageProcessor` or `IFacebookMessengerProcessor`
- **Properties**: Must have `Name` and `Description` properties

#### Available Processors

- **`EchoProcessor`**: Echo Bot with timestamp responses
- **`GoldReportProcessor`**: Processes HTML content from websites for gold price reporting
- **`ReadCodeProcessor`**: QR Code and Barcode reading functionality
- **`ReadImageProcessor`**: Image analysis and OCR processing
- **`ReceiptProcessor`**: Processes image OCR messages for receipt generation
- **`TrackFileProcessor`**: File upload, download, and embedding management
- **`CheckCheatOnlineProcessor`**: Uses OpenRouter provider, processes messages and calls 3rd party REST APIs
- **`FormT1Processor`**: Processes messages with LLM to JSON and generates PDF files
- **`LLamaPassportProcessor`**: Processes passport images with field matching and stores in Google Sheets
- **`BookingProcessor`**: Processes messages with LLM and books Google Calendar appointments
- **`UserDefinedProcessor`**: Custom JSON/Flex message handling
- **`ExampleLineEmailProcessor`**: Reads email and pushes messages to LINE

#### Protected Services
**Do not modify**: `SystemService`, `VectorChatService`, `LineMessagingApi`

## Technology Stack

- **.NET 8**: Modern C# web application framework
- **ASP.NET Core MVC**: Web framework for UI and API
- **Entity Framework Core**: Object-relational mapping
- **PostgreSQL**: Primary database with vector extensions (pgvector)
- **Redis**: Distributed caching
- **OpenAI API**: LLM integrations and embeddings
- **LINE Messaging API**: LINE Bot functionality
- **Facebook Graph API**: Messenger Bot functionality

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Redis (optional, for caching)
- OpenAI API key
- LINE Channel Access Token (for LINE Bot)
- Facebook App credentials (for Messenger Bot)

### Installation

1. Clone the repository
2. Configure connection strings in `appsettings.json`
3. Run database migrations
4. Configure external API keys
5. Build and run the application

## Development Guidelines

### Design Patterns

- **MediatR Pattern**: Use MediatR pattern in MVC actions for clean separation of concerns
- **AutoMapper**: Use AutoMapper and MappingExtensions for LINQ mapping
- **Repository Pattern**: Use `IApplicationDbContext` for data access
- **Identity**: Use ASP.NET Core Identity for user management

### Creating Views & Controllers

For CRUD operations (paging, delete, create, list, detail pages), reference the **Chatbots Controller and Views** as implementation examples.

### Mapping ViewModels

Create private classes that inherit from `Profile` for AutoMapper configuration:

```csharp
private class YourMappingProfile : Profile
{
    public YourMappingProfile()
    {
        CreateMap<YourEntity, YourViewModel>();
        CreateMap<YourViewModel, YourEntity>();
    }
}
```

### Database Migrations

- **Project file**: Located in `Infrastructure` project
- **Startup project**: `Web` project
- **Command**: `dotnet ef migrations add YourMigrationName --project src/Infrastructure --startup-project src/Web`

### Adding Custom Processors

To add a new processor:

```csharp
namespace YourNamespace.Processors.YourProcessor
{
    public class YourCustomProcessor : ILineMessageProcessor
    {
        private readonly IApplicationDbContext _context;
        private readonly IOpenAiService _openAiService;
        private readonly ILogger<YourCustomProcessor> _logger;

        public YourCustomProcessor(
            IApplicationDbContext context,
            IOpenAiService openAiService,
            ILogger<YourCustomProcessor> logger)
        {
            _context = context;
            _openAiService = openAiService;
            _logger = logger;
        }

        public string Name => Systems.YourCustom; // Add constant to Systems.cs
        public string Description => "Your processor description";

        public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId,
            string message, string userId, string replyToken,
            CancellationToken cancellationToken = default)
        {
            // Your processing logic here
            // Use injected services for database, AI, logging, etc.
            
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage("Your response message")
                    }
                }
            };
        }

        public async Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId,
            string messageId, string userId, string replyToken, string accessToken,
            CancellationToken cancellationToken = default)
        {
            // Your image processing logic here
            return new LineReplyStatus { Status = 404 };
        }
    }
}
```

The system will automatically discover and register your processor without requiring manual configuration.

### Adding PreProcessors

A PreProcessor lets you inspect and transform or augment the incoming user message before a chat completion is requested. All implementations of `IPreProcessor` are discovered automatically (via the same auto-discovery in DependencyInjection) and invoked for every chat request. Each implementation may return an `OpenAIMessage` which will be appended immediately after the `system` role message in the messages list sent to the LLM. No UI changes are required to use this feature.

Example implementation:

```csharp
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;

public class ExampleLinePreProcessor : IPreProcessor
{
    public async Task<OpenAIMessage?> PreProcessAsync(string userId, string messageText, CancellationToken cancellationToken = default)
    {
        // Example: prepend a note about the user (only include for logged-in users)
        if (!string.IsNullOrEmpty(userId))
        {
            return new OpenAIMessage
            {
                Role = "user",
                Content = $"[User {userId} is a premium user. Incoming message: {messageText}]"
            };
        }

        return null; // no message to add
    }
}
```

Notes:
- Returned `OpenAIMessage` can use any role supported by your LLM integration (commonly "user" or "system").
- Exceptions thrown by a pre-processor are logged and do not stop the main chat flow.
- If you require a deterministic execution order for pre-processors, add an ordering mechanism (e.g., an Order property) to your implementations and update `DependencyInjection` to sort them when resolving.

### Adding Background Services

```csharp
public class YourBackgroundService : BackgroundService
{
    private readonly ILogger<YourBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public YourBackgroundService(
        ILogger<YourBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your background task logic
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

Register in `DependencyInjection.cs`:
```csharp
services.AddHostedService<YourBackgroundService>();
```

---

## Recent Updates

### Plugin Auto-Discovery System Overhaul (v2024.1)

**Breaking Changes**: The plugin registration system has been completely redesigned from attribute-based to pattern-based discovery.

#### What Changed

**Before (Attribute-Based)**:
```csharp
[Plugin("Description here")]
public class MyProcessor : ILineMessageProcessor
{
    public string Name => Systems.MyPlugin;
    // ...
}
```

**After (Pattern-Based)**:
```csharp
public class MyProcessor : ILineMessageProcessor  // Must end with "Processor"
{
    public string Name => Systems.MyPlugin;
    public string Description => "Description here";  // Now a property
    // ...
}
```

#### Migration Steps for Forks

If you have forked this project, follow these steps to update your code:

1. **Update Interface Implementations**
   - Add `Description` property to all your custom processors
   - Remove `[Plugin("...")]` attributes from processor classes
   - Remove `using ChatbotApi.Application.Common.Attributes;` imports

2. **Update Processor Classes**
   ```csharp
   // Remove this line
   // [Plugin("Your description")]
   
   public class YourProcessor : ILineMessageProcessor
   {
       public string Name => Systems.YourPlugin;
       // Add this line
       public string Description => "Your description";
   }
   ```

3. **Ensure Class Naming**
   - All processor classes must end with `*Processor` (e.g., `EmailProcessor`, `PaymentProcessor`)
   - Must implement `ILineMessageProcessor` or `IFacebookMessengerProcessor`

4. **Remove Obsolete Files** (Optional)
   - Delete `src/Application/Common/Attributes/PluginAttribute.cs` if not used elsewhere
   - Update any hardcoded plugin references to use the new discovery system

5. **Test Auto-Discovery**
   - Build and run your application
   - Verify all processors appear in the chatbot management UI
   - Check that new processors are automatically detected

#### Benefits of New System

- **No More Manual Registration**: Processors are discovered automatically
- **Simpler Code**: No need for attributes or complex registration
- **Better Performance**: Discovery happens once at startup
- **Easier Maintenance**: Less boilerplate code
- **Type Safety**: Still uses constants from Systems.cs

#### Troubleshooting

- **Processor not appearing**: Ensure class name ends with "Processor" and implements required interface
- **Missing description**: Add `Description` property to your processor class
- **Build errors**: Remove old `[Plugin]` attributes and `using` statements

This update significantly simplifies plugin development while maintaining all existing functionality.
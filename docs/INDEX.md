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
- **Caching**: In-memory caching support
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
- **`IFacebookMessengerProcessor`**: Core interface for Facebook Messenger chatbot processors (auto-discovered)
- **`ILineEmailProcessor`**: Processes emails for LINE messaging platform
- **`IPreProcessor`**: Pre-process incoming user messages. Implementations are auto-discovered and invoked before chat completion; any returned OpenAIMessage will be appended immediately after the system role message (no UI required).
- **`IPostProcessor`**: Handles post-processing of messages and events

#### External Integrations
- **`IOpenAiService`**: LLM chat completions and embeddings, it use default model name and api key from a chatbot
- **`IGmailService`**: Gmail operations and email management
- **`ICalendarService`**: Google Calendar operations and booking

#### Caching & Performance
- **`IMemoryCache`**: In-memory caching for session data

#### Background Services
- **`FetchEmailBackgroundService`**: Reads Gmail and pushes to processing queue
- Inherit from `BackgroundService` for custom background tasks

### Plugin System

The system uses an **auto-discovery plugin architecture** where processors are automatically registered based on:

- **Class Name Pattern**: Classes ending with `*Processor`
- **Interface Implementation**: Must implement `ILineMessageProcessor`, `IFacebookMessengerProcessor`, or other processor interfaces
- **Attribute Declaration**: Must use `[Processor("Name", "Description")]` attribute for metadata

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
- **OpenAI API**: LLM integrations and embeddings
- **LINE Messaging API**: LINE Bot functionality
- **Facebook Graph API**: Messenger Bot functionality

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL database
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

#### Creating a LINE Message Processor

To add a new LINE processor:

```csharp
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;

namespace YourNamespace.Processors.YourProcessor
{
    [Processor("YourCustom", "Your processor description")]
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

#### Creating a Facebook Messenger Processor

To add a new Facebook Messenger processor:

```csharp
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

namespace YourNamespace.Processors.YourProcessor
{
    [Processor("YourFacebookCustom", "Your Facebook processor description")]
    public class YourFacebookCustomProcessor : IFacebookMessengerProcessor
    {
        private readonly IApplicationDbContext _context;
        private readonly IOpenAiService _openAiService;
        private readonly ILogger<YourFacebookCustomProcessor> _logger;

        public YourFacebookCustomProcessor(
            IApplicationDbContext context,
            IOpenAiService openAiService,
            ILogger<YourFacebookCustomProcessor> logger)
        {
            _context = context;
            _openAiService = openAiService;
            _logger = logger;
        }

        public async Task<FacebookReplyStatus> ProcessFacebookAsync(int chatbotId, string message, string userId,
            CancellationToken cancellationToken = default)
        {
            // Your processing logic here
            // Use injected services for database, AI, logging, etc.
            
            return new FacebookReplyStatus
            {
                Status = 200,
                ReplyMessage = new List<FacebookReplyMessage>
                {
                    new FacebookReplyMessage
                    {
                        Recipient = new FacebookUser { Id = userId },
                        Message = new TextFacebookMessage { Text = "Your response message" }
                    }
                }
            };
        }

        public async Task<FacebookReplyStatus> ProcessFacebookImageAsync(int chatbotId, List<FacebookAttachment>? attachments, string userId,
            CancellationToken cancellationToken = default)
        {
            // Your image processing logic here
            return new FacebookReplyStatus { Status = 404 };
        }
    }
}
```

#### Auto-Discovery System

The system automatically discovers and registers processors based on:

1. **Naming Convention**: Class name must end with `*Processor`
2. **Interface Implementation**: Must implement one of:
   - `ILineMessageProcessor` for LINE Bot processors
   - `IFacebookMessengerProcessor` for Facebook Messenger processors
   - `ILineEmailProcessor` for email processors
   - `IPreProcessor` for pre-processing
   - `IPostProcessor` for post-processing
3. **Attribute Declaration**: Must use `[Processor("Name", "Description")]` attribute
4. **Dependency Injection**: Processors are automatically registered in DI container

**File Structure Convention:**
```
src/Infrastructure/Processors/
├── YourProcessor/
│   ├── YourProcessor.cs
│   └── (helper classes if needed)
```

No manual registration is required - the system will automatically discover and register your processor.

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

## Coding Style & Best Practices

- **MediatR Pattern**: Use MediatR pattern in MVC actions for clean separation of concerns
- **AutoMapper**: Use AutoMapper and MappingExtensions for LINQ mapping
- **Repository Pattern**: Use `IApplicationDbContext` for data access
- **Views & Controllers**: For CRUD operations (paging, delete, create, list, detail pages), reference the **Chatbots Controller and Views** as implementation examples
- **Database Migrations**: Project file is in Infrastructure, startup project is Web
- **User Management**: Use ASP.NET Core Identity
- **Background Tasks**: Inherit from `BackgroundService`
- **Dependency Injection**: Any created service needs to be registered in `DependencyInjection` in Infrastructure
- **Processor Integration**: The implementation class will be associated with a chatbot that enables this plugin. You can use LLM key and model name of the chatbot to feed `IOpenAiService`

**IMPORTANT**: DO NOT REMOVE EXISTING FUNCTIONALITIES.

---

## Recent Updates

### Plugin Auto-Discovery System (Current Implementation)

The system uses **attribute-based auto-discovery** for processor registration, providing a clean and efficient way to register processors without manual configuration.

#### Current System Features

**Processor Declaration**:
```csharp
[Processor("ProcessorName", "Processor description")]
public class MyProcessor : ILineMessageProcessor  // Must end with "Processor"
{
    // Implementation...
}
```

#### Auto-Discovery Mechanism

The system automatically discovers and registers processors based on:

1. **Class Name Pattern**: Classes ending with `*Processor`
2. **Interface Implementation**: Must implement one of the supported processor interfaces:
   - `ILineMessageProcessor` for LINE Bot processors
   - `IFacebookMessengerProcessor` for Facebook Messenger processors
   - `ILineEmailProcessor` for email processing
   - `IPreProcessor` for message pre-processing
   - `IPostProcessor` for message post-processing
3. **Attribute Declaration**: Must use `[Processor("Name", "Description")]` attribute

#### Registration Process

In `DependencyInjection.cs`, the system automatically registers processors:

```csharp
// Automatically register all ILineMessageProcessor implementations
var processorTypes = typeof(DependencyInjection).Assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && typeof(ILineMessageProcessor).IsAssignableFrom(t));
foreach (var type in processorTypes)
{
    services.AddScoped(typeof(ILineMessageProcessor), type);
}

// Similarly for Facebook processors
var facebookProcessorTypes = typeof(DependencyInjection).Assembly.GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && typeof(IFacebookMessengerProcessor).IsAssignableFrom(t));
foreach (var type in facebookProcessorTypes)
{
    services.AddScoped(typeof(IFacebookMessengerProcessor), type);
}
```

#### Benefits of Current System

- **No Manual Registration**: Processors are discovered automatically at startup
- **Clean Metadata**: `[Processor]` attribute provides clear name and description
- **Type Safety**: Strong typing with interface implementations
- **Dependency Injection**: Full DI support for all processor dependencies
- **Multiple Platform Support**: Unified system for LINE, Facebook, and other platforms

#### Requirements for New Processors

1. **Naming**: Class name must end with `*Processor`
2. **Interface**: Must implement appropriate processor interface
3. **Attribute**: Must use `[Processor("Name", "Description")]` attribute
4. **Location**: Place in `src/Infrastructure/Processors/YourProcessor/` directory
5. **Dependencies**: Use constructor injection for services

#### Troubleshooting

- **Processor not appearing**: Ensure class name ends with "Processor" and has `[Processor]` attribute
- **Interface errors**: Verify you're implementing the correct interface for your platform
- **DI errors**: Check that all constructor dependencies are registered in DI container

This system provides a balance between automatic discovery and explicit metadata declaration.
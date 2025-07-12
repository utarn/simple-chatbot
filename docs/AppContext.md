### Application Context ####
ICurrentUserService: Get user id.
IGmailService: Gmail operations.
ICalendarService: Google calendar operations.
IPostProcessor: Handles post-processing of messages/events.
IOpenAiService: LLM chat completion, located in lib/OpenAiService with another namespace.
ILineEmailProcessor: Processes emails for the Line messaging platform.
ISystemService: Provide date/time and full hostname.
ILineMessageProcessor: Processes messages for the Line platform, line chat bot, you can see implementation of this interface for sample and follow their patterns.
IApplicationDbContext: Abstraction over the application's database context.
ApplicationDbContextInitialiser: the place to initialize data in database.
Every entities need to be configured using IEntityTypeConfiguration.

For user-management, use ASP.NET Core Identity 
For background tasks, inherit from BackgroundService.
FetchEmailBackgroundService: Read gmail and push to the queue.
The implementation class will be associated with a chatbot that enable this plugin. You can use llmkey and model name of the chatbot to feed IOpenAiService. The Name property of the processor is required to be registered in Systems.cs.
Any created service need to be registered in dependencyInjection in infrastructure.
You can see sample from another implementations of ILineMessageProcessor for coding patterns.
DO NOT REMOVE EXISTING FUNCTIONALITIES.

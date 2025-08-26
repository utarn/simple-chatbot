### Services in the application ####
ICurrentUserService: Get user id.
IGmailService: Gmail operations.
ICalendarService: Google calendar operations.
IPostProcessor: Handles post-processing of messages/events.
IOpenAiService: LLM chat completion.
ILineEmailProcessor: Processes emails for the Line messaging platform.
ISystemService: Provide date/time and full hostname.
ILineMessageProcessor: Processes messages for the Line chatbot platform, you can see implementation of this interface for sample and follow their patterns. Line chatbot must implement ILineMessageProcessor. All implementations must be registered in plugins Systems.cs
IApplicationDbContext: Abstraction over the application's database context.
IMemoryCache or IDistributedCache: for caching information with IDistributedCacheExtension
IWebHostEnvironment: provide access to system path

For user-management, use ASP.NET Core Identity 
Do not modify SystemService,VectorChatService,LineMessagingApi
For background tasks, inherit from BackgroundService.
FetchEmailBackgroundService: Read gmail and push to the queue.

## Example ###
TrackFileProcessor: file upload download embedding.
ReceiptProcessor: Processes image OCR message.
CheckCheatOnlineProcessor: Use OpenRouter provider Processes messages, call 3rd party rest api.
GoldReportProcessor: Processes html content from website and response messages.
LLamaPassportProcessor: Processes image with matching field and store in google sheet.
FormT1Processor: Processes message with llm to JSON and generate PDF file.
BookingProcessor: process message with llm and book google calendar.
ExampleLineEmailProcessor: read email and push message to line.


### Coding style ###
Use MediaTR pattern in MVC action. 
For mapping ViewModel, just create private class that inherits Profile.
Use AutoMapper and MappingExtensions for LINQ mapping.
For the paging view, delete, create, list, detail page, look up Chatbots Controller and View for references.
For migrations, project file is in Infrastructure, start up project is Web.
For user-management, use ASP.NET Core Identity 
For background tasks, inherit from BackgroundService.
FetchEmailBackgroundService: Read gmail and push to the queue.
The implementation class will be associated with a chatbot that enable this plugin. You can use llmkey and model name of the chatbot to feed IOpenAiService. The Name property of the processor is required to be registered in Systems.cs.
Any created service need to be registered in dependencyInjection in infrastructure.
You can see sample from another implementations of ILineMessageProcessor for coding patterns.
DO NOT REMOVE EXISTING FUNCTIONALITIES.

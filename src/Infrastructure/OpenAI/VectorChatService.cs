using System.Globalization;
using System.Text;
using ChatbotApi.Application.Common.Exceptions;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Entities;
using ChatbotApi.Domain.Enums;
using ChatbotApi.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using OpenAiService.Interfaces;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ChatbotApi.Infrastructure.OpenAI;

public class VectorChatService : IChatCompletion
{
    private readonly IApplicationDbContext _context;
    private readonly ISystemService _systemService;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<VectorChatService> _logger;
    private readonly IEnumerable<IPostProcessor> _postProcessors;
    private readonly IEnumerable<ISuggestionProcessor> _postSuggestionProcessors;

    public VectorChatService(IApplicationDbContext context, ISystemService systemService,
        IOpenAiService openAiService, ILogger<VectorChatService> logger,
        IEnumerable<IPostProcessor> postProcessors,
        IEnumerable<ISuggestionProcessor> postSuggestionProcessors)
    {
        _context = context;
        _systemService = systemService;
        _openAiService = openAiService;
        _logger = logger;
        _postProcessors = postProcessors;
        _postSuggestionProcessors = postSuggestionProcessors;
    }

    public async Task<ChatCompletionModel> ChatCompleteAsync(
        int chatbotId, string userId, string messageText,
        List<OpenAIMessage>? buffered, MessageChannel channel,
        int? max_tokens = null, decimal? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var chatbot = await _context.Chatbots
            .Include(c => c.ChatbotPlugins)
            .Include(c => c.PredefineMessages)
            .Where(c => c.Id == chatbotId)
            .FirstAsync(cancellationToken);

        if (chatbot.PredefineMessages.Count == 0)
        {
            throw new ChatCompletionException(500, "ยังไม่ได้ตั้งค่าแซทบอท");
        }

        if (chatbot.LlmKey == null)
        {
            throw new ChatCompletionException(500, "ยังไม่ได้ตั้งค่า API Key");
        }

        var toSendMessage = new List<OpenAIMessage>();
        if (chatbot.SystemRole != null)
        {
            toSendMessage.Add(new OpenAIMessage() { Role = "system", Content = chatbot.SystemRole });
        }

        var context = """
                      - เพื่อให้คุณเข้าใจบริบทเวลา วันปัจจุบันในประเทศไทย คือ %DATE%
                      - เวลาที่ให้ข้อมูลอยู่ในรูปแบบ d MMMM yyyy เป็นปีพุทธศักราช (+543)
                      - คุณจะไม่ตอบคำถามเกี่ยวกับโมเดลที่คุณทำงานอยู่ หรือข้อมูลเกี่ยวกับ LLM หรือ Generative AI
                      """;

        toSendMessage.Add(new OpenAIMessage()
        {
            Role = "user",
            Content = context.Replace("%DATE%",
                _systemService.Now.ToString("d MMMM yyyy HH:mm:ssน.", CultureInfo.GetCultureInfo("th-TH")))
        });

        List<MessageHistory> messageHistories = new();
        string concatenatedText = messageText;

        if (!string.IsNullOrEmpty(userId))
        {
            var referenceTime =
                _systemService.UtcNow.AddMinutes(-chatbot.HistoryMinute ?? -15);
            messageHistories = await _context.MessageHistories
                .Where(m => m.ChatBotId == chatbot.Id
                            && m.Channel == channel
                            && m.UserId == userId
                            && m.Created >= referenceTime)
                .OrderBy(m => m.Created)
                .ToListAsync(cancellationToken);

            concatenatedText = string.Join(" ", messageHistories.Select(m => m.Message));
            concatenatedText += "\n\n" + messageText;
        }
        else if (buffered is { Count: > 0 })
        {
            concatenatedText = string.Join("\n", buffered);
        }

        if (concatenatedText.Length > 8000)
        {
            concatenatedText = concatenatedText.Substring(concatenatedText.Length - 8000);
        }

        List<Tuple<string, string>> listOfReferences = new();
        List<FileAttachment> listOfAttachments = new();
        var topK = chatbot.TopKDocument ?? 4;
        var requiredPredefineMessages = chatbot.PredefineMessages
            .Where(p => p.ChatBotId == chatbotId && p.IsRequired)
            .OrderByDescending(p => p.Order)
            .ToList();
        topK -= requiredPredefineMessages.Count;


        var data = new List<PreMessage>();
        if (topK > 0)
        {
            var currentEmbedding =
                await _openAiService.CallEmbeddingsAsync(concatenatedText, chatbot.LlmKey, cancellationToken);
            if (currentEmbedding == null)
            {
                return new ChatCompletionModel()
                {
                    Message = "(ขออภัยระบบขัดข้อง กรุณารอสักครู่แล้วส่งข้อความใหม่อีกครั้ง)",
                    ReferenceItems = new List<ReferenceItem>(),
                    Suggestions = new List<string>()
                };
            }

            var maxDistance = chatbot.MaximumDistance ?? 2;
            var ignoredOrder = requiredPredefineMessages.Select(p => p.Order).ToList();

            var preFilteredQuery = _context.PreMessages
                .Where(p => p.ChatBotId == chatbotId && p.Embedding != null)
                .Where(p => ignoredOrder.All(r => r != p.Order))
                .Select(p => new PreMessageResult // Use the named class
                {
                    PreMessage = p,
                    Score = p.Embedding!.L2Distance(currentEmbedding),
                    GroupNumber = null // Initialize GroupNumber here
                });

            // Log the generated SQL
            // var sqlQuery = preFilteredQuery.ToQueryString();
            // _logger.LogInformation("Generated SQL for preFilteredMessages: {SqlQuery}", sqlQuery);

            var preFilteredMessages = await preFilteredQuery.ToListAsync(cancellationToken);

            var allPredefineMessages = preFilteredMessages
                .Where(x => x.Score <= maxDistance)
                .OrderBy(x => x.Score)
                .Take(topK)
                .ToList();

            // foreach (var preMessage in allPredefineMessages)
            // {
            //     _logger.LogWarning(
            //         "Score: {Score} Group: {GroupNumber} UserMessage: {UserMessage} FileName: {FileName} MimeType: {MimeType}",
            //         preMessage.Score,
            //         preMessage.GroupNumber, preMessage.PreMessage.UserMessage,
            //         preMessage.PreMessage.FileName, preMessage.PreMessage.FileMimeType);
            // }

            data = allPredefineMessages.Select(p => p.PreMessage).ToList();

            listOfReferences = data
                .Where(p => p is { FileHash: not null, FileName: not null })
                .Select(p => new Tuple<string, string>(p.FileName!, p.FileHash!))
                .Distinct()
                .ToList();
        }

        foreach (PreMessage required in requiredPredefineMessages)
        {
            data.Insert(0, required);
        }

        foreach (var preMessage in data)
        {
            if (preMessage is { UseCag: true })
            {
                var messageContent = await _context.PreMessageContents
                    .Where(p => p.Id == preMessage.PreMessageContentId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (messageContent == null)
                {
                    continue;
                }

                var base64 = Convert.ToBase64String(messageContent.Content);
                listOfAttachments.Add(new FileAttachment()
                {
                    Base64 = base64,
                    MimeType = preMessage.FileMimeType ?? "application/pdf",
                });
                continue;
            }

            var userMessage = preMessage.UserMessage;
            var assistantMessage = preMessage.AssistantMessage;
            toSendMessage.Add(new OpenAIMessage() { Role = "user", Content = userMessage });
            if (!string.IsNullOrEmpty(assistantMessage))
            {
                toSendMessage.Add(new OpenAIMessage() { Role = "assistant", Content = assistantMessage });
            }
        }

        if (chatbot.AllowOutsideKnowledge || chatbot.ResponsiveAgent || chatbot.EnableWebSearchTool)
        {
            var userMessage = "\n\n#### OUTPUT FORMAT ####\n";

            if (chatbot.ResponsiveAgent)
            {
                userMessage += """
                               - หลังจากที่ให้คำตอบ คุณจะสอบถามว่าผู้ใช้ต้องการข้อมูลด้านไหน 
                               - คุณจะตอบคำถามแค่คำถามโดยไม่ได้ให้ข้อมูลอื่นหากไม่สอบถาม

                               """;
            }

            if (!chatbot.AllowOutsideKnowledge)
            {
                userMessage +=
                    "\n- คุณจะตอบคำถามตรงไม่มีคำอธิบายของคำตอบ และคุณจะไม่ตอบคำถามเรื่องอื่นที่ไม่ใช่ \"ข้อมูลที่คุณทราบ\" เท่านั้น";
            }
            else
            {
                userMessage +=
                    "\n- นอกจากข้อมูลที่ป้อนให้ คุณจะตอบคำถามโดยใช้ความรู้ที่โมเดลคุณมีเพื่อให้ข้อมูลมีความสมบูรณ์ขึ้น";
            }

            if (chatbot.EnableWebSearchTool)
            {
                userMessage +=
                    "\n- คุณจะใช้ Web Search Tool เพื่อค้นหาข้อมูลเพิ่มเติมให้ได้มากที่สุด";
            }

            toSendMessage.Add(new OpenAIMessage() { Role = "user", Content = userMessage });
        }

        // Add message history to toSendMessage
        foreach (var message in messageHistories)
        {
            toSendMessage.Add(new OpenAIMessage() { Role = message.Role, Content = message.Message });
        }

        if (string.IsNullOrEmpty(userId) && buffered != null)
        {
            foreach (OpenAIMessage openAiMessage in buffered.Where(b => b.Role != "system"))
            {
                toSendMessage.Add(new OpenAIMessage()
                {
                    Role = openAiMessage.Role == "user" ? "user" : "model",
                    Content = openAiMessage.Content
                });
            }

            toSendMessage.AddRange(buffered);
        }
        else
        {
            toSendMessage.Add(new OpenAIMessage() { Role = "user", Content = messageText });
        }

        //DO NOT STORE MESSAGE if userId is empty
        if (!string.IsNullOrEmpty(userId)) // Only store if there's a userId
        {
            var newMessage = new MessageHistory()
            {
                ChatBotId = chatbotId,
                Created = _systemService.UtcNow,
                Channel = channel,
                Role = "user",
                UserId = userId,
                Message = messageText,
                IsProcessed = false
            };

            await _context.MessageHistories.AddAsync(newMessage, cancellationToken);
            toSendMessage.Add(new OpenAIMessage() { Role = newMessage.Role, Content = newMessage.Message });
        }

        var request = new OpenAiRequest()
        {
            Messages = toSendMessage,
            Files = listOfAttachments,
            Model = chatbot.ModelName ?? "openai/gpt-4.1",
            MaxTokens = max_tokens,
            Temperature = temperature
        };

        if (chatbot.EnableWebSearchTool)
        {
            request.WebSearchOptions = new WebSearchOption();
        }
        else
        {
            request.WebSearchOptions = null;
        }

        var apiResponse = await _openAiService.GetOpenAiResponseAsync(request, chatbot.LlmKey, cancellationToken);

        if (apiResponse is not { Choices.Count: > 0 })
        {
            throw new ChatCompletionException(500, "Invalid response from Chat completion API");
        }

        var choice = apiResponse.Choices[0];
        var aiResponseText = choice.Message.Content;
        var postProcessingTask = Task.Run(async () =>
        {
            var processedTextBuilder = new StringBuilder(aiResponseText);
            var processedText = processedTextBuilder.ToString();
            if (string.IsNullOrEmpty(processedText))
            {
                return processedText;
            }

            foreach (IPostProcessor postProcessor in _postProcessors)
            {
                if (chatbot.ChatbotPlugins.Any(c => c.PluginName == postProcessor.Name))
                {
                    processedText =
                        await postProcessor.ProcessResponse(processedText, chatbot, userId, cancellationToken);
                    break;
                }
            }

            return processedText;
        }, cancellationToken);

        // Wait for both tasks to complete
        await Task.WhenAll(postProcessingTask);

        // Get the results
        aiResponseText = await postProcessingTask;
        var references = new List<ReferenceItem>();
        foreach (var annotation in choice.Message.Annotations)
        {
            references.Add(new ReferenceItem()
            {
                Title = annotation.UrlCitation.Title,
                Url = annotation.UrlCitation.Url,
                StartIndex = annotation.UrlCitation.StartIndex,
                EndIndex = annotation.UrlCitation.EndIndex,
                LogoPath = annotation.UrlCitation.LogoPath,
                Text = annotation.UrlCitation.Text
            });
        }

        foreach (var reference in listOfReferences)
        {
            references.Add(new ReferenceItem()
            {
                Title = reference.Item1,
                Url = _systemService.FullHostName + "/i/" + reference.Item2,
            });
        }

        var returnedResponse = new StringBuilder(aiResponseText);

        if (chatbot.ShowReference == true && references.Any())
        {
            returnedResponse.AppendLine();
            returnedResponse.AppendLine();
            returnedResponse.AppendLine("อ้างอิง:");
            var i = 1;
            foreach (var reference in references.Select(r => new { r.Title, r.Url }).Distinct())
            {
                returnedResponse.AppendLine(
                    $"{i++}. {reference.Title} {reference.Url}");
            }
        }

        // DO NOT STORE AI RESPONSE if userId is empty
        if (!string.IsNullOrEmpty(userId)) //Only store if userID provided
        {
            var aiMessage = new MessageHistory()
            {
                ChatBotId = chatbotId,
                Created = _systemService.UtcNow,
                Channel = channel,
                Role = "assistant",
                UserId = userId,
                Message = aiResponseText,
                IsProcessed = true
            };
            await _context.MessageHistories.AddAsync(aiMessage, cancellationToken);
        }

        // No need to save changes if userId is empty, as nothing was added to the context.
        if (!string.IsNullOrEmpty(userId))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ChatCompletionModel() { Message = returnedResponse.ToString(), ReferenceItems = references, };
    }

    private static double CalculateL2Distance(Vector vector1, Vector vector2)
    {
        var array1 = vector1.Memory.Span;
        var array2 = vector2.Memory.Span;

        if (array1.Length != array2.Length)
        {
            throw new ArgumentException("Vectors must have the same dimension");
        }

        double sum = 0;
        for (int i = 0; i < array1.Length; i++)
        {
            double diff = array1[i] - array2[i];
            sum += diff * diff;
        }

        return Math.Sqrt(sum);
    }
}

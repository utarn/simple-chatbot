// src/Infrastructure/Processors/FormT1Processor/FormT1Processor.cs

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using ChatbotApi.Domain.Entities;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;


// ReSharper disable NotResolvedInText

namespace ChatbotApi.Infrastructure.Processors.FormT1Processor;

public class FormT1Processor : ILineMessageProcessor
{
    public string Name => Systems.FormT1;

    private readonly IApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FormT1Processor> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ISystemService _systemService;
    private readonly IOpenAiService _openAiService;
    private readonly string _pdfTemplatePath;

    public FormT1Processor(IApplicationDbContext context, IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment, ILogger<FormT1Processor> logger, ISystemService systemService,
        IOpenAiService openAiService)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _environment = environment;
        _logger = logger;
        _systemService = systemService;
        _openAiService = openAiService;
        _pdfTemplatePath = Path.Combine(environment.ContentRootPath, "FormTemplates", "formT1.pdf");

        if (!File.Exists(_pdfTemplatePath))
        {
            _logger.LogError("PDF template not found at: {TemplatePath}", _pdfTemplatePath);
        }
    }

    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
        string replyToken,
        CancellationToken cancellationToken = default)
    {
        Chatbot? chatbot = await _context.Chatbots
            .FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);

        if (chatbot?.LlmKey == null)
        {
            _logger.LogError("Chatbot {ChatbotId} does not have an AI Summarize token", chatbotId);
            return new LineReplyStatus { Status = 404 };
        }

        var textPrompt = FormT1Prompt.GetTextPrompt(message);

        var openAiRequest = new OpenAiRequest
        {
            Model = chatbot.ModelName ?? "openai/gpt-4.1", // fallback if ModelName is null
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage
                {
                    Role = "user",
                    Content = textPrompt
                }
            }
        };

        OpenAIResponse? response;
        try
        {
            response = await _openAiService.GetOpenAiResponseAsync(openAiRequest, chatbot.LlmKey, cancellationToken, chatbot.ModelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI service call failed");
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages =
                    [
                        new LineTextMessage { Text = "ไม่สามารถติดต่อกับ AI Backend ได้ กรุณาลองใหม่อีกครั้ง" }
                    ]
                }
            };
        }

        var content = response?.Choices?.FirstOrDefault()?.Message?.Content;
        _logger.LogInformation("OpenAI Response Content: {Response}", content);

        var formT1Response = string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize<FormT1Response>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        FormT1Data? extractedData = formT1Response?.Result;

        if ((!extractedData?.NeedToCreate ?? false) && !message.Contains("สร้างใบลา"))
        {
            return new LineReplyStatus() { Status = 404 };
        }

        if (string.IsNullOrEmpty(extractedData?.FullName) ||
            string.IsNullOrEmpty(extractedData.FromDate) ||
            string.IsNullOrEmpty(extractedData.ToDate) ||
            string.IsNullOrEmpty(extractedData.Telephone) ||
            string.IsNullOrEmpty(extractedData.Reason) ||
            string.IsNullOrEmpty(extractedData.Story))
        {
            _logger.LogWarning("Extracted data is incomplete or invalid");
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages =
                    [
                        new LineTextMessage
                        {
                            Text =
                                "กรุณาระบุข้อมูลอย่างน้อยชื่อนามสกุล วันที่เริ่มลา วันที่ลา ประเภทการลา สาเหตุ และเบอร์โทรศัพท์"
                        }
                    ]
                }
            };
        }

        // --- PDF Generation and Handling ---
        byte[] pdfBytes;
        try
        {
            pdfBytes = GeneratePdfForm(_pdfTemplatePath, extractedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF form");
            return new LineReplyStatus { Status = 500 };
        }

        var fileName = Guid.NewGuid() + ".pdf";
        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", fileName);
        await File.WriteAllBytesAsync(uploadPath, pdfBytes, cancellationToken);
        string publicPdfUrl = $"{_systemService.FullHostName}/uploads/{fileName}";

        var replyText = new StringBuilder();
        replyText.AppendLine("ข้อมูลใบลา:");
        replyText.AppendLine($"เรื่อง: {extractedData.Story}");
        replyText.AppendLine($"ชื่อ-นามสกุล: {extractedData.FullName}");
        replyText.AppendLine($"ตำแหน่ง: {extractedData.Position}");
        replyText.AppendLine($"สังกัด: {extractedData.Faculty}");
        replyText.AppendLine($"เหตุผล: {extractedData.Reason}");
        replyText.AppendLine($"ตั้งแต่วันที่: {extractedData.FromDate}");
        replyText.AppendLine($"ถึงวันที่: {extractedData.ToDate}");
        replyText.AppendLine($"เบอร์โทรศัพท์: {extractedData.Telephone}");
        replyText.AppendLine($"ระบุวันที่เขียนใบลา: " + (extractedData.WriteCurrentDate ? "ใช่" : "ไม่ใช่"));
        replyText.AppendLine($"ดาวน์โหลด PDF: {publicPdfUrl}");

        return new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage()
            {
                ReplyToken = replyToken,
                Messages =
                [
                    new LineTextMessage() { Text = replyText.ToString() }
                ]
            }
        };
    }

    public Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken,
        string accessToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus { Status = 404 });
    }


    // Helper method to get content from LINE API
    private async Task<ContentResult?> GetContentAsync(Event evt, string accessToken)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5); // Adjust timeout as needed for potentially large files

        // For media messages, use the message ID endpoint
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        HttpResponseMessage response =
            await client.GetAsync($"https://api-data.line.me/v2/bot/message/{evt.Message.Id}/content");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get content from LINE API. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        byte[] contentBytes = await response.Content.ReadAsByteArrayAsync();
        string? contentType = response.Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrEmpty(evt.Message.FileName))
        {
            contentType = MimeTypes.GetMimeType(evt.Message.FileName);
        }

        if (contentBytes.Length == 0 || string.IsNullOrEmpty(contentType))
        {
            _logger.LogWarning("Received empty content or unknown content type from LINE API");
            return null;
        }

        return new ContentResult { Content = contentBytes, ContentType = contentType };
    }

    private byte[] GeneratePdfForm(string templatePath, FormT1Data data)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"PDF template not found at {templatePath}");
        }

        var fontPath = Path.Combine(_environment.WebRootPath, "assets", "fonts", "THSarabunNew.ttf");

        using var reader = new PdfReader(templatePath);
        using var memoryStream = new MemoryStream();
        using var writer = new PdfWriter(memoryStream);
        using var pdfDoc = new PdfDocument(reader, writer);

        PdfFont thaiFont = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
        float fontSize = 16;
        int targetPageNumber = 1;
        float writeAt_X = 408;
        float writeAt_Y = 712;
        float story_X = 75;
        float story_Y = 668;
        float fullName_X = 153;
        float fullName_Y = 602;
        float fullName2_Y = 344;

        string fullNameTextForSignature = data.FullName;
        float fullNameSignatureWidth = thaiFont.GetWidth(fullNameTextForSignature, fontSize);
        float centeredFullNameSignatureX = 396f - (fullNameSignatureWidth / 2f);

        float position_X = 380;
        float position_Y = 602;
        float faculty_X = 90;
        float faculty_Y = 581;
        float reason_X = 235;
        float reason_Y = 538;
        float fromDate_X = 95;
        float fromDate_Y = 495;
        float toDate_X = 300;
        float toDate_Y = 495;
        float noOfDays_X = 500;
        float noOfDays_Y = 495;
        float telephone_X = 65;
        float telephone_Y = 430;
        float day_X = 405;
        float day_Y = 689;
        float month_X = 447;
        float month_Y = 689;
        float year_X = 536;
        float year_Y = 689;
        float to_X = 75;
        float to_Y = 635;

        float sickLeave_X = 120;
        float sickLeave_Y = 557;

        float personalLeave_X = 120;
        float personalLeave_Y = 536;

        float maternityLeave_X = 120;
        float maternityLeave_Y = 514;

        PdfPage page = pdfDoc.GetPage(targetPageNumber);
        PdfCanvas canvas = new PdfCanvas(page);

        DateTime fromDate;
        DateTime toDate;
        try
        {
            fromDate = DateTime.Parse(data.FromDate, CultureInfo.InvariantCulture);
            toDate = DateTime.Parse(data.ToDate, CultureInfo.InvariantCulture);
        }
        catch (FormatException ex)
        {
            throw new FormatException("Invalid date format in FromDate or ToDate. Expected a parseable date string.",
                ex);
        }

        int noOfDays = 0;
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                noOfDays++;
            }
        }


        var currentDate = DateTime.Now;

        if (data.Story == "ขอลาป่วย" || data.Story == "ลาป่วย")
        {
            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(sickLeave_X, sickLeave_Y)
                .ShowText("X")
                .EndText();
        }
        else if (data.Story == "ขอลากิจ" || data.Story == "ลากิจ")
        {
            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(personalLeave_X, personalLeave_Y)
                .ShowText("X")
                .EndText();
        }
        else if (data.Story == "ขอลากิจส่วนตัว" || data.Story == "ลากิจส่วนตัว")
        {
            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(personalLeave_X, personalLeave_Y)
                .ShowText("X")
                .EndText();
        }
        else if (data.Story == "ขอลาคลอดบุตร" || data.Story == "ลาคลอดบุตร")
        {
            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(maternityLeave_X, maternityLeave_Y)
                .ShowText("X")
                .EndText();
        }


        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(writeAt_X, writeAt_Y)
            .ShowText(data.WriteAt)
            .EndText();

        if (data.WriteCurrentDate)
        {
            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(day_X, day_Y)
                .ShowText(currentDate.Day.ToString(CultureInfo.InvariantCulture))
                .EndText();

            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(month_X, month_Y)
                .ShowText(currentDate.ToString("MMMM", CultureInfo.GetCultureInfo("th-TH")))
                .EndText();

            canvas.BeginText()
                .SetFontAndSize(thaiFont, fontSize)
                .SetTextMatrix(year_X, year_Y)
                .ShowText(currentDate.ToString("yyyy", CultureInfo.GetCultureInfo("th-TH")))
                .EndText();
        }

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(to_X, to_Y)
            .ShowText("คณบดีคณะวิทยาศาสตร์และเทคโนโลยี")
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(story_X, story_Y)
            .ShowText(data.Story)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(fullName_X, fullName_Y)
            .ShowText(data.FullName)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(centeredFullNameSignatureX, fullName2_Y)
            .ShowText(data.FullName)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(position_X, position_Y)
            .ShowText(data.Position)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(faculty_X, faculty_Y)
            .ShowText(data.Faculty)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(reason_X, reason_Y)
            .ShowText(data.Reason)
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(fromDate_X, fromDate_Y)
            .ShowText(fromDate.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("th-TH")))
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(toDate_X, toDate_Y)
            .ShowText(toDate.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("th-TH")))
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(noOfDays_X, noOfDays_Y)
            .ShowText(noOfDays.ToString(CultureInfo
                .InvariantCulture))
            .EndText();

        canvas.BeginText()
            .SetFontAndSize(thaiFont, fontSize)
            .SetTextMatrix(telephone_X, telephone_Y)
            .ShowText("เบอร์โทรศัพท์: " + data.Telephone)
            .EndText();

        pdfDoc.Close();
        return memoryStream.ToArray();
    }
}

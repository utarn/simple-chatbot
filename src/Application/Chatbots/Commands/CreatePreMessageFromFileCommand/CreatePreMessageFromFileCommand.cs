using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Channels;
using System.Web;
using Microsoft.AspNetCore.Http;
using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.CreatePreMessageFromFileCommand;

public class CreatePreMessageFromFileCommand : IRequest<bool>
{
    public int Id { get; set; }
    [Localize(Value = "ชื่อไฟล์")] public List<IFormFile>? Files { get; set; }
    [Localize(Value = "URL เว็บไซต์")] public string? Urls { get; set; }
    [Localize(Value = "ขนาดของข้อมูลที่แบ่ง (สูงสุด 8192)")]
    public int ChunkSize { get; set; }
    [Localize(Value = "ขนาดของข้อมูลที่กำหนดให้ซ้ำกัน")]
    public int OverlappingSize { get; set; }
    [Localize(Value = "ใช้ Content As-is Generation (CAG)")]
    public bool UseCag { get; set; } = false;
}

public class CreatePreMessageFromFileCommandHandler : IRequestHandler<CreatePreMessageFromFileCommand, bool>
{
    private readonly ChannelWriter<ProcessFileBackgroundTask> _channelWriter;
    private readonly IHttpClientFactory _httpClientFactory;

    public CreatePreMessageFromFileCommandHandler(ChannelWriter<ProcessFileBackgroundTask> channelWriter, IHttpClientFactory httpClientFactory)
    {
        _channelWriter = channelWriter;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> Handle(CreatePreMessageFromFileCommand request, CancellationToken cancellationToken)
    {
        if (request.Files != null && request.Files.Any())
        {
            foreach (var file in request.Files)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                var fileBytes = memoryStream.ToArray();

                var backgroundTask = new ProcessFileBackgroundTask
                {
                    ChatBotId = request.Id,
                    FileContent = fileBytes,
                    FileName = file.FileName,
                    ChunkSize = request.ChunkSize,
                    OverlappingSize = request.OverlappingSize,
                    UseCag = request.UseCag
                };

                await _channelWriter.WriteAsync(backgroundTask, cancellationToken);
            }
        }

        if (!string.IsNullOrEmpty(request.Urls))
        {
            var urls = request.Urls.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var url in urls)
            {
                var trimmedUrl = url.Trim();
                var httpClient = _httpClientFactory.CreateClient();
                // Add browser-like headers to the HttpClient
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
                httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
                httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                try
                {
                    // Fetch the headers to determine the content type
                    var response = await httpClient.GetAsync(trimmedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        // Log or handle invalid URLs
                        continue;
                    }

                    var contentType = response.Content.Headers.ContentType?.MediaType;
                    // Parse the URL
                    Uri uri = new Uri(url);

                    if (contentType == "text/html")
                    {
                        string decodedUri = HttpUtility.UrlDecode(url);
                        var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        // HTML content: pass the URL directly
                        var backgroundTask = new ProcessFileBackgroundTask
                        {
                            ChatBotId = request.Id,
                            Url = decodedUri,
                            ChunkSize = request.ChunkSize,
                            OverlappingSize = request.OverlappingSize,
                            UseCag = false,
                            FileContent = Encoding.UTF8.GetBytes(htmlContent),
                            FileMimeType = MediaTypeNames.Text.Html,
                            FileName = $"{Guid.NewGuid()}.html",
                        };

                        await _channelWriter.WriteAsync(backgroundTask, cancellationToken);
                    }
                    else if (contentType == "application/pdf" || contentType?.StartsWith("image/") == true)
                    {
                        string fileName = Path.GetFileName(uri.LocalPath);
                        string decodedFileName = HttpUtility.UrlDecode(fileName);
                        // PDF or image file: download the file content
                        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        var backgroundTask = new ProcessFileBackgroundTask
                        {
                            ChatBotId = request.Id,
                            FileContent = fileBytes,
                            FileName = decodedFileName,
                            FileMimeType = contentType,
                            ChunkSize = request.ChunkSize,
                            OverlappingSize = request.OverlappingSize,
                            UseCag = true, // Use CAG for PDF and image files,
                            Url = trimmedUrl
                        };

                        await _channelWriter.WriteAsync(backgroundTask, cancellationToken);
                    }
                    else
                    {
                        // Unsupported content type
                        // Log or handle unsupported content types
                    }
                }
                catch (Exception)
                {
                    // Log or handle exceptions
                }
            }
        }

        return true;
    }
}

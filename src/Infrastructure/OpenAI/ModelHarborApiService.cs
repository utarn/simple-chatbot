using System.Text;
using System.Text.Json;
using ChatbotApi.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using OpenAiService.Splitters;
using Pgvector;

namespace ChatbotApi.Infrastructure.OpenAI;

public class ModelHarborApiService : IOpenAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModelHarborApiService> _logger;
    private readonly string _baseUrl;
    private readonly string _defaultModel;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ModelHarborApiService(IHttpClientFactory httpClientFactory, ILogger<ModelHarborApiService> logger, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _baseUrl = "https://api.modelharbor.com/v1/";
        _defaultModel = configuration["OpenAI:DefaultModel"] ?? "openai/gpt-4.1";
    }

    public async Task<OpenAIResponse?> GetOpenAiResponseAsync(OpenAiRequest request, string apiKey, CancellationToken cancellationToken = default, string? model = null)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.BaseAddress = new Uri(_baseUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Set the model if not already set in the request
        if (string.IsNullOrEmpty(request.Model))
        {
            request.Model = model ?? _defaultModel;
        }

        var jsonRequest = JsonSerializer.Serialize(request, Options);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to call ModelHarbor API: {ResponseText} {Reason}", response.StatusCode, reason);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Invalid ModelHarbor API key. Please check your API key configuration.");
            }

            throw new Exception($"Failed to call ModelHarbor API: {response.StatusCode} - {reason}");
        }
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
    }

    public async Task<Vector?> CallEmbeddingsAsync(string text, string apiKey, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_baseUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        var jsonRequest = JsonSerializer.Serialize(new { model = "baai/bge-m3", input = text, encoding_format = "float" }, Options);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("embeddings", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to call ModelHarbor embeddings API: {ResponseText} {Reason}", response.StatusCode, errorText);
            throw new Exception("Failed to call ModelHarbor embeddings API: " + errorText);
        }
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddingsResponse = JsonSerializer.Deserialize<EmbeddingsResponse>(responseText, Options);
        return new Vector(embeddingsResponse?.Data[0].Embedding);
    }

    public async Task<List<string>> GetTextChunksFromFileAsync(string base64, string mimeType, string apiKey, string modelName, string? prompt = null, int? chunkSize = null, int? chunkOverlap = null, CancellationToken cancellationToken = default)
    {
        // Defaults
        int actualChunkSize = chunkSize ?? 4000;
        int actualChunkOverlap = chunkOverlap ?? 200;
        prompt ??= "Extract text from this file and return it in a format suitable for LLM processing.";

        // Decode the file
        byte[] fileBytes = Convert.FromBase64String(base64);
        string concatenatedText = "";

        if (mimeType == "application/pdf" && !modelName.Contains("gemini"))
        {
            // Convert each PDF page to an image and send all images in a single request with one prompt
            using var images = new ImageMagick.MagickImageCollection();
            images.Read(fileBytes, new ImageMagick.MagickReadSettings
            {
                Density = new ImageMagick.Density(300, 300), // High DPI for better OCR
                Format = ImageMagick.MagickFormat.Pdf
            });

            // Prepare the prompt message
            var messages = new List<object>
            {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                }
            };

            // Add each image as a separate image_url in the same message array
            foreach (var image in images)
            {
                using var ms = new MemoryStream();
                image.Format = ImageMagick.MagickFormat.Png;
                image.Write(ms);
                var pageBytes = ms.ToArray();
                string pageBase64 = Convert.ToBase64String(pageBytes);
                string dataUrl = $"data:image/png;base64,{pageBase64}";

                messages.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = dataUrl } }
                    }
                });
            }

            var requestBody = new
            {
                model = modelName,
                messages = messages.ToArray(),
                max_tokens = 65536
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.BaseAddress = new Uri(_baseUrl);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var jsonRequest = JsonSerializer.Serialize(requestBody, Options);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to call ModelHarbor chat completion API: {StatusCode} {Error}", response.StatusCode, errorText);
                throw new Exception($"Failed to call ModelHarbor chat completion API: {errorText}");
            }

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
            concatenatedText = result?.Choices?[0]?.Message?.Content ?? "";
            concatenatedText = concatenatedText.Replace("```text", "").Replace("```", "").Trim();
        }
        else
        {
            // For other files, send the file as a data URL to ModelHarbor
            var dataUrl = $"data:{mimeType};base64,{base64}";
            var requestBody = new
            {
                model = modelName,
                messages = new object[]
                {
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt }
                        }
                    },
                    new {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "image_url", image_url = new { url = dataUrl } }
                        }
                    }
                },
                max_tokens = 65536
            };

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.BaseAddress = new Uri(_baseUrl);
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var jsonRequest = JsonSerializer.Serialize(requestBody, Options);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to call ModelHarbor chat completion API: {StatusCode} {Error}", response.StatusCode, errorText);
                throw new Exception($"Failed to call ModelHarbor chat completion API: {errorText}");
            }

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
            concatenatedText = result?.Choices?[0]?.Message?.Content ?? "";
            concatenatedText = concatenatedText.Replace("```text", "").Replace("```", "").Trim();
        }

        // Split the resulting text into chunks
        var splitter = new RecursiveCharacterTextSplitter(
            chunkSize: actualChunkSize,
            chunkOverlap: actualChunkOverlap
        );
        var chunks = splitter.SplitText(concatenatedText);
        return chunks;
    }

    public List<string> SplitTextAsync(string text, string apiKey, int? chunkSize = null, int? chunkOverlap = null, CancellationToken cancellationToken = default)
    {
        int actualChunkSize = chunkSize ?? 4000;
        int actualChunkOverlap = chunkOverlap ?? 200;

        var splitter = new RecursiveCharacterTextSplitter(
            chunkSize: actualChunkSize,
            chunkOverlap: actualChunkOverlap
        );
        var chunks = splitter.SplitText(text);
        return chunks;
    }

    public async Task<string> CallSummaryAsync(string base64, string mimeType, string apiKey, string modelName, CancellationToken cancellationToken = default)
    {
        // Default prompt
        var prompt = "Summarize the content of this file.";

        // Prepare base64 data URL for OpenAI vision models
        var dataUrl = $"data:{mimeType};base64,{base64}";

        // Build OpenAI chat completions request
        var requestBody = new
        {
            model = modelName,
            messages = new object[]
            {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                },
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = dataUrl } }
                    }
                }
            },
            max_tokens = 1024
        };

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.BaseAddress = new Uri(_baseUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var jsonRequest = JsonSerializer.Serialize(requestBody, Options);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to call OpenAI API: {StatusCode} {Error}", response.StatusCode, errorText);
            throw new Exception($"Failed to call OpenAI API: {errorText}");
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    public async Task<string> GetHtmlContentAsync(string url, string apiKey, string modelName, CancellationToken cancellationToken = default)
    {
        // 1. Fetch HTML content from the URL with custom headers
        var httpClient = _httpClientFactory.CreateClient();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        requestMessage.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
        requestMessage.Headers.Add("Accept-Language", "en-US,en;q=0.9");
        requestMessage.Headers.Add("Referer", "https://www.google.com/");
        requestMessage.Headers.Add("Connection", "keep-alive");
        requestMessage.Headers.Add("Upgrade-Insecure-Requests", "1");

        var htmlResponse = await httpClient.SendAsync(requestMessage, cancellationToken);
        if (!htmlResponse.IsSuccessStatusCode)
        {
            var errorText = await htmlResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch HTML content: {StatusCode} {Error}", htmlResponse.StatusCode, errorText);
            throw new Exception("Failed to fetch HTML content from the URL: " + errorText);
        }
        var html = await htmlResponse.Content.ReadAsStringAsync(cancellationToken);

        // 2. Cleanse HTML content (remove script/style/comments, preserve tables)
        static string CleanseHtml(string input)
        {
            // Remove script and style tags and their content
            var noScripts = System.Text.RegularExpressions.Regex.Replace(input, "<script[\\s\\S]*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var noStyles = System.Text.RegularExpressions.Regex.Replace(noScripts, "<style[\\s\\S]*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            // Remove HTML comments
            var noComments = System.Text.RegularExpressions.Regex.Replace(noStyles, "<!--.*?-->", "", System.Text.RegularExpressions.RegexOptions.Singleline);
            // Optionally, collapse whitespace
            var cleansed = System.Text.RegularExpressions.Regex.Replace(noComments, @"[ \t\r\f\v]+", " ");
            return cleansed;
        }
        var cleansedHtml = CleanseHtml(html);

        // 3. Build the prompt as in the Python backend
        var prompt =
            "Act as you are an expert to convert HTML content to LLM readable content to feed it as the knowledge to LLM model.\n" +
            "1) Parse HTML content.\n" +
            "2) Do not translate the content, just leave it as is.\n" +
            "3) Return as markdown format by which 3.1) do not store link and image information 3.2) preserve HTML table as it is\n" +
            $"#### HTML CONTENT ####\n{cleansedHtml}\n####";

        // 4. Call ModelHarbor chat completions API with the prompt
        var requestBody = new
        {
            model = modelName,
            messages = new object[]
            {
                new {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt }
                    }
                }
            },
            max_tokens = 2048
        };

        var chatClient = _httpClientFactory.CreateClient();
        chatClient.Timeout = TimeSpan.FromMinutes(5);
        chatClient.BaseAddress = new Uri(_baseUrl);
        chatClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var jsonRequest = JsonSerializer.Serialize(requestBody, Options);
        var chatContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var chatResponse = await chatClient.PostAsync("chat/completions", chatContent, cancellationToken);

        if (!chatResponse.IsSuccessStatusCode)
        {
            var errorText = await chatResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to call ModelHarbor chat completion API: {StatusCode} {Error}", chatResponse.StatusCode, errorText);
            throw new Exception($"Failed to call ModelHarbor chat completion API: {errorText}");
        }

        var responseText = await chatResponse.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
        return result?.Choices?[0]?.Message?.Content ?? string.Empty;
    }

    public async Task<List<(string Text, string Value)>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(_baseUrl);

        try
        {
            var response = await httpClient.GetAsync("/v1/model/info", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<ModelInfoResponseDto>(content, Options);
            var modelList = new List<(string Text, string Value)>();
            var addedDisplayTexts = new HashSet<string>();

            // Add Gemini models first (as per original implementation)
            var geminiFlash = FormatGeminiModel("gemini/gemini-2.0-flash", 0.000000200, 0.000000600);
            var geminiFlashDisplayText = geminiFlash.Text;
            modelList.Add(geminiFlash);
            addedDisplayTexts.Add(geminiFlashDisplayText);

            var geminiPro = FormatGeminiModel("gemini/gemini-2.5-pro", 0.00000150, 0.00001250);
            var geminiProDisplayText = geminiPro.Text;
            modelList.Add(geminiPro);
            addedDisplayTexts.Add(geminiProDisplayText);

            // Process models from API response
            if (result?.Data != null)
            {
                foreach (var model in result.Data)
                {
                    if (model.ModelName.Contains("baai"))
                    {
                        continue;
                    }
                    var formattedModel = FormatModelDisplay(model);
                    if (formattedModel.HasValue)
                    {
                        var displayText = formattedModel.Value.Text;
                        var originalDisplayText = displayText;
                        var counter = 1;
                        
                        // Check for duplicates and make display text distinctive
                        while (addedDisplayTexts.Contains(displayText))
                        {
                            // Try to use LiteLlmParams model name if available to make it distinctive
                            if (!string.IsNullOrEmpty(model.LiteLlmParams?.Model) && model.LiteLlmParams.Model != model.ModelName)
                            {
                                // Extract cost information from original display text
                                var costInfoStart = originalDisplayText.IndexOf(" ($");
                                if (costInfoStart > 0)
                                {
                                    var modelNamePart = originalDisplayText.Substring(0, costInfoStart);
                                    var costInfoPart = originalDisplayText.Substring(costInfoStart);
                                    displayText = $"{modelNamePart} ({model.LiteLlmParams.Model}){costInfoPart}";
                                }
                                else
                                {
                                    displayText = $"{originalDisplayText} ({model.LiteLlmParams.Model})";
                                }
                            }
                            else
                            {
                                // Fallback to adding a counter
                                var costInfoStart = originalDisplayText.IndexOf(" ($");
                                if (costInfoStart > 0)
                                {
                                    var modelNamePart = originalDisplayText.Substring(0, costInfoStart);
                                    var costInfoPart = originalDisplayText.Substring(costInfoStart);
                                    displayText = $"{modelNamePart} ({counter}){costInfoPart}";
                                }
                                else
                                {
                                    displayText = $"{originalDisplayText} ({counter})";
                                }
                                counter++;
                            }
                            
                            // If even the modified display text exists, continue the loop
                            if (!addedDisplayTexts.Contains(displayText))
                            {
                                break;
                            }
                        }
                        
                        // If we had to modify the display text, update the formatted model
                        if (displayText != originalDisplayText)
                        {
                            formattedModel = (displayText, formattedModel.Value.Value);
                        }
                        
                        modelList.Add(formattedModel.Value);
                        addedDisplayTexts.Add(displayText);
                    }
                }
            }

            return modelList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from ModelHarbor API");
            throw new ApplicationException("Failed to retrieve models from ModelHarbor API.", ex);
        }
    }

    private static (string Text, string Value) FormatGeminiModel(string modelName, double inputCostPerToken, double outputCostPerToken)
    {
        var inputCostPerMillion = inputCostPerToken * 1_000_000;
        var outputCostPerMillion = outputCostPerToken * 1_000_000;

        var displayText = $"{modelName} (${inputCostPerMillion:F2} / ${outputCostPerMillion:F2})";
        return (displayText, modelName);
    }

    private static (string Text, string Value)? FormatModelDisplay(ModelInfoDto model)
    {
        if (string.IsNullOrEmpty(model.ModelName) || model.ModelInfo == null)
        {
            return null;
        }

        // Get input and output costs per token
        var inputCostPerToken = model.ModelInfo.InputCostPerToken ?? 0;
        var outputCostPerToken = model.ModelInfo.OutputCostPerToken ?? 0;

        // Convert to cost per million tokens
        var inputCostPerMillion = inputCostPerToken * 1_000_000;
        var outputCostPerMillion = outputCostPerToken * 1_000_000;

        // Format: {ModelName} ({input price per million token with dollar sign} / {output price per million token with dollar sign})
        var displayText = $"{model.ModelName} (${inputCostPerMillion:F2} / ${outputCostPerMillion:F2})";

        return (displayText, model.ModelName);
    }

    public async Task<OpenAIResponse?> GetOpenAiResponseAsync(object request, string apiKey, CancellationToken cancellationToken = default, string? model = null)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);
        httpClient.BaseAddress = new Uri(_baseUrl);
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Serialize the request object to JSON
        var jsonRequest = JsonSerializer.Serialize(request, Options);
        
        // If a model parameter is provided, we need to ensure it's included in the request
        if (!string.IsNullOrEmpty(model))
        {
            // Parse the JSON and add/modify the model property
            using var jsonDoc = JsonDocument.Parse(jsonRequest);
            var root = jsonDoc.RootElement;
            
            // Create a new JSON object with the model property
            var modifiedRequest = new Dictionary<string, object>();
            
            // Copy all existing properties
            foreach (var property in root.EnumerateObject())
            {
                modifiedRequest[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
            }
            
            // Set or override the model property
            modifiedRequest["model"] = model;
            
            jsonRequest = JsonSerializer.Serialize(modifiedRequest, Options);
        }

        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("chat/completions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to call ModelHarbor API: {ResponseText} {Reason}", response.StatusCode, reason);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Invalid ModelHarbor API key. Please check your API key configuration.");
            }

            throw new Exception($"Failed to call ModelHarbor API: {response.StatusCode} - {reason}");
        }
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OpenAIResponse>(responseText, Options);
    }
}

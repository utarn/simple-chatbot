using System.Text.Json;
using ChatbotApi.Application.Common.Exceptions;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;
using ChatbotApi.Application.Webhook.Commands.LineWebhookCommand;
using ChatbotApi.Application.Webhook.Commands.OpenAIWebhookCommand;
using ChatbotApi.Application.Webhook.Queries.GetFacebookSubscribeQuery;
using ChatbotApi.Domain.Entities;
using Microsoft.Extensions.Primitives;

namespace ChatbotApi.Web.Endpoints;

public class WebhookEndpoint : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup("/webhook")
            .MapGet(FacebookGet, "/facebook/{chatbotId:int}")
            .MapPost(FacebookPost, "/facebook/{chatbotId:int}")
            .MapPost(Line, "/line/{chatbotId:int}")
            .MapPost(OpenAI, "/openai/{chatbotId:int}/chat/completions")
            ;
    }

    private async Task<IResult> OpenAI(ISender sender, int chatbotId, HttpContext httpContext, IApplicationDbContext context)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        await context.IncomingRequests.AddAsync(new IncomingRequest()
        {
            Raw = requestBody
        });
        await context.SaveChangesAsync(CancellationToken.None);

        // Deserialize the request body into LineWebhookCommand object
        var command = JsonSerializer.Deserialize<OpenAIWebhookCommand>(requestBody);

        if (command == null)
        {
            return Results.BadRequest("Not openai webhook");
        }

        command.ChatbotId = chatbotId;

        if (httpContext.Request.Headers["Authorization"] == StringValues.Empty)
        {
            return Results.Unauthorized();
        }

        var authorization = httpContext.Request.Headers["Authorization"];
        var bearerToken = authorization.ToString().Replace("Bearer ", "");
        command.ApiKey = bearerToken;
        try
        {
            var result = await sender.Send(command);
            if (result.Choices != null && result.Choices.Count > 0)
            {
                return Results.Ok(result);
            }

        }
        catch (ChatCompletionException e)
        {
            return Results.BadRequest(e.Message);
        }

        return Results.BadRequest();
    }
    private async Task<IResult> FacebookGet(ISender sender, HttpContext context, int chatbotId)
    {
        var mode = context.Request.Query["hub.mode"];
        var token = context.Request.Query["hub.verify_token"];
        var challenge = context.Request.Query["hub.challenge"];

        var result = await sender.Send(new GetFacebookSubscribeQuery()
        {
            Mode = mode.ToString(),
            VerifyToken = token.ToString(),
            Challenge = challenge.ToString(),
            ChatbotId = chatbotId
        });

        return result;
    }

    private async Task<IResult> FacebookPost(ISender sender, int chatbotId, HttpContext httpContext, IApplicationDbContext context)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        await context.IncomingRequests.AddAsync(new IncomingRequest()
        {
            Raw = requestBody
        });
        await context.SaveChangesAsync(CancellationToken.None);

        var command = JsonSerializer.Deserialize<FacebookWebhookCommand>(requestBody);

        if (command == null)
        {
            return Results.BadRequest("Invalid Facebook webhook payload.");
        }

        command.ChatbotId = chatbotId;
        var result = await sender.Send(command);
        if (result)
        {
            return Results.Ok();
        }

        return Results.BadRequest();
    }


    private async Task<IResult> Line(ISender sender, int chatBotId, HttpContext httpContext, IApplicationDbContext context)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        await context.IncomingRequests.AddAsync(new IncomingRequest()
        {
            Raw = requestBody
        });
        await context.SaveChangesAsync(CancellationToken.None);

        // Deserialize the request body into LineWebhookCommand object
        var command = JsonSerializer.Deserialize<LineWebhookCommand>(requestBody);

        if (command == null || command.Destination == null)
        {
            return Results.BadRequest("Not line webhook");
        }

        command.ChatbotId = chatBotId;
        await sender.Send(command);
        return Results.Ok();
    }
}

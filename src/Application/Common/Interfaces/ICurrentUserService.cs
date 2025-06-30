using System.Net;
using Microsoft.AspNetCore.Http;

namespace ChatbotApi.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string Role { get; }
    string Email { get; }
    IPAddress? IPAddress { get; }
}

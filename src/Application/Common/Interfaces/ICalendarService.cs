using System.Threading.Tasks;
using ChatbotApi.Domain.Models;

namespace ChatbotApi.Application.Common.Interfaces
{
    public interface ICalendarService
    {
        /// <summary>
        /// Gets the OAuth authorization URL for Calendar access.
        /// </summary>
        /// <param name="userId">User identifier for storing tokens</param>
        /// <returns>Authorization URL to redirect user to</returns>
        string GetAuthorizationUrl(string userId);

        /// <summary>
        /// Handles the OAuth callback and exchanges authorization code for tokens.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="authorizationCode">Authorization code from OAuth callback</param>
        /// <returns>True if authorization was successful</returns>
        Task<bool> HandleAuthorizationCallbackAsync(string userId, string authorizationCode);

        /// <summary>
        /// Creates a calendar event using Google Calendar API on behalf of the authenticated user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="summary">Event summary/title</param>
        /// <param name="description">Event description</param>
        /// <param name="startDateTime">Event start date and time</param>
        /// <param name="endDateTime">Event end date and time</param>
        /// <param name="isAllDay">Whether the event is all day</param>
        /// <returns>True if event was created successfully</returns>
        Task<bool> CreateEventAsync(string userId, string summary, string description, DateTime startDateTime, DateTime endDateTime, bool isAllDay = false);

        /// <summary>
        /// Checks if the user has valid Calendar tokens.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if user has valid tokens</returns>
        Task<bool> IsAuthorizedAsync(string userId);

        /// <summary>
        /// Revokes the user's Calendar authorization.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if revocation was successful</returns>
        Task<bool> RevokeAuthorizationAsync(string userId);
    }
}
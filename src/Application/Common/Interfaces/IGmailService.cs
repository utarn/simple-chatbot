using System.Threading.Tasks;
using ChatbotApi.Domain.Models;

namespace ChatbotApi.Application.Common.Interfaces
{
    public interface IGmailService
    {
        /// <summary>
        /// Gets the OAuth authorization URL for Gmail access.
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
        /// Sends an email using Gmail API on behalf of the authenticated user.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML or plain text)</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendEmailAsync(string userId, string to, string subject, string body);

        /// <summary>
        /// Checks if the user has valid Gmail tokens.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if user has valid tokens</returns>
        Task<bool> IsAuthorizedAsync(string userId);

        /// <summary>
        /// Revokes the user's Gmail authorization.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if revocation was successful</returns>
        Task<bool> RevokeAuthorizationAsync(string userId);

        /// <summary>
        /// Gets the latest emails from the last 4 hours, tracking the latest email ID to avoid duplicates.
        /// On first call, fetches all emails from last 4 hours. On subsequent calls, only returns new emails.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>List of new email information</returns>
        Task<List<GmailEmailInfo>> GetLatestEmailsAsync(string userId);
    }
}
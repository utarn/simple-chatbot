using System.Threading.Tasks;

namespace ChatbotApi.Application.Common.Interfaces
{
    public interface IGoogleDriveService
    {
        /// <summary>
        /// Gets the OAuth authorization URL for Google Drive access.
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
        /// Checks if the user has valid Google Drive tokens.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if user has valid tokens</returns>
        Task<bool> IsAuthorizedAsync(string userId);

        /// <summary>
        /// Revokes the user's Google Drive authorization.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if revocation was successful</returns>
        Task<bool> RevokeAuthorizationAsync(string userId);

        /// <summary>
        /// Lists files in the root directory of the user's Google Drive.
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>List of files in the root directory</returns>
        Task<IList<Google.Apis.Drive.v3.Data.File>> ListRootFilesAsync(string userId);
    }
}
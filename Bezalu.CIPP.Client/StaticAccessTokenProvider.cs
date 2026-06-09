using Microsoft.Kiota.Abstractions.Authentication;

namespace Bezalu.CIPP.Client
{
    /// <summary>
    /// Provides a static access token for Bearer authentication.
    /// </summary>
    /// <remarks>
    /// Useful when a caller already has a bearer token in hand and does not need the
    /// token-refresh machinery of a full <see cref="IAuthenticationProvider"/> implementation.
    /// </remarks>
    public sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
    {
        /// <inheritdoc/>
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();

        /// <inheritdoc/>
        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(accessToken);
    }
}

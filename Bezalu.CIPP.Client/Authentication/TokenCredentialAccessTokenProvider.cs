using Azure.Core;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Bezalu.CIPP.Client.Authentication
{
    /// <summary>
    /// Bridges an <see cref="Azure.Core.TokenCredential"/> into the Kiota authentication pipeline.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="TokenCredential"/> handles acquisition, caching, and refresh for
    /// every Microsoft Entra flow (client secret, managed identity, workload identity, developer
    /// credentials, and more), so no token lifetime management is required here.
    /// </remarks>
    public sealed class TokenCredentialAccessTokenProvider : IAccessTokenProvider
    {
        private readonly TokenCredential _credential;
        private readonly string[] _scopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCredentialAccessTokenProvider"/> class.
        /// </summary>
        /// <param name="credential">The credential used to acquire access tokens for the CIPP API.</param>
        /// <param name="scopes">The scopes to request (for example <c>api://&lt;app-id&gt;/.default</c>).</param>
        /// <param name="allowedHosts">
        /// Optional set of hosts the token may be sent to. When omitted, all hosts are allowed; supply
        /// the CIPP host to prevent the bearer token from leaking to unexpected destinations.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="credential"/> or <paramref name="scopes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="scopes"/> is empty.</exception>
        public TokenCredentialAccessTokenProvider(TokenCredential credential, string[] scopes, IEnumerable<string>? allowedHosts = null)
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(scopes);
            if (scopes.Length == 0)
            {
                throw new ArgumentException("At least one scope must be supplied.", nameof(scopes));
            }

            _credential = credential;
            _scopes = scopes;
            AllowedHostsValidator = allowedHosts is null ? new AllowedHostsValidator() : new AllowedHostsValidator(allowedHosts);
        }

        /// <inheritdoc/>
        public AllowedHostsValidator AllowedHostsValidator { get; }

        /// <inheritdoc/>
        public async Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(uri);
            if (!AllowedHostsValidator.IsUrlHostValid(uri))
            {
                return string.Empty;
            }

            var token = await _credential
                .GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken)
                .ConfigureAwait(false);
            return token.Token;
        }
    }
}

using Azure.Core;
using Bezalu.CIPP.Client.Authentication;
using Bezalu.CIPP.Client.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bezalu.CIPP.Client
{
    public partial class CIPPClient
    {
        /// <summary>
        /// Creates a <see cref="CIPPClient"/> authenticated with a static access token.
        /// </summary>
        /// <remarks>
        /// This is an escape hatch for callers that already hold a bearer token. It performs no token
        /// refresh; prefer the <see cref="TokenCredential"/> overload or
        /// <c>IServiceCollection.AddCippClient(...)</c> for production scenarios.
        /// </remarks>
        /// <param name="baseUrl">The CIPP API base URL (e.g., https://your-cipp-instance.azurewebsites.net).</param>
        /// <param name="accessToken">The OAuth2 Bearer access token for the CIPP API.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>. When supplied, the caller owns the transport pipeline and the built-in resilience/telemetry handlers are not added.</param>
        /// <param name="loggerFactory">Optional logger factory used by the telemetry handler when the default transport is created.</param>
        /// <returns>A ready-to-use <see cref="CIPPClient"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="accessToken"/> is null or whitespace.</exception>
        public static CIPPClient Create(string baseUrl, string accessToken, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

            var authProvider = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken));
            return Create(baseUrl, authProvider, httpClient, loggerFactory);
        }

        /// <summary>
        /// Creates a <see cref="CIPPClient"/> authenticated with an Azure <see cref="TokenCredential"/>.
        /// </summary>
        /// <remarks>
        /// The credential handles acquisition, caching, and refresh for every Microsoft Entra flow
        /// (client secret, managed identity, workload identity, developer credentials, and more).
        /// </remarks>
        /// <param name="baseUrl">The CIPP API base URL (e.g., https://your-cipp-instance.azurewebsites.net).</param>
        /// <param name="credential">The credential used to acquire access tokens for the CIPP API.</param>
        /// <param name="scopes">The scopes to request (for example <c>api://&lt;app-id&gt;/.default</c>).</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>. When supplied, the caller owns the transport pipeline and the built-in resilience/telemetry handlers are not added.</param>
        /// <param name="loggerFactory">Optional logger factory used by the telemetry handler when the default transport is created.</param>
        /// <returns>A ready-to-use <see cref="CIPPClient"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null or whitespace, or when <paramref name="scopes"/> is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="credential"/> or <paramref name="scopes"/> is null.</exception>
        public static CIPPClient Create(string baseUrl, TokenCredential credential, string[] scopes, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(scopes);
            if (scopes.Length == 0)
            {
                throw new ArgumentException("At least one scope must be supplied.", nameof(scopes));
            }

            var tokenProvider = new TokenCredentialAccessTokenProvider(credential, scopes, GetAllowedHosts(baseUrl));
            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
            return Create(baseUrl, authProvider, httpClient, loggerFactory);
        }

        /// <summary>
        /// Creates a <see cref="CIPPClient"/> from any Kiota <see cref="IAuthenticationProvider"/>.
        /// </summary>
        /// <remarks>
        /// This is the shared composition seam used by the other factory overloads. Use it directly for
        /// advanced authentication scenarios (for example, a custom provider or anonymous access). When no
        /// <paramref name="httpClient"/> is supplied, a default transport is built with Kiota's standard
        /// middleware (including <c>Retry-After</c>-aware retries) and the CIPP telemetry/error handler on top.
        /// </remarks>
        /// <param name="baseUrl">The CIPP API base URL (e.g., https://your-cipp-instance.azurewebsites.net).</param>
        /// <param name="authenticationProvider">The authentication provider used to authorize requests.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>. When supplied, the caller owns the transport pipeline and the built-in resilience/telemetry handlers are not added.</param>
        /// <param name="loggerFactory">Optional logger factory used by the telemetry handler when the default transport is created.</param>
        /// <returns>A ready-to-use <see cref="CIPPClient"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, whitespace, or not an absolute HTTP/HTTPS URI.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="authenticationProvider"/> is null.</exception>
        public static CIPPClient Create(string baseUrl, IAuthenticationProvider authenticationProvider, HttpClient? httpClient = null, ILoggerFactory? loggerFactory = null)
        {
            ArgumentNullException.ThrowIfNull(authenticationProvider);
            var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);

            var adapter = new HttpClientRequestAdapter(authenticationProvider, httpClient: httpClient ?? CreateDefaultHttpClient(loggerFactory))
            {
                BaseUrl = normalizedBaseUrl
            };
            return new CIPPClient(adapter);
        }

        /// <summary>
        /// Builds the default transport: Kiota's standard middleware with the CIPP telemetry/error handler outermost.
        /// </summary>
        private static HttpClient CreateDefaultHttpClient(ILoggerFactory? loggerFactory)
        {
            var handlers = KiotaClientFactory.CreateDefaultHandlers();
            // Insert outermost so retries and Retry-After handling complete before failures become exceptions.
            handlers.Insert(0, new CippTelemetryHandler(loggerFactory?.CreateLogger<CippTelemetryHandler>()));
            return KiotaClientFactory.Create(handlers);
        }

        /// <summary>
        /// Validates and normalizes the CIPP base URL: trims surrounding whitespace, requires an absolute
        /// HTTP/HTTPS URI, and removes any trailing slash so Kiota can compose request URIs reliably.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, whitespace, or not an absolute HTTP/HTTPS URI.</exception>
        private static string NormalizeBaseUrl(string baseUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

            var trimmed = baseUrl.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Base URL must be an absolute HTTP or HTTPS URI (e.g., https://your-cipp-instance.azurewebsites.net).",
                    nameof(baseUrl));
            }

            return trimmed.TrimEnd('/');
        }

        /// <summary>
        /// Restricts the bearer token to the CIPP host so it cannot leak to unexpected destinations.
        /// </summary>
        internal static IEnumerable<string>? GetAllowedHosts(string baseUrl)
            => Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var uri) ? [uri.Host] : null;
    }
}

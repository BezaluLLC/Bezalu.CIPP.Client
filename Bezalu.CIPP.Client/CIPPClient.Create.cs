using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bezalu.CIPP.Client
{
    public partial class CIPPClient
    {
        /// <summary>
        /// Creates a <see cref="CIPPClient"/> authenticated with the given access token.
        /// </summary>
        /// <param name="baseUrl">The CIPP API base URL (e.g., https://your-cipp-instance.azurewebsites.net/api).</param>
        /// <param name="accessToken">The OAuth2 Bearer access token for the CIPP API.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/> for custom transport, proxies, or middleware.</param>
        /// <returns>A ready-to-use <see cref="CIPPClient"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> or <paramref name="accessToken"/> is null or whitespace.</exception>
        public static CIPPClient Create(string baseUrl, string accessToken, HttpClient? httpClient = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

            var tokenProvider = new StaticAccessTokenProvider(accessToken);
            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient ?? new HttpClient())
            {
                BaseUrl = baseUrl.TrimEnd('/')
            };
            return new CIPPClient(adapter);
        }
    }
}

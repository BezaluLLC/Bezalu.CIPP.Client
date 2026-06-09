using Azure.Core;
using Bezalu.CIPP.Client.Authentication;
using Bezalu.CIPP.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Bezalu.CIPP.Client
{
    /// <summary>
    /// Dependency-injection registration helpers for <see cref="CIPPClient"/>.
    /// </summary>
    public static class CippClientServiceCollectionExtensions
    {
        /// <summary>The name of the <see cref="HttpClient"/> registered for the CIPP API transport.</summary>
        public const string HttpClientName = "Bezalu.CIPP.Client";

        /// <summary>
        /// Registers <see cref="CIPPClient"/> and its dependencies for resolution from the container.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Wires up validated <see cref="CippClientOptions"/> (enforced at startup via
        /// <see cref="OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations{TOptions}(OptionsBuilder{TOptions})"/>
        /// and <see cref="OptionsBuilder{TOptions}.ValidateOnStart"/>), a named <see cref="HttpClient"/>
        /// fronted by an <see cref="IHttpClientFactory"/> with the CIPP telemetry/error handler and a
        /// standard resilience pipeline, and Azure credential-based authentication.
        /// </para>
        /// <para>
        /// Supply the credential through <see cref="CippClientOptions.Credential"/> or register an
        /// <see cref="TokenCredential"/> in the container (for example via
        /// <c>services.AddSingleton&lt;TokenCredential&gt;(new DefaultAzureCredential())</c>); the options
        /// value takes precedence when both are present.
        /// </para>
        /// </remarks>
        /// <param name="services">The service collection to add the client to.</param>
        /// <param name="configureOptions">A delegate that configures the client options.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.</exception>
        public static IServiceCollection AddCippClient(this IServiceCollection services, Action<CippClientOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.AddOptions<CippClientOptions>()
                .Configure(configureOptions)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddTransient<CippTelemetryHandler>();

            services.AddHttpClient(HttpClientName)
                .AddHttpMessageHandler<CippTelemetryHandler>()
                .AddHttpMessageHandler(serviceProvider =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<CippClientOptions>>().Value;
                    return new CippDefaultTenantHandler(options.DefaultTenantFilter);
                })
                .AddStandardResilienceHandler();

            services.AddSingleton(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<CippClientOptions>>().Value;
                var credential = options.Credential
                    ?? serviceProvider.GetService<TokenCredential>()
                    ?? throw new InvalidOperationException(
                        $"No Azure credential was provided. Set {nameof(CippClientOptions)}.{nameof(CippClientOptions.Credential)} or register a {nameof(TokenCredential)} in the service collection.");

                var tokenProvider = new TokenCredentialAccessTokenProvider(
                    credential,
                    options.Scopes,
                    CIPPClient.GetAllowedHosts(options.BaseUrl));
                var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

                var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
                var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
                {
                    BaseUrl = options.BaseUrl.TrimEnd('/')
                };
                return new CIPPClient(adapter);
            });

            return services;
        }
    }
}

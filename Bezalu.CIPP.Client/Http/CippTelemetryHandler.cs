using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bezalu.CIPP.Client.Http
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that adds observability and typed error handling to CIPP requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For every request it starts an <see cref="Activity"/> on the <see cref="ActivitySourceName"/> source
    /// (so consumers can subscribe via OpenTelemetry), logs request/response metadata with secrets redacted,
    /// and converts non-success responses into a <see cref="CippApiException"/> carrying the status code,
    /// a server error snippet, and the correlation/request id.
    /// </para>
    /// <para>
    /// Register this handler <em>outside</em> the resilience handler so retries and <c>Retry-After</c> handling
    /// run to completion before a failure is surfaced as an exception.
    /// </para>
    /// </remarks>
    public sealed class CippTelemetryHandler : DelegatingHandler
    {
        /// <summary>The name of the <see cref="System.Diagnostics.ActivitySource"/> consumers can subscribe to for request spans.</summary>
        public const string ActivitySourceName = "Bezalu.CIPP.Client";

        private const int MaxErrorBodyLength = 512;

        private static readonly string[] RequestIdHeaders =
        [
            "x-ms-request-id",
            "request-id",
            "x-request-id",
            "client-request-id",
        ];

        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        private readonly ILogger<CippTelemetryHandler>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CippTelemetryHandler"/> class.
        /// </summary>
        /// <param name="logger">Optional logger; when omitted, telemetry is limited to the activity span.</param>
        public CippTelemetryHandler(ILogger<CippTelemetryHandler>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var method = request.Method.Method;
            var host = request.RequestUri?.Host;
            // Deliberately omit the query string: CIPP query parameters carry tenant and user identifiers.
            var path = request.RequestUri?.AbsolutePath;

            using var activity = ActivitySource.StartActivity($"CIPP {method}", ActivityKind.Client);
            activity?.SetTag("http.request.method", method);
            if (host is not null)
            {
                activity?.SetTag("server.address", host);
            }
            if (path is not null)
            {
                activity?.SetTag("url.path", path);
            }

            _logger?.LogDebug("CIPP request {Method} {Host}{Path}", method, host, path);

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger?.LogError(ex, "CIPP request {Method} {Host}{Path} failed before a response was received", method, host, path);
                throw;
            }

            var statusCode = (int)response.StatusCode;
            var requestId = ExtractRequestId(response);
            activity?.SetTag("http.response.status_code", statusCode);
            if (requestId is not null)
            {
                activity?.SetTag("cipp.request_id", requestId);
            }

            if (response.IsSuccessStatusCode)
            {
                _logger?.LogDebug(
                    "CIPP response {StatusCode} for {Method} {Host}{Path} (request {RequestId})",
                    statusCode, method, host, path, requestId);
                return response;
            }

            activity?.SetStatus(ActivityStatusCode.Error);
            var errorCode = await ReadErrorSnippetAsync(response, cancellationToken).ConfigureAwait(false);
            _logger?.LogError(
                "CIPP request {Method} {Host}{Path} failed with {StatusCode} (request {RequestId})",
                method, host, path, statusCode, requestId);

            response.Dispose();
            throw new CippApiException($"CIPP API request to {path} failed with status code {statusCode}.")
            {
                StatusCode = statusCode,
                ErrorCode = errorCode,
                RequestId = requestId,
            };
        }

        private static string? ExtractRequestId(HttpResponseMessage response)
        {
            foreach (var header in RequestIdHeaders)
            {
                if (response.Headers.TryGetValues(header, out var values))
                {
                    foreach (var value in values)
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }

            return null;
        }

        private static async Task<string?> ReadErrorSnippetAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.Content is null)
            {
                return null;
            }

            try
            {
                // Read at most MaxErrorBodyLength characters from the stream so large error payloads
                // (HTML error pages, verbose JSON) never allocate the full body during failure storms.
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    using var reader = new StreamReader(stream);
                    var buffer = new char[MaxErrorBodyLength];
                    var totalRead = 0;
                    while (totalRead < MaxErrorBodyLength)
                    {
                        var read = await reader
                            .ReadAsync(buffer.AsMemory(totalRead, MaxErrorBodyLength - totalRead), cancellationToken)
                            .ConfigureAwait(false);
                        if (read == 0)
                        {
                            break;
                        }

                        totalRead += read;
                    }

                    if (totalRead == 0)
                    {
                        return null;
                    }

                    var snippet = new string(buffer, 0, totalRead).Trim();
                    return snippet.Length == 0 ? null : snippet;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return null;
            }
        }
    }
}

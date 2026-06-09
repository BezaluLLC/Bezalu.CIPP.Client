using System.Runtime.CompilerServices;
using Bezalu.CIPP.Client.Api.ListGraphRequest;
using Bezalu.CIPP.Client.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

namespace Bezalu.CIPP.Client
{
    /// <summary>
    /// Cursor-based pagination helpers for CIPP endpoints that return a <c>Metadata.nextLink</c> cursor.
    /// </summary>
    /// <remarks>
    /// CIPP's <c>ListGraphRequest</c> proxy returns <c>{ "Results": [...], "Metadata": { "nextLink": "..." } }</c>.
    /// The Kiota-generated <see cref="StandardResults"/> maps the unmodeled <c>Metadata</c> object into
    /// <see cref="StandardResults.AdditionalData"/>; these helpers extract the cursor and feed it back through
    /// the <c>nextLink</c> query parameter (with <c>manualPagination=true</c>) until the server stops returning one.
    /// </remarks>
    public static class CippPaginationExtensions
    {
        private const string MetadataKey = "Metadata";
        private const string NextLinkKey = "nextLink";

        /// <summary>
        /// Streams every page of a <c>ListGraphRequest</c> query, following the <c>nextLink</c> cursor.
        /// </summary>
        /// <param name="builder">The <c>ListGraphRequest</c> request builder.</param>
        /// <param name="body">The request body describing the Graph endpoint and options.</param>
        /// <param name="configureQuery">
        /// Optional delegate to set query parameters (for example <c>TenantFilter</c> or <c>Endpoint</c>). It is applied to
        /// every page request; the <c>nextLink</c> and <c>manualPagination</c> parameters are managed by the paginator and
        /// override any values set here.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the enumeration.</param>
        /// <returns>An async stream of <see cref="StandardResults"/> pages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="body"/> is null.</exception>
        public static async IAsyncEnumerable<StandardResults> GetAllPagesAsync(
            this ListGraphRequestRequestBuilder builder,
            ListGraphRequestGetRequestBody body,
            Action<ListGraphRequestRequestBuilder.ListGraphRequestRequestBuilderGetQueryParameters>? configureQuery = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(body);

            string? nextLink = null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cursor = nextLink;
                var page = await builder.GetAsync(body, config =>
                {
                    configureQuery?.Invoke(config.QueryParameters);
                    config.QueryParameters.ManualPagination = "true";
                    if (!string.IsNullOrEmpty(cursor))
                    {
                        config.QueryParameters.NextLink = cursor;
                    }
                }, cancellationToken).ConfigureAwait(false);

                if (page is null)
                {
                    yield break;
                }

                yield return page;
                nextLink = page.GetNextLink();
            }
            while (!string.IsNullOrEmpty(nextLink));
        }

        /// <summary>
        /// Streams the individual <c>Results</c> entries across every page of a <c>ListGraphRequest</c> query.
        /// </summary>
        /// <param name="builder">The <c>ListGraphRequest</c> request builder.</param>
        /// <param name="body">The request body describing the Graph endpoint and options.</param>
        /// <param name="configureQuery">Optional delegate to set query parameters; applied to every page request.</param>
        /// <param name="cancellationToken">A token to cancel the enumeration.</param>
        /// <returns>An async stream of result entries, flattened across pages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="body"/> is null.</exception>
        public static async IAsyncEnumerable<string> GetAllResultsAsync(
            this ListGraphRequestRequestBuilder builder,
            ListGraphRequestGetRequestBody body,
            Action<ListGraphRequestRequestBuilder.ListGraphRequestRequestBuilderGetQueryParameters>? configureQuery = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var page in builder.GetAllPagesAsync(body, configureQuery, cancellationToken).ConfigureAwait(false))
            {
                if (page.Results is null)
                {
                    continue;
                }

                foreach (var result in page.Results)
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Extracts the CIPP <c>Metadata.nextLink</c> pagination cursor from a response, if present.
        /// </summary>
        /// <param name="results">The response envelope returned by a CIPP endpoint.</param>
        /// <returns>The <c>nextLink</c> cursor, or <see langword="null"/> when the response has no further pages.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="results"/> is null.</exception>
        public static string? GetNextLink(this StandardResults results)
        {
            ArgumentNullException.ThrowIfNull(results);

            if (results.AdditionalData is null ||
                !results.AdditionalData.TryGetValue(MetadataKey, out var metadata) ||
                metadata is not UntypedObject metadataObject)
            {
                return null;
            }

            if (metadataObject.GetValue().TryGetValue(NextLinkKey, out var nextLinkNode) &&
                nextLinkNode is UntypedString nextLink)
            {
                var value = nextLink.GetValue();
                return string.IsNullOrEmpty(value) ? null : value;
            }

            return null;
        }
    }
}

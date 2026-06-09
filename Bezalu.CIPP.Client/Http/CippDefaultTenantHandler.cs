using System.Web;

namespace Bezalu.CIPP.Client.Http
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that applies a default CIPP <c>tenantFilter</c> query parameter
    /// to outgoing requests that do not already specify one.
    /// </summary>
    /// <remarks>
    /// Most CIPP endpoints target a tenant via a <c>tenantFilter</c> query parameter. This handler lets a
    /// consumer configure a default tenant once (via <see cref="CippClientOptions.DefaultTenantFilter"/>)
    /// while still allowing any individual call to override it: a request that already carries a non-empty
    /// <c>tenantFilter</c> is left untouched. When no default is configured the handler is a transparent
    /// pass-through.
    /// </remarks>
    public sealed class CippDefaultTenantHandler : DelegatingHandler
    {
        private const string TenantFilterParameter = "tenantFilter";

        private readonly string? _defaultTenantFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CippDefaultTenantHandler"/> class.
        /// </summary>
        /// <param name="defaultTenantFilter">
        /// The tenant filter to apply when a request does not specify one. When null or whitespace the
        /// handler does nothing.
        /// </param>
        public CippDefaultTenantHandler(string? defaultTenantFilter)
        {
            _defaultTenantFilter = string.IsNullOrWhiteSpace(defaultTenantFilter) ? null : defaultTenantFilter;
        }

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (_defaultTenantFilter is not null && request.RequestUri is not null)
            {
                request.RequestUri = EnsureTenantFilter(request.RequestUri);
            }

            return base.SendAsync(request, cancellationToken);
        }

        private Uri EnsureTenantFilter(Uri uri)
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (!string.IsNullOrEmpty(query[TenantFilterParameter]))
            {
                // A per-call tenant filter always wins.
                return uri;
            }

            query[TenantFilterParameter] = _defaultTenantFilter;
            return new UriBuilder(uri) { Query = query.ToString() }.Uri;
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Azure.Core;

namespace Bezalu.CIPP.Client
{
    /// <summary>
    /// Options that configure a dependency-injected <see cref="CIPPClient"/>.
    /// </summary>
    public sealed class CippClientOptions
    {
        /// <summary>The CIPP API base URL (e.g., https://your-cipp-instance.azurewebsites.net).</summary>
        [Required(AllowEmptyStrings = false)]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// The scopes requested when acquiring access tokens (for example <c>api://&lt;app-id&gt;/.default</c>).
        /// </summary>
        [Required]
        [MinLength(1)]
        public string[] Scopes { get; set; } = [];

        /// <summary>
        /// The credential used to acquire access tokens. When null, a <see cref="TokenCredential"/> is
        /// resolved from the service provider, allowing the credential to be registered separately.
        /// </summary>
        public TokenCredential? Credential { get; set; }

        /// <summary>
        /// An optional default <c>tenantFilter</c> applied to requests that do not specify one. Individual
        /// calls can still override it by supplying their own <c>tenantFilter</c> query parameter.
        /// </summary>
        public string? DefaultTenantFilter { get; set; }
    }
}

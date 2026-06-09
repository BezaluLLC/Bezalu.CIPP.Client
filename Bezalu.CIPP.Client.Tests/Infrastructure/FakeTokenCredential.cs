using Azure.Core;

namespace Bezalu.CIPP.Client.Tests.Infrastructure
{
    /// <summary>
    /// A <see cref="TokenCredential"/> that returns a fixed token without contacting Entra.
    /// </summary>
    public sealed class FakeTokenCredential : TokenCredential
    {
        private readonly string _token;

        public FakeTokenCredential(string token = "fake-access-token")
        {
            _token = token;
        }

        public int CallCount { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            CallCount++;
            return new AccessToken(_token, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new(GetToken(requestContext, cancellationToken));
    }
}

using System.Threading.Tasks;
using Bezalu.CIPP.Client;

namespace Bezalu.CIPP.Client.Tests
{
    public class StaticAccessTokenProviderTests
    {
        [Fact]
        public async Task GetAuthorizationTokenAsyncReturnsSuppliedToken()
        {
            var provider = new StaticAccessTokenProvider("my-access-token");

            var token = await provider.GetAuthorizationTokenAsync(new Uri("https://contoso.example.com"));

            Assert.Equal("my-access-token", token);
        }

        [Fact]
        public void AllowedHostsValidatorIsInitialized()
        {
            var provider = new StaticAccessTokenProvider("my-access-token");

            Assert.NotNull(provider.AllowedHostsValidator);
        }
    }
}

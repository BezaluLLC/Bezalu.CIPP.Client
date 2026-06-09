using Bezalu.CIPP.Client;

namespace Bezalu.CIPP.Client.Tests
{
    public class CIPPClientCreateTests
    {
        private const string BaseUrl = "https://contoso.example.com/api";
        private const string AccessToken = "my-access-token";

        [Fact]
        public void CreateReturnsClient()
        {
            var client = CIPPClient.Create(BaseUrl, AccessToken);

            Assert.NotNull(client);
        }

        [Fact]
        public void CreateReturnsClientWithApiBuilder()
        {
            var client = CIPPClient.Create(BaseUrl, AccessToken);

            Assert.NotNull(client.Api);
        }

        [Fact]
        public void CreateAcceptsCustomHttpClient()
        {
            using var httpClient = new HttpClient();

            var client = CIPPClient.Create(BaseUrl, AccessToken, httpClient);

            Assert.NotNull(client);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateThrowsWhenBaseUrlIsMissing(string? baseUrl)
        {
            Assert.ThrowsAny<ArgumentException>(() => CIPPClient.Create(baseUrl!, AccessToken));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateThrowsWhenAccessTokenIsMissing(string? accessToken)
        {
            Assert.ThrowsAny<ArgumentException>(() => CIPPClient.Create(BaseUrl, accessToken!));
        }
    }
}

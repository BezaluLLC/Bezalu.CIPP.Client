using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Tests.Infrastructure;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Bezalu.CIPP.Client.Tests
{
    public class CIPPClientCreateTests
    {
        private const string BaseUrl = "https://contoso.example.com";
        private const string AccessToken = "my-access-token";
        private static readonly string[] Scopes = ["api://contoso/.default"];

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

            var requestInfo = client.Api.PublicPing.ToGetRequestInformation();
            Assert.Equal($"{BaseUrl}/api/PublicPing", requestInfo.URI.ToString());
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

        [Fact]
        public void CreateWithCredentialReturnsClient()
        {
            var credential = new FakeTokenCredential();

            var client = CIPPClient.Create(BaseUrl, credential, Scopes);

            Assert.NotNull(client.Api);
        }

        [Fact]
        public void CreateWithCredentialThrowsWhenCredentialIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => CIPPClient.Create(BaseUrl, (Azure.Core.TokenCredential)null!, Scopes));
        }

        [Fact]
        public void CreateWithCredentialThrowsWhenScopesAreEmpty()
        {
            var credential = new FakeTokenCredential();

            Assert.Throws<ArgumentException>(() => CIPPClient.Create(BaseUrl, credential, []));
        }

        [Fact]
        public async Task CreateWithCredentialAcquiresTokenForRequests()
        {
            var credential = new FakeTokenCredential();
            using var handler = new StubHttpMessageHandler(System.Net.HttpStatusCode.OK, "{\"Results\":[]}");
            using var httpClient = new HttpClient(handler);
            var client = CIPPClient.Create(BaseUrl, credential, Scopes, httpClient);

            await client.Api.PublicPing.GetAsync();

            Assert.Equal(1, credential.CallCount);
        }

        [Fact]
        public async Task CreateWithCredentialAttachesBearerToken()
        {
            var credential = new FakeTokenCredential("test-token-value");
            using var handler = new StubHttpMessageHandler(System.Net.HttpStatusCode.OK, "{\"Results\":[]}");
            using var httpClient = new HttpClient(handler);
            var client = CIPPClient.Create(BaseUrl, credential, Scopes, httpClient);

            await client.Api.PublicPing.GetAsync();

            var authHeader = handler.Requests[0].Headers.Authorization;
            Assert.NotNull(authHeader);
            Assert.Equal("Bearer", authHeader!.Scheme);
            Assert.Equal("test-token-value", authHeader.Parameter);
        }

        [Fact]
        public void CreateWithAuthenticationProviderReturnsClient()
        {
            var client = CIPPClient.Create(BaseUrl, new AnonymousAuthenticationProvider());

            Assert.NotNull(client.Api);
        }

        [Fact]
        public void CreateWithAuthenticationProviderThrowsWhenProviderIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => CIPPClient.Create(BaseUrl, (IAuthenticationProvider)null!));
        }
    }
}

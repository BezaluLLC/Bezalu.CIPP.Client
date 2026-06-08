using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Tests.Infrastructure;
using Microsoft.Kiota.Abstractions;
using Moq;

namespace Bezalu.CIPP.Client.Tests
{
    public class CIPPClientTests
    {
        [Fact]
        public void WhenBaseUrlIsEmptyThenClientDefaultsToApi()
        {
            var adapter = new Mock<IRequestAdapter>();
            adapter.SetupAllProperties();
            adapter.Object.BaseUrl = string.Empty;

            _ = new CIPPClient(adapter.Object);

            Assert.Equal("/api", adapter.Object.BaseUrl);
        }

        [Fact]
        public void WhenBaseUrlIsProvidedThenClientKeepsIt()
        {
            var adapter = new Mock<IRequestAdapter>();
            adapter.SetupAllProperties();
            adapter.Object.BaseUrl = "https://contoso.example.com";

            _ = new CIPPClient(adapter.Object);

            Assert.Equal("https://contoso.example.com", adapter.Object.BaseUrl);
        }

        [Fact]
        public void ApiPropertyReturnsRequestBuilder()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.NotNull(client.Api);
        }

        [Fact]
        public void ApiExposesPublicPingBuilder()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.NotNull(client.Api.PublicPing);
        }

        [Fact]
        public void ApiExposesAddUserBuilder()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.NotNull(client.Api.AddUser);
        }

        [Fact]
        public void ApiExposesListUsersBuilder()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.NotNull(client.Api.ListUsers);
        }

        [Fact]
        public void ApiExposesEditGroupBuilder()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.NotNull(client.Api.EditGroup);
        }
    }
}

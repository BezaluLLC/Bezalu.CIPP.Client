using System;
using Bezalu.CIPP.Client.Api.AddUser;
using Bezalu.CIPP.Client.Api.EditGroup;
using Bezalu.CIPP.Client.Api.ListUsers;
using Bezalu.CIPP.Client.Tests.Infrastructure;
using Microsoft.Kiota.Abstractions;

namespace Bezalu.CIPP.Client.Tests
{
    public class RequestBuilderTests
    {
        [Fact]
        public void PublicPingProducesGetRequest()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.PublicPing.ToGetRequestInformation();

            Assert.Equal(Method.GET, requestInfo.HttpMethod);
        }

        [Fact]
        public void PublicPingTargetsExpectedPath()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.PublicPing.ToGetRequestInformation();

            Assert.Equal($"{KiotaTestHelpers.DefaultBaseUrl}/api/PublicPing", requestInfo.URI.ToString());
        }

        [Fact]
        public void PublicPingRequestsJsonAcceptHeader()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.PublicPing.ToGetRequestInformation();

            Assert.True(requestInfo.Headers.TryGetValue("Accept", out var accept));
            Assert.Contains("application/json", accept);
        }

        [Fact]
        public void AddUserProducesPostRequest()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new AddUserPostRequestBody { DisplayName = "Ada Lovelace" };

            var requestInfo = client.Api.AddUser.ToPostRequestInformation(body);

            Assert.Equal(Method.POST, requestInfo.HttpMethod);
        }

        [Fact]
        public void AddUserSerializesBodyDisplayName()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new AddUserPostRequestBody { DisplayName = "Ada Lovelace" };

            var requestInfo = client.Api.AddUser.ToPostRequestInformation(body);

            var content = KiotaTestHelpers.ReadContentAsString(requestInfo);
            Assert.Contains("\"DisplayName\":\"Ada Lovelace\"", content);
        }

        [Fact]
        public void AddUserSerializesBodyUsername()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new AddUserPostRequestBody { Username = "ada" };

            var requestInfo = client.Api.AddUser.ToPostRequestInformation(body);

            var content = KiotaTestHelpers.ReadContentAsString(requestInfo);
            Assert.Contains("\"username\":\"ada\"", content);
        }

        [Fact]
        public void AddUserSetsJsonContentType()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new AddUserPostRequestBody { Username = "ada" };

            var requestInfo = client.Api.AddUser.ToPostRequestInformation(body);

            Assert.True(requestInfo.Headers.TryGetValue("Content-Type", out var contentType));
            Assert.Contains("application/json", contentType);
        }

        [Fact]
        public void AddUserWithNullBodyThrowsArgumentNullException()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.Throws<ArgumentNullException>(() => client.Api.AddUser.ToPostRequestInformation(null!));
        }

        [Fact]
        public void EditGroupProducesPatchRequest()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new EditGroupPatchRequestBody { DisplayName = "Engineering" };

            var requestInfo = client.Api.EditGroup.ToPatchRequestInformation(body);

            Assert.Equal(Method.PATCH, requestInfo.HttpMethod);
        }

        [Fact]
        public void EditGroupSerializesBodyDisplayName()
        {
            var client = KiotaTestHelpers.CreateClient();
            var body = new EditGroupPatchRequestBody { DisplayName = "Engineering" };

            var requestInfo = client.Api.EditGroup.ToPatchRequestInformation(body);

            var content = KiotaTestHelpers.ReadContentAsString(requestInfo);
            Assert.Contains("\"displayName\":\"Engineering\"", content);
        }

        [Fact]
        public void EditGroupWithNullBodyThrowsArgumentNullException()
        {
            var client = KiotaTestHelpers.CreateClient();

            Assert.Throws<ArgumentNullException>(() => client.Api.EditGroup.ToPatchRequestInformation(null!));
        }

        [Fact]
        public void ListUsersMapsTenantFilterQueryParameter()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.ListUsers.ToGetRequestInformation(config =>
            {
                config.QueryParameters.TenantFilter = "contoso.onmicrosoft.com";
            });

            Assert.Contains("tenantFilter=contoso.onmicrosoft.com", requestInfo.URI.ToString());
        }

        [Fact]
        public void ListUsersMapsGraphFilterQueryParameter()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.ListUsers.ToGetRequestInformation(config =>
            {
                config.QueryParameters.GraphFilter = "accountEnabled eq true";
            });

            Assert.Contains("graphFilter=", requestInfo.URI.ToString());
        }

        [Fact]
        public void ListUsersWithoutQueryParametersOmitsOptionalGraphFilter()
        {
            var client = KiotaTestHelpers.CreateClient();

            var requestInfo = client.Api.ListUsers.ToGetRequestInformation();

            Assert.DoesNotContain("graphFilter=", requestInfo.URI.ToString());
        }

        [Fact]
        public void WithUrlOverridesRequestUri()
        {
            var client = KiotaTestHelpers.CreateClient();

            var builder = client.Api.PublicPing.WithUrl("https://override.example.com/api/PublicPing");
            var requestInfo = builder.ToGetRequestInformation();

            Assert.StartsWith("https://override.example.com/api/PublicPing", requestInfo.URI.ToString());
        }
    }
}

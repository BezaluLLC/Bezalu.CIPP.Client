using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bezalu.CIPP.Client.Http;
using Bezalu.CIPP.Client.Tests.Infrastructure;

namespace Bezalu.CIPP.Client.Tests
{
    public class CippDefaultTenantHandlerTests
    {
        [Fact]
        public async Task SendAsyncAddsDefaultTenantFilterWhenMissing()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.OK);
            var uri = await SendAsync("contoso.onmicrosoft.com", "https://contoso.example.com/api/ListUsers", stub);

            Assert.Contains("tenantFilter=contoso.onmicrosoft.com", uri);
        }

        [Fact]
        public async Task SendAsyncPreservesExistingQueryParameters()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.OK);
            var uri = await SendAsync("contoso.onmicrosoft.com", "https://contoso.example.com/api/ListUsers?graphFilter=accountEnabled", stub);

            Assert.Contains("graphFilter=accountEnabled", uri);
        }

        [Fact]
        public async Task SendAsyncDoesNotOverrideExplicitTenantFilter()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.OK);
            var uri = await SendAsync("default.onmicrosoft.com", "https://contoso.example.com/api/ListUsers?tenantFilter=explicit.onmicrosoft.com", stub);

            Assert.Contains("tenantFilter=explicit.onmicrosoft.com", uri);
            Assert.DoesNotContain("default.onmicrosoft.com", uri);
        }

        [Fact]
        public async Task SendAsyncLeavesRequestUnchangedWhenNoDefaultConfigured()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.OK);
            var uri = await SendAsync(null, "https://contoso.example.com/api/ListUsers", stub);

            Assert.DoesNotContain("tenantFilter", uri);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAsyncTreatsWhitespaceDefaultAsNotConfigured(string defaultTenant)
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.OK);
            var uri = await SendAsync(defaultTenant, "https://contoso.example.com/api/ListUsers", stub);

            Assert.DoesNotContain("tenantFilter", uri);
        }

        private static async Task<string> SendAsync(string? defaultTenantFilter, string requestUri, StubHttpMessageHandler innerHandler)
        {
            using var handler = new CippDefaultTenantHandler(defaultTenantFilter) { InnerHandler = innerHandler };
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            await invoker.SendAsync(request, CancellationToken.None);
            return innerHandler.Requests[0].RequestUri!.ToString();
        }
    }
}

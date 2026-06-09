using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Http;
using Bezalu.CIPP.Client.Tests.Infrastructure;

namespace Bezalu.CIPP.Client.Tests
{
    public class CippTelemetryHandlerTests
    {
        private const string RequestUri = "https://contoso.example.com/api/ListUsers?tenantFilter=contoso.onmicrosoft.com";

        [Fact]
        public async Task SendAsyncReturnsResponseOnSuccess()
        {
            using var response = await SendAsync(new StubHttpMessageHandler(HttpStatusCode.OK, "{\"Results\":[]}"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendAsyncThrowsCippApiExceptionOnFailure()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.BadRequest, "Invalid tenant");

            var ex = await Assert.ThrowsAsync<CippApiException>(() => SendAsync(stub));

            Assert.Equal(400, ex.StatusCode);
        }

        [Fact]
        public async Task SendAsyncCapturesErrorBodyAsErrorCode()
        {
            var stub = new StubHttpMessageHandler(HttpStatusCode.BadRequest, "Invalid tenant");

            var ex = await Assert.ThrowsAsync<CippApiException>(() => SendAsync(stub));

            Assert.Equal("Invalid tenant", ex.ErrorCode);
        }

        [Fact]
        public async Task SendAsyncCapturesRequestId()
        {
            var stub = new StubHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("boom"),
                };
                response.Headers.TryAddWithoutValidation("x-ms-request-id", "abc-123");
                return response;
            });

            var ex = await Assert.ThrowsAsync<CippApiException>(() => SendAsync(stub));

            Assert.Equal("abc-123", ex.RequestId);
        }

        [Fact]
        public async Task SendAsyncTruncatesLongErrorBodies()
        {
            var longBody = new string('x', 1000);
            var stub = new StubHttpMessageHandler(HttpStatusCode.BadRequest, longBody);

            var ex = await Assert.ThrowsAsync<CippApiException>(() => SendAsync(stub));

            Assert.Equal(512, ex.ErrorCode!.Length);
        }

        private static async Task<HttpResponseMessage> SendAsync(HttpMessageHandler innerHandler)
        {
            using var handler = new CippTelemetryHandler { InnerHandler = innerHandler };
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
            return await invoker.SendAsync(request, CancellationToken.None);
        }
    }
}

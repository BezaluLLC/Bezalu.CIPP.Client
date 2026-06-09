using System.Net;

namespace Bezalu.CIPP.Client.Tests.Infrastructure
{
    /// <summary>
    /// A test <see cref="HttpMessageHandler"/> that returns a queued or factory-produced response
    /// and records the requests it observed.
    /// </summary>
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public StubHttpMessageHandler(HttpStatusCode statusCode, string? content = null, string mediaType = "application/json")
            : this(_ => CreateResponse(statusCode, content, mediaType))
        {
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responder(request));
        }

        private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string? content, string mediaType)
        {
            var response = new HttpResponseMessage(statusCode);
            if (content is not null)
            {
                response.Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType);
            }

            return response;
        }
    }
}

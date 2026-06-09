using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Api.ListGraphRequest;
using Bezalu.CIPP.Client.Models;
using Bezalu.CIPP.Client.Tests.Infrastructure;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using Moq;

namespace Bezalu.CIPP.Client.Tests
{
    public class CippPaginationExtensionsTests
    {
        [Fact]
        public void GetNextLinkReturnsCursorWhenMetadataPresent()
        {
            var results = CreatePage(["a"], "cursor1");

            var nextLink = results.GetNextLink();

            Assert.Equal("cursor1", nextLink);
        }

        [Fact]
        public void GetNextLinkReturnsNullWhenMetadataMissing()
        {
            var results = new StandardResults { Results = ["a"] };

            var nextLink = results.GetNextLink();

            Assert.Null(nextLink);
        }

        [Fact]
        public void GetNextLinkReturnsNullWhenMetadataHasNoNextLink()
        {
            var results = new StandardResults
            {
                AdditionalData = new Dictionary<string, object>
                {
                    ["Metadata"] = new UntypedObject(new Dictionary<string, UntypedNode>
                    {
                        ["total"] = new UntypedString("5"),
                    }),
                },
            };

            var nextLink = results.GetNextLink();

            Assert.Null(nextLink);
        }

        [Fact]
        public async Task GetNextLinkReadsCursorFromDeserializedResponse()
        {
            const string json = "{ \"Results\": [\"a\"], \"Metadata\": { \"nextLink\": \"cursor-from-json\" } }";

            var results = await KiotaTestHelpers.DeserializeFromJsonAsync(json, StandardResults.CreateFromDiscriminatorValue);

            Assert.Equal("cursor-from-json", results!.GetNextLink());
        }

        [Fact]
        public void GetNextLinkThrowsWhenResultsIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ((StandardResults)null!).GetNextLink());
        }

        [Fact]
        public async Task GetAllPagesAsyncFollowsNextLinkUntilExhausted()
        {
            var capturedUris = new List<string>();
            var client = CreatePagingClient(capturedUris, CreatePage(["a"], "cursor1"), CreatePage(["b"], null));

            var pages = new List<StandardResults>();
            await foreach (var page in client.Api.ListGraphRequest.GetAllPagesAsync(new ListGraphRequestGetRequestBody()))
            {
                pages.Add(page);
            }

            Assert.Equal(2, pages.Count);
        }

        [Fact]
        public async Task GetAllPagesAsyncSendsCursorOnSubsequentRequests()
        {
            var capturedUris = new List<string>();
            var client = CreatePagingClient(capturedUris, CreatePage(["a"], "cursor1"), CreatePage(["b"], null));

            await foreach (var _ in client.Api.ListGraphRequest.GetAllPagesAsync(new ListGraphRequestGetRequestBody()))
            {
            }

            Assert.DoesNotContain("nextLink=", capturedUris[0]);
            Assert.Contains("nextLink=cursor1", capturedUris[1]);
        }

        [Fact]
        public async Task GetAllPagesAsyncAlwaysSetsManualPagination()
        {
            var capturedUris = new List<string>();
            var client = CreatePagingClient(capturedUris, CreatePage(["a"], null));

            await foreach (var _ in client.Api.ListGraphRequest.GetAllPagesAsync(new ListGraphRequestGetRequestBody()))
            {
            }

            Assert.Contains("manualPagination=true", capturedUris[0]);
        }

        [Fact]
        public async Task GetAllResultsAsyncFlattensResultsAcrossPages()
        {
            var capturedUris = new List<string>();
            var client = CreatePagingClient(capturedUris, CreatePage(["a", "b"], "cursor1"), CreatePage(["c"], null));

            var results = new List<string>();
            await foreach (var result in client.Api.ListGraphRequest.GetAllResultsAsync(new ListGraphRequestGetRequestBody()))
            {
                results.Add(result);
            }

            Assert.Equal(["a", "b", "c"], results);
        }

        [Fact]
        public async Task GetAllPagesAsyncThrowsWhenBodyIsNull()
        {
            var client = KiotaTestHelpers.CreateClient();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in client.Api.ListGraphRequest.GetAllPagesAsync(null!))
                {
                }
            });
        }

        private static StandardResults CreatePage(List<string> results, string? nextLink)
        {
            var page = new StandardResults { Results = results };
            if (nextLink is not null)
            {
                page.AdditionalData["Metadata"] = new UntypedObject(new Dictionary<string, UntypedNode>
                {
                    ["nextLink"] = new UntypedString(nextLink),
                });
            }

            return page;
        }

        private static CIPPClient CreatePagingClient(List<string> capturedUris, params StandardResults[] pages)
        {
            var responses = new Queue<StandardResults>(pages);
            var adapter = new Mock<IRequestAdapter>();
            adapter.SetupAllProperties();
            adapter.SetupGet(a => a.SerializationWriterFactory).Returns(new JsonSerializationWriterFactory());
            adapter.Object.BaseUrl = KiotaTestHelpers.DefaultBaseUrl;

            adapter.Setup(a => a.SendAsync(
                    It.IsAny<RequestInformation>(),
                    It.IsAny<ParsableFactory<StandardResults>>(),
                    It.IsAny<Dictionary<string, ParsableFactory<IParsable>>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((RequestInformation requestInfo, ParsableFactory<StandardResults> _, Dictionary<string, ParsableFactory<IParsable>>? _, CancellationToken _) =>
                {
                    capturedUris.Add(requestInfo.URI.ToString());
                    return Task.FromResult<StandardResults?>(responses.Count > 0 ? responses.Dequeue() : null);
                });

            return new CIPPClient(adapter.Object);
        }
    }
}

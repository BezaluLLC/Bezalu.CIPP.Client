using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bezalu.CIPP.Client;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using Moq;

namespace Bezalu.CIPP.Client.Tests.Infrastructure
{
    /// <summary>
    /// Shared helpers for testing the Kiota-generated SDK without performing real HTTP calls.
    /// Request builders only need an <see cref="IRequestAdapter"/> for its serialization writer
    /// factory and base URL, so the adapter is mocked and the real JSON serializer is used.
    /// </summary>
    public static class KiotaTestHelpers
    {
        public const string DefaultBaseUrl = "https://example.com";

        /// <summary>
        /// Creates a mock <see cref="IRequestAdapter"/> that returns a real JSON serialization
        /// writer factory so request bodies serialize exactly as they would in production.
        /// </summary>
        public static Mock<IRequestAdapter> CreateMockAdapter(string? baseUrl = DefaultBaseUrl)
        {
            var adapter = new Mock<IRequestAdapter>();
            adapter.SetupAllProperties();
            adapter.SetupGet(a => a.SerializationWriterFactory).Returns(new JsonSerializationWriterFactory());
            adapter.Object.BaseUrl = baseUrl;
            return adapter;
        }

        /// <summary>
        /// Builds a <see cref="CIPPClient"/> backed by a mock adapter with the supplied base URL.
        /// </summary>
        public static CIPPClient CreateClient(string? baseUrl = DefaultBaseUrl)
        {
            return new CIPPClient(CreateMockAdapter(baseUrl).Object);
        }

        /// <summary>
        /// Reads the serialized request body from a built <see cref="RequestInformation"/>.
        /// </summary>
        public static string ReadContentAsString(RequestInformation requestInformation)
        {
            var content = requestInformation.Content;
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Serializes an <see cref="IParsable"/> model to JSON using the Kiota JSON writer.
        /// </summary>
        public static async Task<string> SerializeToJsonAsync(IParsable model)
        {
            var writerFactory = new JsonSerializationWriterFactory();
            using var writer = writerFactory.GetSerializationWriter("application/json");
            writer.WriteObjectValue(string.Empty, model);
            using var contentStream = writer.GetSerializedContent();
            using var reader = new StreamReader(contentStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Deserializes JSON back into an <see cref="IParsable"/> model using the Kiota JSON parser.
        /// </summary>
        public static async Task<T?> DeserializeFromJsonAsync<T>(string json, ParsableFactory<T> factory)
            where T : IParsable
        {
            var parseNodeFactory = new JsonParseNodeFactory();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var rootNode = await parseNodeFactory.GetRootParseNodeAsync("application/json", stream);
            return rootNode.GetObjectValue(factory);
        }
    }
}

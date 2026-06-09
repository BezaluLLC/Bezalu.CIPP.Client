using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bezalu.CIPP.Client.Tests
{
    public class CippClientServiceCollectionExtensionsTests
    {
        private const string BaseUrl = "https://contoso.example.com";
        private static readonly string[] Scopes = ["api://contoso/.default"];

        [Fact]
        public void AddCippClientRegistersClient()
        {
            var services = new ServiceCollection();
            services.AddCippClient(options =>
            {
                options.BaseUrl = BaseUrl;
                options.Scopes = Scopes;
                options.Credential = new FakeTokenCredential();
            });

            using var provider = services.BuildServiceProvider();
            var client = provider.GetService<CIPPClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public void AddCippClientResolvesCredentialFromContainer()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TokenCredential>(new FakeTokenCredential());
            services.AddCippClient(options =>
            {
                options.BaseUrl = BaseUrl;
                options.Scopes = Scopes;
            });

            using var provider = services.BuildServiceProvider();
            var client = provider.GetService<CIPPClient>();

            Assert.NotNull(client);
        }

        [Fact]
        public void AddCippClientThrowsWhenNoCredentialAvailable()
        {
            var services = new ServiceCollection();
            services.AddCippClient(options =>
            {
                options.BaseUrl = BaseUrl;
                options.Scopes = Scopes;
            });

            using var provider = services.BuildServiceProvider();

            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<CIPPClient>());
        }

        [Fact]
        public void AddCippClientValidatesMissingBaseUrlOnStart()
        {
            var services = new ServiceCollection();
            services.AddCippClient(options =>
            {
                options.Scopes = Scopes;
                options.Credential = new FakeTokenCredential();
            });

            using var provider = services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CippClientOptions>>().Value);
        }

        [Fact]
        public void AddCippClientValidatesEmptyScopesOnStart()
        {
            var services = new ServiceCollection();
            services.AddCippClient(options =>
            {
                options.BaseUrl = BaseUrl;
                options.Credential = new FakeTokenCredential();
            });

            using var provider = services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CippClientOptions>>().Value);
        }

        [Fact]
        public void AddCippClientThrowsWhenServicesIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddCippClient(_ => { }));
        }

        [Fact]
        public void AddCippClientThrowsWhenConfigureOptionsIsNull()
        {
            var services = new ServiceCollection();

            Assert.Throws<ArgumentNullException>(() => services.AddCippClient(null!));
        }
    }
}

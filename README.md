# Bezalu.CIPP.Client

A strongly-typed .NET client SDK for the [CIPP API](https://docs.cipp.app/api-documentation/setup-and-authentication), generated from the CIPP OpenAPI description with [Microsoft Kiota](https://learn.microsoft.com/openapi/kiota/).

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Kiota](https://img.shields.io/badge/Kiota-2.0.0-5C2D91)](https://learn.microsoft.com/openapi/kiota/)

---

## Overview

CIPP (the CyberDrain Improved Partner Portal) exposes a large HTTP API for managing Microsoft 365 across many tenants. `Bezalu.CIPP.Client` wraps that API in an idiomatic, discoverable .NET surface so you can call it with compile-time safety instead of hand-crafting `HttpClient` requests and parsing raw JSON.

The client is **generated**, not hand-written: the entire request/response surface is produced by Kiota from `Ref/CIPP-OpenAPI.json`. That means the SDK stays faithful to the API contract and can be regenerated whenever the spec changes.

> This README focuses on the **SDK** — how to authenticate, call endpoints, and consume results. For what the endpoints *do*, see the official [CIPP API documentation](https://docs.cipp.app/api-documentation/endpoints).

## Features

- **Fully typed fluent API** — every endpoint is reachable via `client.Api.<Endpoint>` with typed request bodies, query parameters, and responses.
- **~559 endpoints** covering identity, tenants, Exchange, Intune/endpoint, security & compliance, standards, and tooling.
- **Async end-to-end** — every operation is awaitable and accepts a `CancellationToken`.
- **Pluggable authentication** — works with any Kiota `IAuthenticationProvider` (bearer token, Azure identity, anonymous).
- **Pluggable transport** — bring your own `HttpClient` for proxies, retries, logging, or custom middleware.
- **Multi-format serialization** — JSON, text, form-urlencoded, and multipart writers are registered out of the box.
- **Request inspection** — build `RequestInformation` without sending, which makes the client easy to unit test and ideal for wrapping inside an MCP server or other orchestration layer.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/) or later
- Access to a CIPP API instance and credentials (see [Setup & Authentication](https://docs.cipp.app/api-documentation/setup-and-authentication))

## Installation

Reference the project directly:

```xml
<ItemGroup>
  <ProjectReference Include="..\Bezalu.CIPP.Client\Bezalu.CIPP.Client.csproj" />
</ItemGroup>
```

Or, if you distribute it as a NuGet package:

```bash
dotnet add package Bezalu.CIPP.Client
```

The SDK depends on [`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle), which provides the request adapter, HTTP transport, authentication abstractions, and serializers.

## Getting started

A `CIPPClient` is constructed from an `IRequestAdapter`. The adapter pairs an authentication provider with an `HttpClient`, and points at your CIPP API base URL.

### Quick start (bearer token)

If you already have a bearer token, the `CIPPClient.Create` factory wires up the access token provider, authentication provider, and request adapter for you:

```csharp
using Bezalu.CIPP.Client;

var client = CIPPClient.Create(
    "https://your-cipp-instance.azurewebsites.net",
    accessToken);

var pong = await client.Api.PublicPing.GetAsync();
```

Pass your own `HttpClient` as the optional third argument when you need custom transport (proxies, retries, logging). For token refresh or `Azure.Identity` scenarios, construct the adapter manually as shown below.

### Authenticated client

CIPP authenticates through a Microsoft Entra app registration. Acquire a bearer token for the CIPP API scope (see the [Setup & Authentication](https://docs.cipp.app/api-documentation/setup-and-authentication) guide), then supply it through an access token provider:

```csharp
using Bezalu.CIPP.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

// Provides access tokens to the request pipeline.
sealed class CippTokenProvider(Func<CancellationToken, Task<string>> tokenFactory) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public async Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
        => await tokenFactory(cancellationToken);
}

var authProvider = new BaseBearerTokenAuthenticationProvider(
    new CippTokenProvider(async ct => await GetAccessTokenAsync(ct)));

var adapter = new HttpClientRequestAdapter(authProvider)
{
    BaseUrl = "https://your-cipp-instance.azurewebsites.net/api"
};

var client = new CIPPClient(adapter);
```

> If you use `Azure.Identity`, you can swap in `AzureIdentityAuthenticationProvider` with a `ClientSecretCredential` instead of writing a token provider.

### Anonymous client (health checks / local testing)

```csharp
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider())
{
    BaseUrl = "https://your-cipp-instance.azurewebsites.net/api"
};

var client = new CIPPClient(adapter);
```

If the base URL is left unset, the client defaults to the relative path `/api`.

## Usage

### Health check

```csharp
var pong = await client.Api.PublicPing.GetAsync();
Console.WriteLine(string.Join(", ", pong?.Results ?? []));
```

### List resources (with query parameters)

```csharp
var users = await client.Api.ListUsers.GetAsync(config =>
{
    config.QueryParameters.TenantFilter = "contoso.onmicrosoft.com";
    config.QueryParameters.GraphFilter = "accountEnabled eq true";
});
```

### Create a resource (POST)

```csharp
using Bezalu.CIPP.Client.Api.AddUser;

var newUser = new AddUserPostRequestBody
{
    TenantFilter = "contoso.onmicrosoft.com",
    DisplayName  = "Ada Lovelace",
    Username     = "ada",
    MailNickname = "ada",
};

var result = await client.Api.AddUser.PostAsync(newUser);
```

### Update a resource (PATCH)

```csharp
using Bezalu.CIPP.Client.Api.EditGroup;

var edit = new EditGroupPatchRequestBody
{
    TenantFilter = "contoso.onmicrosoft.com",
    DisplayName  = "Engineering",
};

var result = await client.Api.EditGroup.PatchAsync(edit);
```

### Cancellation

Every operation accepts a `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var users = await client.Api.ListUsers.GetAsync(cancellationToken: cts.Token);
```

### Inspect a request without sending it

Useful for logging, testing, or relaying through another service (for example, an MCP server):

```csharp
var requestInfo = client.Api.AddUser.ToPostRequestInformation(newUser);
// requestInfo.HttpMethod, requestInfo.URI, requestInfo.Headers, requestInfo.Content
```

## API surface

Endpoints are exposed under `client.Api` and follow consistent verb conventions that mirror the CIPP API:

| Prefix     | Verb    | Purpose                          | Example                         |
|------------|---------|----------------------------------|---------------------------------|
| `List*`    | GET     | Query resources                  | `client.Api.ListUsers`          |
| `Add*`     | POST    | Create resources                 | `client.Api.AddGroup`           |
| `Edit*`    | PATCH   | Update existing resources        | `client.Api.EditGroup`          |
| `Exec*`    | POST    | Execute actions / run operations | `client.Api.ExecTestRun`        |
| `Remove*`  | POST    | Delete resources                 | `client.Api.RemoveGroup`        |

Most write operations target a tenant via a `TenantFilter` value and return a shared `StandardResults` envelope (a `Results` string collection plus any additional data). Browse the generated folders under `Bezalu.CIPP.Client/Api/` for the full list, or consult the [CIPP endpoint reference](https://docs.cipp.app/api-documentation/endpoints).

## Error handling

Failures surface as exceptions from the request adapter. Wrap calls and handle the Kiota `ApiException` (and standard transport/cancellation exceptions):

```csharp
using Microsoft.Kiota.Abstractions;

try
{
    var users = await client.Api.ListUsers.GetAsync(config =>
        config.QueryParameters.TenantFilter = "contoso.onmicrosoft.com");
}
catch (ApiException ex)
{
    Console.Error.WriteLine($"CIPP API returned {ex.ResponseStatusCode}: {ex.Message}");
}
```

## Testing

The solution includes a contract-level test suite (`Bezalu.CIPP.Client.Tests`) using **xUnit** and **Moq**. Because the client is generated, the tests verify the things that actually break across regenerations:

- **Client wiring** — base-URL defaulting and builder exposure.
- **Request construction** — HTTP method, URL/path, headers, query-parameter mapping, and body serialization.
- **Serialization round-trips** — models serialize and deserialize through the Kiota JSON pipeline without data loss.

Run the tests:

```bash
dotnet test
```

Collect coverage:

```bash
dotnet tool install -g dotnet-coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

## Regenerating the client

The SDK is generated by Kiota from the OpenAPI description at `Bezalu.CIPP.Client/Ref/CIPP-OpenAPI.json`. After updating the spec, regenerate with:

```bash
kiota generate \
  --openapi Bezalu.CIPP.Client/Ref/CIPP-OpenAPI.json \
  --language CSharp \
  --class-name CIPPClient \
  --namespace-name Bezalu.CIPP.Client \
  --output Bezalu.CIPP.Client
```

Generation settings are tracked in `kiota-lock.json`. Do not hand-edit files marked `// <auto-generated/>` — they are overwritten on every regeneration.

## Project structure

```
Bezalu.CIPP.Client/
├─ CIPPClient.cs            # SDK entry point (client + configuration)
├─ Api/                     # Generated request builders (one folder per endpoint)
├─ Models/                  # Shared response/request models
├─ Ref/CIPP-OpenAPI.json    # OpenAPI source of truth
└─ kiota-lock.json          # Kiota generation settings

Bezalu.CIPP.Client.Tests/   # xUnit + Moq contract tests
```

## Acknowledgements

- [CIPP](https://github.com/KelvinTegelaar/CIPP) and the [CIPP API](https://docs.cipp.app/) by Kelvin Tegelaar and the CyberDrain community.
- [Microsoft Kiota](https://learn.microsoft.com/openapi/kiota/) for client generation.

## License

See [LICENSE](LICENSE) for details.
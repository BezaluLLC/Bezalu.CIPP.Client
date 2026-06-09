# Bezalu.CIPP.Client

A strongly-typed .NET client SDK for the [CIPP API](https://docs.cipp.app/api-documentation/setup-and-authentication), generated from the CIPP OpenAPI description with [Microsoft Kiota](https://learn.microsoft.com/openapi/kiota/).

[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)](https://dotnet.microsoft.com/)
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
- **Credential-based authentication** — pass an `Azure.Core.TokenCredential` (e.g., `DefaultAzureCredential`, `ClientSecretCredential`, `ManagedIdentityCredential`) and token acquisition, caching, and refresh are handled for you. A static-token escape hatch remains for tests and short-lived tokens.
- **First-class dependency injection** — `services.AddCippClient(...)` wires up validated options, an `IHttpClientFactory`-managed transport, resilience, and authentication.
- **Built-in resilience** — the default transport uses the standard resilience pipeline (retry with exponential backoff + jitter, circuit breaker, per-attempt timeout) and honors `Retry-After` on `429`/`503` throttling responses.
- **Typed errors & observability** — failures surface as a `CippApiException` (status code, server error snippet, correlation/request id), and every request emits an `ActivitySource` span plus `ILogger` diagnostics with query strings redacted.
- **Pagination helpers** — `await foreach` over cursor/`nextLink` paging with `GetAllPagesAsync` / `GetAllResultsAsync`.
- **Multi-tenancy convenience** — configure a default `tenantFilter` once and override it per call.
- **Pluggable transport** — bring your own `HttpClient` for proxies, custom middleware, or full control.
- **Request inspection** — build `RequestInformation` without sending, which makes the client easy to unit test and ideal for wrapping inside an MCP server or other orchestration layer.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/) or later (the package multi-targets `net8.0`, `net9.0`, and `net10.0`)
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

The SDK depends on [`Microsoft.Kiota.Bundle`](https://www.nuget.org/packages/Microsoft.Kiota.Bundle) (request adapter, HTTP transport, authentication abstractions, serializers), [`Azure.Identity`](https://www.nuget.org/packages/Azure.Identity) (credential-based authentication), and [`Microsoft.Extensions.Http.Resilience`](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) (retry, circuit breaker, and timeout pipeline).

## Getting started

The recommended path registers the client through dependency injection with an `Azure.Core.TokenCredential`. This gives you automatic token acquisition and refresh, an `IHttpClientFactory`-managed transport, built-in resilience, telemetry, and typed errors. A static-token factory remains available as an escape hatch for tests and short-lived tokens.

### Recommended: dependency injection + credential

Register the client once and inject `CIPPClient` wherever you need it:

```csharp
using Azure.Identity;
using Bezalu.CIPP.Client;

builder.Services.AddCippClient(options =>
{
    options.BaseUrl = "https://your-cipp-instance.azurewebsites.net";
    options.Scopes = ["api://<cipp-app-id>/.default"];

    // Token acquisition, caching, and refresh are handled for you.
    options.Credential = new DefaultAzureCredential();

    // Optional: applied to every request unless overridden per call.
    options.DefaultTenantFilter = "contoso.onmicrosoft.com";
});
```

```csharp
public sealed class TenantReport(CIPPClient client)
{
    public async Task<IReadOnlyList<string>> GetUsersAsync(CancellationToken ct)
    {
        var users = await client.Api.ListUsers.GetAsync(cancellationToken: ct);
        return users?.Results ?? [];
    }
}
```

The credential can be any `Azure.Core.TokenCredential` — `DefaultAzureCredential`, `ClientSecretCredential`, `ManagedIdentityCredential`, and so on:

```csharp
options.Credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
```

You can also leave `options.Credential` unset and register a `TokenCredential` in the container; `AddCippClient` will resolve it from DI.

This registration wires up:

- a typed `HttpClient` from `IHttpClientFactory`,
- the standard resilience pipeline (retry with exponential backoff + jitter, circuit breaker, per-attempt timeout) that honors `Retry-After` on `429`/`503`,
- a telemetry handler that emits an `ActivitySource` span and `ILogger` diagnostics per request, and
- conversion of failed responses into a typed `CippApiException`.

`CippClientOptions` is validated with `ValidateDataAnnotations().ValidateOnStart()`, so a missing `BaseUrl` or empty `Scopes` fails fast at startup.

### Credential-based client without DI

If you are not using a service container, the `CIPPClient.Create` factory accepts a `TokenCredential` directly:

```csharp
using Azure.Identity;
using Bezalu.CIPP.Client;

var client = CIPPClient.Create(
    "https://your-cipp-instance.azurewebsites.net",
    new DefaultAzureCredential(),
    scopes: ["api://<cipp-app-id>/.default"]);

var pong = await client.Api.PublicPing.GetAsync();
```

### Escape hatch: static bearer token

If you already have a bearer token (tests, short-lived scenarios), the `CIPPClient.Create` factory wires up the access token provider, authentication provider, and request adapter for you. Note that this token is **not** refreshed when it expires:

```csharp
using Bezalu.CIPP.Client;

var client = CIPPClient.Create(
    "https://your-cipp-instance.azurewebsites.net",
    accessToken);

var pong = await client.Api.PublicPing.GetAsync();
```

Pass your own `HttpClient` as the optional argument when you need custom transport (proxies, custom middleware).

### Bring your own authentication provider

For full control, supply any Kiota `IAuthenticationProvider`:

```csharp
using Bezalu.CIPP.Client;
using Microsoft.Kiota.Abstractions.Authentication;

var client = CIPPClient.Create(
    "https://your-cipp-instance.azurewebsites.net",
    new BaseBearerTokenAuthenticationProvider(myAccessTokenProvider));
```

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

### Pagination

CIPP's `ListGraphRequest` proxy returns results in cursor-paged batches (`Metadata.nextLink`). The pagination helpers follow the cursor for you and expose the results as an `IAsyncEnumerable`, so you can `await foreach` over pages or flattened entries:

```csharp
using Bezalu.CIPP.Client;
using Bezalu.CIPP.Client.Api.ListGraphRequest;

var body = new ListGraphRequestGetRequestBody();

// Stream individual results across every page.
await foreach (var entry in client.Api.ListGraphRequest.GetAllResultsAsync(
    body,
    config =>
    {
        config.TenantFilter = "contoso.onmicrosoft.com";
        config.Endpoint = "users";
    },
    cancellationToken))
{
    Console.WriteLine(entry);
}

// Or work page by page.
await foreach (var page in client.Api.ListGraphRequest.GetAllPagesAsync(body, cancellationToken: cancellationToken))
{
    Console.WriteLine($"Page with {page.Results?.Count ?? 0} results");
}
```

The paginator manages the `nextLink` and `manualPagination` query parameters automatically; any values you set for them in `configureQuery` are overridden.

### Multi-tenancy

When you configure `DefaultTenantFilter` (via `AddCippClient`), it is applied to every request that does not already specify one. Override it per call by setting `TenantFilter` on the request's query parameters:

```csharp
// Uses the configured DefaultTenantFilter.
var defaultTenantUsers = await client.Api.ListUsers.GetAsync();

// Overrides it for this call only.
var otherTenantUsers = await client.Api.ListUsers.GetAsync(config =>
    config.QueryParameters.TenantFilter = "fabrikam.onmicrosoft.com");
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

When you use the DI registration or a credential-based client, failed responses are converted into a typed `CippApiException` that carries the HTTP status code, the server-provided error snippet, and the correlation/request id for support and tracing:

```csharp
using Bezalu.CIPP.Client;

try
{
    var users = await client.Api.ListUsers.GetAsync(config =>
        config.QueryParameters.TenantFilter = "contoso.onmicrosoft.com");
}
catch (CippApiException ex)
{
    Console.Error.WriteLine(
        $"CIPP API returned {ex.StatusCode} (request {ex.RequestId}): {ex.ErrorCode}");
}
```

`CippApiException` derives from the Kiota `ApiException`, so you can still catch `ApiException` if you prefer to handle CIPP and other Kiota clients uniformly.

## Testing

The solution includes a test suite (`Bezalu.CIPP.Client.Tests`) using **xUnit** and **Moq**. It covers both the generated contract (so regenerations stay safe) and the hand-written support layer:

- **Client wiring** — base-URL defaulting and builder exposure.
- **Request construction** — HTTP method, URL/path, headers, query-parameter mapping, and body serialization.
- **Serialization round-trips** — models serialize and deserialize through the Kiota JSON pipeline without data loss.
- **Authentication & DI** — `CIPPClient.Create` overloads, `AddCippClient` registration, credential resolution, and options validation.
- **Resilience & diagnostics** — telemetry/error mapping into `CippApiException`, default tenant-filter injection, and cursor pagination.

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
├─ CIPPClient.cs                  # Generated SDK entry point (client + configuration)
├─ CIPPClient.Create.cs           # Factory overloads (token credential, static token, custom auth)
├─ CippClientOptions.cs           # Validated DI options (endpoint, scopes, default tenant filter)
├─ ServiceCollectionExtensions.cs # AddCippClient(...) registration
├─ CippApiException.cs            # Typed API exception (status, error code, request id)
├─ CippPaginationExtensions.cs    # Cursor/nextLink pagination helpers
├─ Authentication/                # TokenCredential → Kiota access-token bridge
├─ Http/                          # Telemetry and default-tenant message handlers
├─ Api/                           # Generated request builders (one folder per endpoint)
├─ Models/                        # Shared response/request models
├─ Ref/CIPP-OpenAPI.json          # OpenAPI source of truth
└─ kiota-lock.json                # Kiota generation settings

Bezalu.CIPP.Client.Tests/         # xUnit + Moq test suite
```

The hand-written support files above live outside the generated `Api/` and `Models/` folders, so regenerating the client with Kiota leaves them untouched.

## Acknowledgements

- [CIPP](https://github.com/KelvinTegelaar/CIPP) and the [CIPP API](https://docs.cipp.app/) by Kelvin Tegelaar and the CyberDrain community.
- [Microsoft Kiota](https://learn.microsoft.com/openapi/kiota/) for client generation.

## License

See [LICENSE](LICENSE) for details.
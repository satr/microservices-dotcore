# Stage 5: Security — Authentication and Authorization

## Scope Covered

1. **Keycloak** identity server container — realm, users, roles pre-configured via realm import
2. **JWT Bearer** validation in all three microservices — controllers protected with `[Authorize]`
3. **Role-based policies** — `member` borrows books, `librarian` manages them
4. **OIDC login in the frontend** — browser redirected to Keycloak, tokens stored in cookie
5. **Token propagation** — access token forwarded as `Authorization: Bearer` header on every service-to-service call for both auth and audit

---

## What Was Implemented

### Keycloak Container

Added to `docker-compose.yml`:

```yaml
keycloak:
  image: quay.io/keycloak/keycloak:26.2
  command: start-dev --import-realm
  environment:
    KC_BOOTSTRAP_ADMIN_USERNAME: admin
    KC_BOOTSTRAP_ADMIN_PASSWORD: admin
    KC_HOSTNAME: localhost
    KC_HOSTNAME_PORT: 8888        # tokens carry iss=http://localhost:8888/realms/library
    KC_HOSTNAME_STRICT: "false"
    KC_HOSTNAME_STRICT_BACKCHANNEL: "false"
  ports:
    - "8888:8080"
  volumes:
    - ./docker/keycloak-realm.json:/opt/keycloak/data/import/realm.json
```

**Admin UI:** http://localhost:8888 — admin / admin

Realm `library` is imported automatically on first start from `docker/keycloak-realm.json`.

### Pre-configured Accounts

| Username | Password | Role |
|---|---|---|
| `user1` | `user1` | `member` |
| `user2` | `user2` | `member` |
| `librarian1` | `librarian1` | `librarian` |

### Realm Configuration (`docker/keycloak-realm.json`)

- **Client** `library-frontend` — public OIDC client, PKCE enabled, redirect URI `http://localhost:8080/*`
- **Realm roles** — `member`, `librarian`
- **Protocol mapper** — realm roles mapped to a flat `roles` string array in the access token (claim name `roles`), so `[Authorize(Roles = "member")]` works out of the box with no custom claims transformation

### JWT Bearer in Services

All three services (`books-service`, `users-service`, `booking-service`) now validate JWT tokens from Keycloak.

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority = token issuer (browser-visible URL)
        options.Authority = config["Keycloak:Authority"];
        // MetadataAddress = where the server fetches JWKS from (Docker-internal URL)
        options.MetadataAddress = config["Keycloak:MetadataAddress"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer   = config["Keycloak:Authority"],
            RoleClaimType = "roles",
            NameClaimType = "preferred_username"
        };
    });
```

**Why two URLs?**
Keycloak tokens carry `iss = http://localhost:8888/realms/library` (the browser-reachable address).
Inside Docker, services can't reach `localhost:8888`. `MetadataAddress` points to the internal container address `http://keycloak:8080/...` so JWKS keys are fetched correctly, while `ValidIssuer` accepts the `localhost` issuer from the token.

### Role-Based Authorization

| Endpoint | Required role |
|---|---|
| `GET /api/v1/books/search` | any authenticated |
| `GET /api/v1/users/by-name/{name}` | any authenticated |
| `GET /api/v1/cart/{userId}` | `member` or `librarian` |
| `POST /api/v1/cart/items` | `member` |
| `DELETE /api/v1/cart/items/{bookId}` | `member` |
| `POST /api/v1/cart/complete` | `member` |
| `GET /api/v1/inventory/stock/*` | any authenticated |

`librarian` can view carts but not add/remove items — they manage the catalogue, not the borrowing queue.

### Frontend OIDC Login

`LibraryWeb` uses Cookie + OpenID Connect:

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(...)
    .AddOpenIdConnect(options =>
    {
        options.Authority       = kcAuthority;    // browser redirect target
        options.MetadataAddress = kcMetadata;     // server-side OIDC discovery
        options.ClientId        = "library-frontend";
        options.ResponseType    = OpenIdConnectResponseType.Code;  // PKCE
        options.SaveTokens      = true;           // stores access_token in cookie
        ...
    });
```

Two minimal routes are registered in `Program.cs`:
- `GET /login` — challenges the browser with an OIDC redirect to Keycloak
- `POST /logout` — signs out of cookie and Keycloak session

**Login flow:**
1. User clicks **Login with Keycloak** → browser redirected to `http://localhost:8888/realms/library/...`
2. User logs in with `user1` / `user1`
3. Keycloak redirects back with authorization code
4. Frontend exchanges code for tokens (PKCE)
5. Access token stored in encrypted cookie (`LibraryAuth`)
6. `preferred_username` claim (`user1`) used to look up the internal `userId` from `users-service` once; stored in session

A **role badge** is shown next to the username (`member` in blue, `librarian` in yellow).

### Token Propagation

Every HTTP call from the frontend to a microservice includes the access token:

```csharp
private async Task<HttpClient> CreateAuthenticatedClientAsync()
{
    var client = _httpClientFactory.CreateClient();
    var token = await HttpContext.GetTokenAsync("access_token");
    if (!string.IsNullOrEmpty(token))
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    return client;
}
```

This means:
- Services receive the full JWT on every request — they can inspect `sub`, `preferred_username`, and `roles`
- OpenTelemetry traces include the authenticated identity automatically via `AddAspNetCoreInstrumentation()`

### Makefile Targets

```bash
make keycloak-up      # start Keycloak container only (for local dev)
make keycloak-logs    # tail Keycloak logs
make keycloak-status  # health check + realm info
```

---

## How to Test Authorization

```bash
# Get a token for user1 (member)
TOKEN=$(curl -s -X POST http://localhost:8888/realms/library/protocol/openid-connect/token \
  -d "client_id=library-frontend&grant_type=password&username=user1&password=user1" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

# Call a protected endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:5002/api/v1/books/search?query=Book

# Try a librarian-only path with a member token (expect 403)
curl -H "Authorization: Bearer $TOKEN" -X POST http://localhost:5003/api/v1/cart/items \
  -H "Content-Type: application/json" \
  -d '{"userId":"u1","bookId":"b1","title":"Book1","author":"Author1"}'

# Get a librarian token and verify the role badge in the UI
TOKEN_LIB=$(curl -s -X POST http://localhost:8888/realms/library/protocol/openid-connect/token \
  -d "client_id=library-frontend&grant_type=password&username=librarian1&password=librarian1" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")
```

---

## Notes

- `direct-access-grants` (resource owner password credentials) is disabled on the `library-frontend` client in production-realistic config. The `curl` examples above use the implicit grant for testing convenience; enable it only in dev.
- Token expiry is 1 hour (Keycloak default). The frontend cookie session is 4 hours with sliding expiration — tokens may expire before the session does; add token refresh logic (via `options.UseTokenLifetime = true` or a background refresh) for production.
- `KC_HOSTNAME_STRICT: false` is suitable for development only. In production, set a single fixed hostname and enable HTTPS.


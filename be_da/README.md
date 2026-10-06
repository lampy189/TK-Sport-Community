# TKSC backend — Google sign-in

Implementation on branch `NhatLT`, based on the existing `main` backend `be_da` (.NET 8).
Open `be_da.slnx` with a recent Visual Studio / .NET 10 SDK, or open/build `be_da.csproj` with the .NET 8 SDK.
The frontend and the old local four-project scaffold are not changed by this implementation.

## Scope and sources

- UC-01, UC-02, UC-49 / API-02: Google sign-in and first-time registration.
- FR-01.3, BR-30, BR-47: new users receive Player; an existing account retains its one role.
- The user chose Google-only login on 6 October 2026. The earlier username/password SRS flow is not implemented here.
- No SMTP, Gmail password, email OTP, password-reset flow or Google mail permission is needed.
- Google OAuth credentials are still required to test with a real Google account. Tests use a fake Google backchannel and an in-memory account store; the opt-in SQL test only reads the real database.

## Code placement

| Folder | Responsibility |
| --- | --- |
| Controllers | HTTP endpoints and CSRF validation |
| Authorization | Secure cookie and fresh account/role checks |
| Application | Google registration/sign-in rules and store interface |
| Domain | Local account model |
| Infrastructure/Persistence | Parameterized SQL Server queries and atomic registration |

These are folders within the existing project. SQL access uses Microsoft.Data.SqlClient; no EF Core model or automatic migration is installed.

## Database

Local development uses `localhost`, database `Sep490`, Windows Authentication. Development connection settings contain no password. `TrustServerCertificate=True` is for the existing local development instance; production must use a valid server certificate and supply its connection string through environment configuration or a secret store.

On the user's workstation, upgrade `db/upgrades/001-google-subject.sql` was approved and applied on 6 October 2026. It adds nullable `users.google_subject VARCHAR(255)` using ordinal/case-sensitive collation and a unique index for non-null values. No existing columns or records were removed.

For another existing database, inspect the target and obtain approval before running:

```powershell
sqlcmd -S localhost -d Sep490 -E -C -b -i db/upgrades/001-google-subject.sql
```

`db/schema.sql` is the fresh-database baseline, not an upgrade script for existing tables. It includes the six roles; authentication requires the `Player` role. Do not run schema creation against an existing deployment as a substitute for an upgrade review.

On first real sign-in, the account, player profile and registration audit row are inserted in one transaction. Existing email + different/missing Google subject is rejected: linking an existing account requires a separate verified administrative flow. The application never assigns privileged roles from Google claims, typed email or client request data. Deleted/inactive accounts cannot sign in; role/status are rechecked on every authenticated request.

## Google Cloud setup

1. Sign into [Google Cloud Console](https://console.cloud.google.com/) yourself. An ordinary Google account can own the development project; do not put its password in TKSC configuration.
2. Create/select a project, for example `TKSC Development`.
3. Open **Google Auth Platform** (or **APIs & Services → OAuth consent screen**). Set an app name, support email and contact email. Choose **External** for normal Google accounts. While in Testing, add the Google accounts you will use as test users if the console requests this.
4. In **Clients**, choose **Create client → Web application**. Give the client a name such as `TKSC Local Backend`.
5. Add this exact **Authorized redirect URI** for the `https` launch profile:

   `https://localhost:7207/signin-google`

   The `/signin-google` endpoint is handled by ASP.NET authentication middleware. It is not a controller endpoint. The `IIS Express` profile uses a different port; use the `https` profile for these instructions.
6. Copy the **Client ID** and **Client Secret** into local User Secrets. In Visual Studio, right-click **be_da → Manage User Secrets**, then merge these keys into its JSON:

```json
{
  "Authentication:Google:ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
  "Authentication:Google:ClientSecret": "YOUR_CLIENT_SECRET"
}
```

The actual values belong only in the User Secrets editor, never `appsettings.json`, Git, screenshots or chat. Project `be_da.csproj` already contains a UserSecretsId. Save the file and restart the backend. Only profile/email scopes are requested. A client secret is an application credential, not a Gmail password.

Official references: [Microsoft Google login setup](https://learn.microsoft.com/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-8.0), [Google OpenID Connect](https://developers.google.com/identity/openid-connect/openid-connect).

## Run and endpoints

From the repository root:

```powershell
dotnet dev-certs https --trust
dotnet run --project be_da --launch-profile https
```

Swagger: `https://localhost:7207/swagger`.

| Endpoint | Use |
| --- | --- |
| GET `/api/auth/csrf` | Returns CSRF token and cookie; allowed before login |
| POST `/api/auth/google` | Starts Google authorization-code flow with PKCE and correlation/state protection |
| GET `/signin-google` | Google callback, validated by middleware |
| GET `/api/auth/me` | Current local user and role, or 401 |
| POST `/api/auth/logout` | Signs out this browser session; requires CSRF token |

Unsafe requests require `X-CSRF-TOKEN` or a form field `__RequestVerificationToken`. Fetch a new token after sign-in because it is bound to the current identity. All cookies require HTTPS. Session cookie is HttpOnly, Secure, SameSite=Lax, expires after eight hours without sliding renewal. Tokens are not returned in URLs or saved to localStorage. Authentication responses are not cacheable.

For the future FE, use a same-origin reverse proxy for `/api` and `/signin-google`. Start login with browser form navigation so the browser follows the redirect to Google; calling it using fetch/Swagger Execute will not navigate the page. Cross-origin FE hosting/CORS is intentionally not guessed; configure it with the final FE origin before integration.

For a manual backend-only test, open the local Swagger page and run this snippet in its browser developer console after reviewing it. It only fetches the local CSRF token and submits a form to the local backend; it is not frontend application code:

```javascript
const response = await fetch('/api/auth/csrf');
const { token } = await response.json();
const form = document.createElement('form');
form.method = 'POST';
form.action = '/api/auth/google';
const field = document.createElement('input');
field.type = 'hidden';
field.name = '__RequestVerificationToken';
field.value = token;
form.append(field);
document.body.append(form);
form.submit();
```

After consent, `/api/auth/me` returns the local account. First-time sign-in writes the new Player/profile/audit row; subsequent sign-in reuses the Google subject. To log out from the same origin:

```javascript
const { token } = await (await fetch('/api/auth/csrf')).json();
await fetch('/api/auth/logout', { method: 'POST', headers: { 'X-CSRF-TOKEN': token } });
```

Missing OAuth settings return 503 from login. Invalid/cancelled callbacks return a generic 400. Unauthenticated `/me` returns 401; rate-limited auth requests return 429. An existing account's email conflict is deliberately not disclosed to an unauthenticated caller.

## Checks

```powershell
dotnet build be_da/be_da.csproj
dotnet test tests/TKSC.Api.Tests
```

Without a test connection string, the read-only SQL check is explicitly skipped. To opt into that check against the configured local database:

```powershell
$env:TKSC_TEST_SQL_CONNECTION = 'Server=localhost;Database=Sep490;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/TKSC.Api.Tests
```

The test suite does not insert users into the real database. Concurrent SQL registration and live Google consent still need validation in a dedicated test database / configured OAuth environment before production. Deployments also need persistent protected Data Protection keys (shared between instances if scaled), an appropriate database service identity, and trusted proxy/HTTPS configuration matching the actual host.

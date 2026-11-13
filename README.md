# Qwek

A simple Discord-like real-time chat backend built with ASP.NET Core.

## Stack

- .NET 10 / ASP.NET Core Minimal APIs
- PostgreSQL + EF Core
- ASP.NET Identity + JWT
- SignalR

## Features

- Register / login with JWT
- User profiles: display name, username, avatar
- Servers, channels, and membership
- Real-time messaging over SignalR
- Replies, reactions, typing indicators, and presence
- Message history, edit, delete, and attachments

## Structure

```
Domains/         Entities and repository contracts
Applications/    Use cases, ports, and DTOs
Infrastructures/ EF Core, Identity, repositories, and services
Interfaces/      HTTP endpoints, SignalR hub, and web adapters
```

## Configuration

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Jwt:Issuer` | JWT issuer |
| `Jwt:Audience` | JWT audience |
| `Jwt:Key` | Signing key (32+ characters) |
| `Jwt:ExpiryMinutes` | Access token lifetime |
| `Storage:Provider` | `Local` (default) or `S3` |
| `Storage:Private` | Serve files through authenticated `/api/files` instead of public URLs |
| `Storage:PrivateBasePath` | URL prefix for private file routes |
| `Storage:MaxTotalBytes` | Hard cap for all stored uploads |
| `Storage:MaxAttachmentBytes` | Maximum attachment size |
| `Storage:MaxAvatarBytes` | Maximum avatar size |
| `Storage:Local:Path` | Local upload directory |
| `Storage:Local:PublicBasePath` | Public URL prefix for local files |
| `Storage:S3:*` | S3-compatible bucket settings (R2, B2, MinIO) |

Values can be set in `appsettings.json` or with `ConnectionStrings__DefaultConnection` / `Jwt__*` / `Storage__*` environment variables.

## File storage

Uploads use the local disk by default (`uploads/`, mounted as a Docker volume).
Before every upload the current usage is checked against `Storage:MaxTotalBytes`
and rejected with `507 Insufficient Storage` when the cap would be exceeded.
Local disk is scanned for actual usage, so external changes are respected.

Set `Storage:Private=true` for private buckets. Files are then served through
`GET /api/files/...` and require `Authorization: Bearer <token>`; attachment
downloads are only allowed for members of the server. Public `/uploads` serving
is disabled in private mode.

To use Cloudflare R2 or another S3-compatible bucket:

```bash
STORAGE_PROVIDER=S3
STORAGE_PRIVATE=true
STORAGE_S3_BUCKET=qwek-uploads
STORAGE_S3_ENDPOINT=https://<account_id>.r2.cloudflarestorage.com
STORAGE_S3_REGION=auto
STORAGE_S3_ACCESS_KEY_ID=<access-key>
STORAGE_S3_SECRET_ACCESS_KEY=<secret-key>
```

`STORAGE_S3_PUBLIC_BASE_URL` is only required when `STORAGE_PRIVATE=false`.

Use a dedicated bucket because the quota is calculated from the entire bucket.
Keep `Storage:MaxTotalBytes` slightly below your provider limit, for example
`9000000000` for R2's 10 GB free tier.

## Running

```bash
dotnet run
```

API listens on `http://localhost:5206`; OpenAPI is enabled in Development.

## API

| Area | Routes |
|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` |
| Users | `GET /api/users/me`, `PATCH /api/users/me/display-name`, `/username`, `/avatar`, `POST /avatar/upload` |
| Servers | `GET/POST /api/servers`, `GET /api/servers/{id}/members`, `GET /api/servers/{id}/invite`, `POST /api/servers/{id}/join`, `/leave`, `DELETE /api/servers/{id}` |
| Files | `GET /api/files/avatars/{name}`, `GET /api/files/attachments/{name}` |
| Channels | `GET/POST /api/servers/{id}/channels`, `PATCH/DELETE /channel/{id}` |
| Messages | `GET/POST /api/channels/{id}/messages`, `POST /upload`, `PATCH/DELETE /api/messages/{id}`, `POST /api/messages/{id}/reactions` |
| Hub | `/hubs/chat` |

All routes except auth require `Authorization: Bearer <token>`.

## Testing requests

`Qwek.http` contains a request flow using named requests; open it with the VS Code REST Client extension.

## Docker

```bash
docker build -t qwek:latest .
cp .env.example .env
docker compose up -d
```

The API is bound to `127.0.0.1:8080` by default. Set `BIND_ADDRESS` if your
reverse proxy runs on another interface or container.

## Production

Always put the API behind a reverse proxy that terminates TLS and never expose
port 8080 directly. Example `Caddyfile`:

```text
chat.example.com {
    reverse_proxy 127.0.0.1:8080
}
```

The app trusts loopback proxies for `X-Forwarded-For` and `X-Forwarded-Proto`.
If the proxy runs in another container, set its address with
`ForwardedHeaders__KnownProxies__0=<proxy-ip>`.

Auth endpoints are rate limited to 10 requests per minute per IP. Keep `.env`
private (`chmod 600`), use a database user with minimal privileges, and rotate
`JWT_KEY` immediately if it is ever exposed.

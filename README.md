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

Values can be set in `appsettings.json` or with `ConnectionStrings__DefaultConnection` / `Jwt__*` environment variables.

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

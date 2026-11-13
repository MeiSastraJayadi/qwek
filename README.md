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
| Servers | `GET/POST /api/servers`, `GET /api/servers/{id}/members`, `POST /api/servers/{id}/join`, `/leave`, `DELETE /api/servers/{id}` |
| Channels | `GET/POST /api/servers/{id}/channels`, `PATCH/DELETE /channel/{id}` |
| Messages | `GET/POST /api/channels/{id}/messages`, `POST /upload`, `PATCH/DELETE /api/messages/{id}`, `POST /api/messages/{id}/reactions` |
| Hub | `/hubs/chat` |

All routes except auth require `Authorization: Bearer <token>`.

## Testing requests

`Qwek.http` contains a request flow using named requests; open it with the VS Code REST Client extension.

## Docker

```bash
docker build -t qwek .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="<connection-string>" \
  -e Jwt__Issuer=Qwek \
  -e Jwt__Audience=Qwek \
  -e Jwt__Key="<32+-character-signing-key>" \
  qwek
```

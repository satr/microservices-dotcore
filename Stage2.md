# Stage 2: Persistent Microservices (PostgreSQL + EF Core)

## What Was Added

Stage 2 replaces in-memory data stores with PostgreSQL-backed repositories using
Entity Framework Core, while keeping in-memory as a fallback for local development
(no Docker required). Each service owns its own database — a core microservices
principle.

---

## Architecture Change

```
Before (Stage 1):                    After (Stage 2):
┌──────────────────┐                 ┌──────────────────┐
│ UsersService     │                 │ UsersService     │
│  InMemoryRepo    │                 │  PostgresRepo ───┼──▶ users_db
└──────────────────┘                 └──────────────────┘
┌──────────────────┐                 ┌──────────────────┐
│ BooksService     │                 │ BooksService     │
│  InMemoryRepo    │                 │  PostgresRepo ───┼──▶ books_db
└──────────────────┘                 └──────────────────┘
┌──────────────────┐                 ┌──────────────────┐
│ BookingService   │                 │ BookingService   │
│  InMemoryRepo    │                 │  PostgresRepo ───┼──▶ booking_db
└──────────────────┘                 └──────────────────┘
                                     All databases on one
                                     postgres container
                                     (per service in production)
```

---

## Files Added / Changed

### New files

| File | Purpose |
|---|---|
| `services/UsersService/Data/UsersDbContext.cs` | EF Core DbContext for users; includes seeded `user1`, `user2` |
| `services/UsersService/Data/UsersDbContextFactory.cs` | Design-time factory for `dotnet ef` tooling |
| `services/UsersService/Data/Migrations/` | EF Core `InitialCreate` migration |
| `services/UsersService/Repositories/PostgresUserRepository.cs` | PostgreSQL implementation of `IUserRepository` |
| `services/BooksService/Data/BooksDbContext.cs` | EF Core DbContext for books; includes seeded `Book1–3` |
| `services/BooksService/Data/BooksDbContextFactory.cs` | Design-time factory |
| `services/BooksService/Data/Migrations/` | EF Core `InitialCreate` migration |
| `services/BooksService/Repositories/PostgresBookRepository.cs` | PostgreSQL implementation of `IBookRepository` |
| `services/BookingService/Data/CartItemEntity.cs` | EF Core entity class (mutable, has PK) |
| `services/BookingService/Data/BookingDbContext.cs` | EF Core DbContext for cart items |
| `services/BookingService/Data/BookingDbContextFactory.cs` | Design-time factory |
| `services/BookingService/Data/Migrations/` | EF Core `InitialCreate` migration |
| `docker/postgres-init.sql` | Creates `users_db`, `books_db`, `booking_db` on first container start |

### Modified files

| File | Change |
|---|---|
| `services/UsersService/Models/UserRecord.cs` | Converted from `record` to `class` for EF Core compatibility |
| `services/BooksService/Models/BookRecord.cs` | Converted from `record` to `class` for EF Core compatibility |
| `services/UsersService/Program.cs` | Registers PostgresRepo when connection string is present; runs migrations at startup |
| `services/BooksService/Program.cs` | Same pattern |
| `services/BookingService/Program.cs` | Same pattern |
| `services/BookingService/Repositories/PostgresCartRepository.cs` | Replaced stub with full EF Core implementation |
| `docker-compose.yml` | Mounts init SQL, adds `ConnectionStrings__DefaultConnection` env var per service, adds `postgres` healthcheck dependency |

---

## Database-Per-Service Design

Each service has its own database on the shared PostgreSQL container:

| Service | Database | Tables |
|---|---|---|
| UsersService | `users_db` | `Users` |
| BooksService | `books_db` | `Books` |
| BookingService | `booking_db` | `CartItems` |

The databases are created by `docker/postgres-init.sql` on first container start.

---

## Repository Pattern

The `ICartRepository`, `IBookRepository`, and `IUserRepository` interfaces are
**unchanged**. The only difference is which implementation is injected:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<UsersDbContext>(opt => opt.UseNpgsql(connectionString));
    builder.Services.AddSingleton<IUserRepository, PostgresUserRepository>();   // ← NEW
}
else
{
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();   // ← fallback
}
```

This means:
- **No connection string** → in-memory (local dev, tests, CI without Docker)
- **Connection string present** → PostgreSQL (Docker, staging, production)

---

## Migrations

EF Core migrations are stored per service:

```
services/UsersService/Data/Migrations/
services/BooksService/Data/Migrations/
services/BookingService/Data/Migrations/
```

Migrations are applied **automatically at startup** via `db.Database.Migrate()`.
This is suitable for development and small deployments. For production, prefer
running migrations as a separate init step or Kubernetes Job.

### Add a new migration (per service)
```bash
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet ef migrations add <MigrationName> \
  --project services/UsersService/UsersService.csproj \
  --output-dir Data/Migrations

dotnet ef migrations add <MigrationName> \
  --project services/BooksService/BooksService.csproj \
  --output-dir Data/Migrations

dotnet ef migrations add <MigrationName> \
  --project services/BookingService/BookingService.csproj \
  --output-dir Data/Migrations
```

---

## Seeded Data

Seeding is defined in `OnModelCreating` using `HasData`:

**UsersDbContext** → `user1 (u1)`, `user2 (u2)`

**BooksDbContext** → `Book1/Author1 (b1)`, `Book2/Author2 (b2)`, `Book3/Author3 (b3)`

Seeds are applied as part of the `InitialCreate` migration.

---

## Local Development (no Docker)

Run any service without Docker — it falls back to in-memory automatically:

```bash
cd services/UsersService
dotnet run
# → InMemoryUserRepository activated (no connection string)
```

---

## Running with PostgreSQL

```bash
make up   # starts all containers including postgres
```

On first start:
1. `postgres-init.sql` creates `users_db`, `books_db`, `booking_db`
2. Each service runs `db.Database.Migrate()` and creates its tables
3. Seeded users and books are inserted via `HasData`

Subsequent restarts: data is **persisted** in the `postgres_data` Docker volume.

```bash
make clean   # stops containers AND deletes the postgres_data volume (resets all data)
```

---

## What's Still Missing for Production

| Concern | Status |
|---|---|
| Per-service PostgreSQL container | ⚠️ Shared instance (databases separated) |
| Connection string secrets | ⚠️ Plain env vars (use Vault or K8s secrets in prod) |
| Migration as CI/CD step | ⚠️ Auto-migrate at startup (use separate job in prod) |
| Database backups | ❌ Not configured |
| Read replicas / connection pooling (PgBouncer) | ❌ Not configured |
| Resiliency to DB unavailability at startup | ⚠️ Basic (relies on healthcheck ordering) |


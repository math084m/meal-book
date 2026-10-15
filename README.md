# Meal Book

Meal Book is a recipe-library starter with a .NET 10 API and an Angular frontend.
The recipe shape includes ingredients, ordered steps, servings, preparation and
cooking times, and equipment.

## Run locally

Requirements: .NET 10 SDK, Node.js 22, and npm.

In one terminal:

```powershell
dotnet run --project src/backend/MealBook.Api
```

In another:

```powershell
cd src/frontend/meal-book
npm start
```

Open `http://localhost:4200`. Angular proxies `/api` requests to the backend at
`http://localhost:5080`.

## Build

```powershell
dotnet build src/backend/MealBook.sln
cd src/frontend/meal-book
npm ci
npm run build
```

## Run with Docker Compose

From the repository root:

```powershell
docker compose up --build
```

Open `http://localhost:8080`. The frontend container proxies `/api` requests
to the backend service. The API is also available directly at
`http://localhost:5080`, and PostgreSQL at `localhost:5432`. Stop the containers
with `Ctrl+C`, or run `docker compose down`; the database volume is preserved.

## API

- `GET /api/health` checks that the service is running.
- `GET /api/recipes` lists recipes and supports search, ingredients, tags, meal
  type, course, cooking method, cuisine, time, calories, rating, and sort filters.
- `GET /api/recipes/{id}` reads one recipe.
- `GET /api/recipes/dashboard` provides recent additions, recently cooked, and
  most-cooked recipes.
- `POST /api/recipes` creates a recipe (Admin role required).
- `PUT` and `DELETE /api/recipes/{id}` update and delete recipes (Admin required).
- `POST /api/recipes/{id}/cooked` logs a cook and optional 1–5 rating (signed in).
- `POST /api/auth/register` creates a standard user; ASP.NET Core Identity
  endpoints at `/api/identity` provide login/token management.
- `GET /api/auth/me` returns the signed-in account.

Recipes, ingredients, ordered cooking steps, and equipment are stored in
PostgreSQL. SQL files in `src/backend/MealBook.Infrastructure/Migrations` are the schema
source of truth and are applied at API startup by Migr8. Add new migrations
with the next sequence number and branch name, for example `0003-master.sql`,
and write PostgreSQL DDL directly in the script. The EF Core model maps API
queries to this database-first schema; EF migrations are not used.

New registrations can browse recipes and maintain their own cooking history.
Recipe authoring is restricted to the seeded Admin role. To seed the first admin
locally, set credentials before starting Compose (password requires at least ten
characters, uppercase, lowercase, and a digit):

```powershell
$env:MEALBOOK_ADMIN_EMAIL = "admin@example.com"
$env:MEALBOOK_ADMIN_PASSWORD = "Use-a-local-secret-123"
docker compose up --build
```

Compose passes these values to startup seeding; it does not create a default
administrator when credentials are omitted. Set `Admin__Email` and
`Admin__Password` as Fly secrets for deployment:

```powershell
fly secrets set Admin__Email="admin@example.com" Admin__Password="Use-a-secret-123"
```

For local development with the .NET API running outside Docker, start only the
database with `docker compose up -d database`, then run the backend as above.
The development connection string is in `appsettings.Development.json`.
Compose keeps database files in the `mealbook-postgres` named volume;
`docker compose down -v` removes that volume and permanently deletes local data.

## Fly.io and a `/meals/` subroute

The frontend and API have separate Dockerfiles and Fly app configs. Update
`app` and `primary_region` in each `fly.toml` to match your Fly.io apps and
region. Set `ConnectionStrings__MealBook` as a Fly secret to your managed
PostgreSQL connection string, or attach a Fly Postgres database that provides a
`DATABASE_URL` URI. The API reads either setting and applies Migr8 SQL migrations at startup.
Deploy each service from its project directory:

```powershell
cd src/backend
fly deploy --config fly.toml
cd ../../frontend/meal-book
fly deploy --config fly.toml
```

The frontend image builds with `/meals/` as its Angular base URL. Configure
your website's reverse proxy to strip `/meals` before forwarding frontend
requests to the frontend Fly app, and forward `/meals/api/*` to the API Fly app
with the `/meals` prefix removed. This keeps browser requests same-origin and
supports Angular route refreshes. For a different mount path, update
`APP_BASE_HREF` in `src/frontend/meal-book/fly.toml`.

Use managed PostgreSQL or another persistent database for production recipes.

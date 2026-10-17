# Meal Book

Meal Book is a recipe-library starter with a .NET 10 API and an Angular frontend.
The recipe shape includes ingredients, ordered steps, servings, preparation and
cooking times, and equipment.

## Run locally

Requirements: .NET 10 SDK, Node.js 24 LTS (22.22+ also works), and npm 11.

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
- `GET /api/recipes` lists recipes and supports search, ingredients (all of),
  tags, meal types, courses, cooking methods, cuisines (repeat a key for several
  values), `minTotalMinutes`/`maxTotalMinutes`, calories, rating, and sort filters.
- `GET /api/recipes/suggestions?q=` returns up to 8 name suggestions.
- `GET /api/terms` and `GET /api/ingredients` list the premade filter terms and
  the ingredient catalog; admins manage ingredients on the **Ingredients** page.
- `GET /api/recipes/{id}` reads one recipe.
- `GET /api/recipes/dashboard` provides recent additions, recently cooked, and
  most-cooked recipes.
- `POST /api/recipes` creates a recipe (Admin role required).
- `PUT` and `DELETE /api/recipes/{id}` update and delete recipes (Admin required).
- `GET /api/recipes/admin/drafts` lists unpublished recipes (Admin required).
- `POST /api/recipe-imports` imports a structured recipe as an unpublished draft
  using the server-to-server `X-Recipe-Import-Key` header.
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

## Meal planner and shopping list

Signed-in users share one household plan (`/plan`). Add recipes with the "Add to plan" button; the shopping list is aggregated across recipes (scaled by planned servings, volumes summed in ml, weights in g) and grouped by shop section (`ShopSections` table, assigned per ingredient in the admin ingredients page). Checked items are shared too.

- Realtime: every change calls Postgres `NOTIFY meal_plan_changed`; each API instance `LISTEN`s and pushes `planChanged` over SignalR (`/api/hubs/mealplan`), and clients refetch. This needs a direct (non-pooled) Postgres connection for LISTEN, and the proxy should allow WebSockets (SignalR falls back to SSE/long polling otherwise).
- Suggestions: recipes sharing perishable ingredients with the plan score highest, rarer ingredients weigh more, and extra perishables are penalised. "Perishable" comes from the section (`ShopSections.Perishable`), so e.g. onions count as fresh.
- Week view (`/plan/week`): pick a date range (up to 42 days, remembered per browser) and drag plan recipes onto days; on touch screens tap a recipe, then "Place here". A recipe can span 1-14 days (leftovers) and is scheduled with `PUT api/meal-plan/recipes/{id}/schedule` (`{ start: "yyyy-MM-dd" | null, days }`). Each recipe is in the plan once, so it has a single scheduled slot.

## Cook mode and favourites

- **Cook mode** (`/recipes/:id/cook`, "Start cooking" on the recipe page): one big step at a time, previous/next buttons, arrow keys and swipe, an ingredients panel scaled to the chosen servings, and an optional timer when a step mentions a duration ("10 minutes", "2 timer"). The screen is kept awake via the Screen Wake Lock API where supported.
- **Favourites** are personal (per user): a star on recipe cards and the detail page, plus a "Favourites only" filter on the recipe list. API: `GET api/favorites`, `PUT|DELETE api/favorites/{recipeId}` (migration `0025`).

## Fly.io and a `/meals/` subroute

The frontend and API have separate Dockerfiles and Fly app configs. Update
`app` and `primary_region` in each `fly.toml` to match your Fly.io apps and
region. Set `ConnectionStrings__MealBook` as a Fly secret to your managed
PostgreSQL connection string, or attach a Fly Postgres database that provides a
`DATABASE_URL` URI. The API reads either setting and applies Migr8 SQL migrations at startup.

## CI and pull requests

`.github/workflows/ci.yml` builds and tests the backend (against a Postgres service) and the
frontend (`ng build` + Karma in ChromeHeadless). It runs on every pull request to `main` and is
reused by `deploy.yml`, so a push to `main` deploys only after the same checks pass.

`main` is protected: changes go through a feature branch and a pull request that needs one
approving review and green **Backend build and tests** and **Frontend build and tests** checks.

## Backups and monitoring

Production Postgres is the single-node Fly app `meal-book-db`. Two layers protect it:

1. **Fly volume snapshots** (automatic, daily). Raise the retention from the default 5 days:
   `fly volumes list -a meal-book-db`, then `fly volumes update <volume-id> --snapshot-retention 14 -a meal-book-db`.
2. **Off-Fly logical dump** (`.github/workflows/db-backup.yml`, daily 02:17 UTC, or run it from the
   Actions tab). It opens a `fly proxy` to `meal-book-db`, runs `pg_dump`, restores the dump into a
   throwaway Postgres to prove it works, encrypts it with GPG (AES-256) and uploads it as the
   `mealbook-db-backup` artifact, kept for 30 days. A failed run emails the repository owner.

Set up the three repository secrets once (Settings, Secrets and variables, Actions):

```powershell
# Token that can reach the database app; the deploy token is scoped to meal-book only.
fly tokens create deploy -a meal-book-db   # -> FLY_BACKUP_TOKEN
# The production connection string, as the API sees it -> BACKUP_DATABASE_URL
fly ssh console -a meal-book -C "printenv DATABASE_URL"
# Any long random passphrase -> BACKUP_PASSPHRASE. Store a copy in your password manager:
# without it the backups cannot be decrypted.
```

Restore (into any Postgres 16+ database):

```powershell
gpg --decrypt mealbook-YYYYMMDD-HHMMSS.dump.gpg > mealbook.dump
pg_restore --no-owner --no-privileges --clean --if-exists -d "<target connection string>" mealbook.dump
```

Cover images live in Tigris, not in the database, so they are not part of the dump. The database
and bucket are separate failure domains; keep the dump and bucket in step when restoring.

`.github/workflows/uptime.yml` calls `/meal-book/api/health` every 30 minutes and fails (and emails)
when the site is down. Fly also health-checks `/api/health` itself and restarts unhealthy machines.

## Languages (English / Danish)

The UI is available in English and Danish (switcher in the top bar; defaults to
the browser language). Recipes have a source `language` (`en` or `da`). When a
recipe is published (created published, saved while published, or published from
drafts) it is machine-translated to the other language with
[DeepL](https://www.deepl.com/pro-api) and stored; readers see it in their UI
language with a "machine translated" badge. Meal type, course, cooking method
and cuisine are not translated (they are filter values), and search runs on the
original text. Saving an unchanged recipe does not call DeepL again. Existing
recipes are translated the next time they are saved or republished.

Without a key nothing is translated and everything else works. Set the DeepL
API key (a `:fx` key uses the free endpoint) as a Fly secret, and locally via
`MEALBOOK_DEEPL_API_KEY` for Docker Compose or `DeepL__ApiKey` for `dotnet run`:

```powershell
fly secrets set DeepL__ApiKey="your-key" -a meal-book
```

## Recipe cover images

Admins upload one cover image per recipe on the recipe form. The browser resizes
it to at most 1600 px JPEG; the API accepts JPEG, PNG or WebP up to 3 MB and
stores it in an S3-compatible bucket (Fly Tigris). Endpoints:
`POST|GET|DELETE /api/recipes/{id}/image` (upload and delete need Admin; drafts'
images are only served to admins).

Without bucket settings uploads return 503 and everything else works. On Fly:

```powershell
fly storage create -a meal-book
```

This sets `BUCKET_NAME`, `AWS_ENDPOINT_URL_S3`, `AWS_ACCESS_KEY_ID` and
`AWS_SECRET_ACCESS_KEY` as secrets. For local development set the same
variables (any S3-compatible store, e.g. MinIO) as environment variables.
Alternatively set `ImageStorage__LocalPath` to store images on disk (docker
compose uses the `mealbook-images` volume).

## Scraping protection

Recipes are public, so this is deterrence, not a vault:

- Per-visitor rate limits (sliding windows, minute and day) for the API, images
  and sign-in/registration; over the limit returns `429` with `Retry-After`.
  `/api/health` is exempt. Tune with env vars such as
  `Protection__Api__PerMinute`, `Protection__Api__PerDay`,
  `Protection__Images__PerMinute`, `Protection__Auth__PerMinute`; switch off with
  `Protection__RateLimitingEnabled=false`.
- Requests with an empty or tool-like User-Agent (curl, python-requests, bots,
  headless browsers) get `403` (`Protection__BlockAutomatedClients=false` to
  disable). This only stops lazy bots; the recipe import endpoint and health check
  are exempt.
- The recipe list and dashboard return summaries only (counts, no ingredients or
  steps), so a full dump needs one rate-limited request per recipe.
- `X-Robots-Tag`, a robots meta tag and `robots.txt` ask crawlers and AI bots to
  stay away. Crawlers read the root `robots.txt` of the main site, so add
  `Disallow: /meal-book/` there too.
- The client address comes from `X-Forwarded-For` (configurable with
  `Protection__ClientIpHeader`, e.g. `Fly-Client-IP`), so the proxy in front must
  forward it.

## AI-assisted recipe imports

Playwright is free browser automation, not an AI recipe extractor. Keep browser
fetching outside the API: an AI agent can read a recipe URL, map its content to
the recipe JSON shape, then submit it to `POST /api/recipe-imports`. The endpoint
stores the source URL and always creates an unpublished draft; it never fetches
arbitrary URLs itself. This avoids exposing the API to server-side request
forgery from user-controlled links.

Configure a private import key of at least 32 characters. Never put it in the
Angular app or commit it:

```powershell
$env:MEALBOOK_RECIPE_IMPORT_API_KEY = "replace-with-a-long-random-secret-value"
docker compose up -d --force-recreate backend
```

For production, set the same secret in the backend's Fly app:

```powershell
fly secrets set RecipeImport__ApiKey="replace-with-a-long-random-secret-value"
```

Send `POST /api/recipe-imports` with `Content-Type: application/json` and
`X-Recipe-Import-Key`. Payload uses recipe write fields (`name`, `description`,
`servings`, `prepTimeMinutes`, `cookTimeMinutes`, `mealTypes`, `courses`,
`cookingMethods`, `cuisines`, nutrition fields, `tags`, `ingredients`, `steps`,
`equipment`) plus `sourceUrl` and `language` (`en`/`da`, default `en`). Term
fields are arrays of slugs from `GET /api/terms`; ingredients reference catalog
ids from `GET /api/ingredients`. Example:

```json
{
  "name": "Example recipe",
  "description": "Adapted from the linked source.",
  "sourceUrl": "https://example.com/recipe",
  "servings": 4,
  "prepTimeMinutes": 15,
  "cookTimeMinutes": 30,
  "mealTypes": ["dinner"],
  "courses": ["main"],
  "cookingMethods": ["baking"],
  "cuisines": ["italian"],
  "tags": ["easy"],
  "ingredients": [
    { "ingredientId": "<id from /api/ingredients>", "quantity": 2, "unit": "piece", "section": "Ingredients" }
  ],
  "steps": ["Prepare ingredients.", "Cook until done."],
  "equipment": ["Baking dish"]
}
```

Admin users review drafts under **Review drafts**. Edit the imported details,
then check **Publish this recipe to public library** before saving. Drafts are
hidden from public search, recipe details, home dashboard, and cooking logs
until published. Existing recipes remain published after the migration.
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

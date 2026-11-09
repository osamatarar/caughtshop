# Caught

A link-in-bio for TikTok Shop affiliates. The creator says “it’s #47 in my bio.” The viewer types `47`, hits a buy button in seconds, and the click is recorded even when TikTok Shop never paid.

Tagline: Nothing slips.

Sandbox only. No TikTok API, no SMS, no email, no payments, no affiliate network.

## Click through

After the run steps below, this is the loop the brief grades.

1. Open `http://localhost:4200/` and sign up. Claim a handle (for example `remy`).
2. Seed the catalog (see [Run](#run)). It attaches to the first creator row.
3. Open `http://localhost:4200/remy` on a phone width.
   - Type `4` and submit. `#4` is sold out. The page shows `#8` with `#4 is sold out. #8 is the one to get.`
   - Type `stanley` — one hit, product page. Type `lip` — two hits, stay on the bio. Type `47` — nothing, miss stored.
   - Tap a video tile. It opens that product, not TikTok.
   - Leave a phone or email on `#4`. That waitlist is demand for the restock, not the backup.
4. Log in. Dashboard shows clicks by button, unsuccessful searches, and waitlist per product. Products and videos are under `/dashboard/products` and `/dashboard/videos`. Socials are `/dashboard/profile`.

Public bio needs no login. The studio does.

## Run

Needs Postgres, .NET 10, and Node 22+. From the repo root.

Create an empty database named `caught`, then point both `psql` and the API at it. The API accepts a `postgresql://` URL.

```bash
export CONN="postgresql://postgres:postgres@localhost:5432/caught"
cd BackEnd/Caught
dotnet user-secrets set ConnectionStrings:Caught "$CONN"
cd ../..
```

```powershell
$env:CONN = "postgresql://postgres:postgres@localhost:5432/caught"
Set-Location BackEnd/Caught
dotnet user-secrets set ConnectionStrings:Caught $env:CONN
Set-Location ../..
```

Apply schema and functions in filename order. They are safe to re-run.

```bash
for f in Database/Schema/*.sql Database/Functions/*.sql; do
  psql "$CONN" -f "$f"
done
```

```powershell
Get-ChildItem Database/Schema/*.sql, Database/Functions/*.sql |
  Sort-Object Name |
  ForEach-Object { psql $env:CONN -f $_.FullName }
```

Start both processes.

```bash
cd BackEnd/Caught
dotnet run
```

```bash
cd FrontEnd/Caught
npm install
npm start
```

| Process | URL |
| --- | --- |
| API | `http://caught.somee.com/` |
| Web | `http://localhost:4200/` |

Sign up, then load the eight-product catalog. The script wipes products, videos, clicks, and waitlist for that creator. It does not delete the account.

```bash
psql "$CONN" -f Database/Scripts/seed_dummy_products_videos.sql
```

```powershell
psql $env:CONN -f Database/Scripts/seed_dummy_products_videos.sql
```

Seed contents: eight products, eight videos, TikTok + Instagram on the bio, `#4` Laneige sold out with `#8` Rhode as backup.

Clicks sit in a short in-memory buffer and flush at least every two seconds. Wait a beat before the dashboard if you just tapped a button.

```bash
cd FrontEnd/Caught && npm test -- --watch=false
cd BackEnd/Caught && dotnet build
```

## Stack

| Layer | Choice | Why |
| --- | --- | --- |
| Web | Angular 22, standalone components, Tailwind | Mobile-first public routes. Typed forms in the studio. |
| API | ASP.NET Core 10, Dapper, Npgsql | Thin HTTP over SQL functions. No EF model to fight. |
| Data | Postgres | The catalog, backups, and reports are relational. Scripts are idempotent and re-runnable. |
| Auth | PBKDF2-SHA256, creator profile in `localStorage` | Viewers never get an account. Cookie Identity is the next step out of the sandbox, not the core loop. |
| Images | HTTPS URLs | No blob store. Seed uses public images. |

SQL lives in `Database/`. The API does not migrate on startup.

## Calls I made

The brief left these open. These are the rules the code follows.

**Find.** One field. `#47`, `047`, and `47` are the same number. A number hit, or a unique text hit, opens the product immediately. Several text hits stay on the bio. Zero hits record the query and say `Nothing for that.` One-character text does nothing so the miss list is not noise.

**Video grid.** The tile is the product. The viewer already came from TikTok. Sending them back is a leaked sale.

**Sold-out.** One hop. Out of stock requires an in-stock backup on the same storefront. A product that is already someone’s backup cannot be marked sold out until that link is moved. `/{handle}/p/4` still exists so the spoken number does not lie. The page shows the backup: `#4 is sold out. #8 is the one to get.` Waitlist stays on `#4`. Buy clicks record on `#8`. If the backup is missing or also sold out, no buy buttons, no chain walk.

**Buy buttons.** TikTok Shop is the filled button. Amazon and the brand site sit under it when those URLs exist. Same tab: `fetch` with `keepalive`, then `location.assign`. TikTok’s in-app browser drops `target=_blank` often enough that a new tab is a failed buy.

**Socials.** Optional TikTok, Instagram, YouTube URLs. Empty stays off the bio. Edited after signup on `/dashboard/profile`, so handle claim stays short.

**Waitlist.** Phone or email against one product. Nothing is sent. A repeat contact returns the existing row. Dashboard “Waiting” is that count.

**Clicks.** 10-second buckets per product and channel (`tiktok`, `amazon`, `brand`, `product`, `notification`). No viewer identity. No dedupe. Every press counts.

**Numbers.** Assigned per creator as `MAX(public_number)+1`. Shown as `#n` in the studio. Delete exists (videos first). The number is not reused.

**Look.** Paper `#F4F1E8`, ink `#141413`, green `#157A48` for money. Numbers and prices are monospace. Voice is plain: `Caught: $4.80 from #47`.

**Dashboard extras.** Date ranges, clicks by day, ranking, miss list, waitlist per product. That is what a creator uses to decide what to film and what to link next. No fake GMV. A click is not a sale.

**One storefront per login.** The brief’s multi-account screen is later. Combined totals would lie until then.

## Routes

Reserved handles: `login`, `signup`, `studio`, `dashboard`, `api`, `assets`, `health`, `admin`.

| Path | Who | |
| --- | --- | --- |
| `/{handle}` | Viewer | Bio: search, products, videos, socials |
| `/{handle}/p/{number}` | Viewer | Product. Shareable. Direct hit is valid. |
| `/signup`, `/login` | Creator | Claim a handle, or enter the studio |
| `/dashboard` | Creator | Clicks, misses, waitlist |
| `/dashboard/products` | Creator | Catalog, sold-out backup |
| `/dashboard/videos` | Creator | TikTok URL, thumbnail, caption, product |
| `/dashboard/profile` | Creator | Social URLs |

## What I would build next

In order, if this left the sandbox:

1. HttpOnly cookie auth. `localStorage` plus `creatorId` on writes is enough here and wrong in production.
2. Shareable receipt card for a caught click. The click and the number already exist.
3. Arrival on each click (`number`, `search`, `video`, `direct`) so the dashboard can say which spoken number actually works.
4. Image upload. URL paste is the sandbox compromise.
5. Real SMS/email for the waitlist, still mocked until a provider is paid.
6. Second storefront under one login, then combined totals.

Not in that list: a TikTok API, live affiliate attribution, or hosting. The brief said not to.

## Layout

```
Database/Schema      001–002
Database/Functions   003–010
Database/Scripts     seed
BackEnd/Caught       ASP.NET API
FrontEnd/Caught      Angular app
```

# Koperasi Store — Village Cooperative Management System

A full-stack app for running a village cooperative (koperasi) store: inventory,
Cashier Mode POS, member enrollment & SHU profit-sharing, discounts, staff
management, and financial reporting — with four role tiers (Admin, Shop
Owner, Employee, Member) enforced at both the API and the frontend.

**Stack:** MySQL · ASP.NET Core 8 (REST API, JWT auth) · Next.js 14
(App Router, TypeScript, Tailwind) · Docker Compose · Nginx.

---

## What's been verified

This stack has been booted end-to-end with Docker Compose and exercised
through the browser:

- `docker compose up -d --build` succeeds from a clean checkout (plus the
  `cp .env.example` steps below). On the first boot of an empty database
  volume, MySQL auto-creates and seeds the demo data from
  `mysql/init/01_koperasi_schema_seed.sql`.
- `scripts/e2e_test.py` runs a 96-check end-to-end suite (auth, items,
  categories, members, checkout incl. a concurrency race, loans, restocks,
  reports vs raw SQL, frontend contract shapes) against the live stack from
  the host: `python3 scripts/e2e_test.py`.
- Staff/member login, Cashier Mode checkout + thermal receipt printing, and
  the admin/owner/member screens have all been walked through against the
  running API. `next build` compiles all frontend routes clean.

Still treat the security-sensitive spots with care (see [Known gaps &
next steps](#known-gaps--next-steps)).

---

## Quick Start (local development)

**Requirements:** Docker & Docker Compose. That's it — MySQL and Node
all run inside containers.

```bash
git clone <this-repo> koperasi-app && cd koperasi-app

# 1. Walk the setup wizard — it asks for the database / backend / frontend
#    connection secrets (blank answers get strong generated values) and
#    writes .env + frontend/.env.
bash scripts/setup.sh

# 2. Build and start everything — the database is created and seeded
#    automatically on the first boot (empty volume).
docker compose up -d --build
```

Then open **http://localhost**. Demo accounts (see
`mysql/init/01_koperasi_schema_seed.sql`):

| Role       | Login                  | Password   |
|------------|-------------------------|------------|
| Admin      | admin@koperasi.test      | password   |
| Shop Owner | owner@koperasi.test      | password   |
| Employee   | kasir1@koperasi.test     | password   |
| Member     | phone `081399990001` (first seeded member) | password |

Notes:

- Reset an existing database back to the demo state any time:
  `docker compose up -d mysql && bash scripts/db-reset.sh`.
- Opening the app from another machine on the LAN? The frontend calls the
  API same-origin (`NEXT_PUBLIC_API_BASE_URL=/api` through Nginx), so
  `http://<server-ip>` just works — no per-client URL to configure.
- Re-running the wizard later regenerates secrets into the same `.env`
  (the old one is kept as `.env.bak.<stamp>`). MySQL only applies
  credentials on the first boot of an empty volume — after changing
  DB_PASSWORD on an initialized DB, wipe + reseed with the reset script.

---

## Project Structure

```
koperasi-app/
├── docker-compose.yml         # local dev stack
├── docker-compose.prod.yml    # production stack (SSL-ready Nginx)
├── nginx/                     # reverse proxy configs for both
├── mysql/init/                # schema + demo seed applied on first boot
├── scripts/
│   ├── e2e_test.py            # 96-check end-to-end suite (python3)
│   └── db-reset.sh            # reset a used DB to the demo seed state
├── backend-dotnet/            # ASP.NET Core 8 API (all routes, Swagger at /swagger)
│   ├── Controllers/           # grouped by role: Staff/, Member/, owner routes
│   ├── Services/              # CheckoutTransactionService, DiscountService, ...
│   ├── Models/                # EF Core entities (snake_case mapped to MySQL)
│   └── Data/                  # KoperasiDbContext
└── frontend/                  # Next.js 14 App Router
    ├── middleware.ts          # edge-level route guard for all 4 roles
    ├── lib/                   # api client, auth/session, shared types
    ├── components/            # AppShell, DataTable, StatCard
    └── app/
        ├── login/
        ├── admin/      (dashboard, users, discounts, settings, audit-logs, backups)
        ├── owner/      (dashboard, items, categories, suppliers, employees, members, transactions, reports)
        ├── employee/   (dashboard, cashier ← POS, restock, my-history)
        └── member/     (dashboard, history, discounts, shu, profile)
```

## How the role hierarchy works

The brief specifies "Admin has all Shop Owner and Employee permissions."
Rather than duplicating permission checks, this is modeled as a strict rank:

```
admin (0)  ≤  shop_owner (1)  ≤  employee (2)     — lower rank = more access
```

- **Backend:** every endpoint carries an ASP.NET `[Authorize(Roles = ...)]`
  attribute listing the exact roles that may call it — e.g. `admin,owner,
  manager,shop_owner,employee` lets the whole staff chain through, while
  owner-only routes list just their roles.
- **Frontend:** `middleware.ts` mirrors the same rank table at the edge,
  before any page renders, so an Employee account is redirected away from
  `/owner/*` or `/admin/*` even if they guess the URL.
- **Members** are a completely separate table, guard, token type, and route
  namespace (`/api/member/*`, `/member/*` pages) — there is no rank
  relationship between "member" and the staff tiers at all, matching the
  brief's "fully isolated" requirement.

## Runtime-editable settings (no redeploy required)

Business rules and most infrastructure fields — tax rate, SHU rate, low-stock
threshold, backup schedule, store name/address, CORS/API base URL — live in
a `settings` database table, not `.env` constants, and are edited from
**Admin → Settings**. Changes take effect on the next request, no restart
needed. See `backend-dotnet/Services/SettingsService.cs` for the one
caveat: the *bootstrap* DB connection itself (which host/port
the container connects to on boot) still needs a container restart to
change, for the unavoidable reason that you can't re-point a live PDO
connection mid-request — the admin UI still lets you record the value,
but a real host migration remains an ops action, not a click.

## Cashier Mode (POS)

`/employee/cashier` — the daily-driver screen: scan a barcode or type a SKU
(works with plain USB keyboard-wedge scanners, since they just type text +
Enter), or search by name; build a cart; look up a member by phone or
membership ID; the backend auto-applies whichever active discount saves the
customer the most (or the cashier can pick one explicitly); choose a
payment method; charge. Receipt is a printable on-screen view
(`window.print()`) styled like a thermal ticket.

## Backups

Admin → Backup & Restore calls `POST /api/backup`, which runs
`mysqldump | gzip` inside the .NET backend container and writes to the
`backend-dotnet-storage` volume (`/app/backups`). Last backup status is
`GET /api/backup/last` and restore is `POST /api/backup/restore` with a
file upload. The backups dir is also served read-only under `/storage/` so
owners can download the archive from the browser. There is no in-container
scheduler yet — automate via host cron calling the API if you need
scheduled dumps.

## Production deployment

```bash
cp .env.production.example .env.production   # fill in DB creds + your domain
# Drop TLS certs into nginx/certs/ (fullchain.pem, privkey.pem — see
# nginx/certs/README.md) and set server_name in nginx/prod.conf.
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
```

Optionally seed demo data on a real store with:
`docker compose -f docker-compose.prod.yml exec -T mysql sh -c "exec mysql -uroot -p<root_pw> koperasi" < mysql/init/01_koperasi_schema_seed.sql`

The prod compose file builds without bind-mounting source into the
containers, runs `next build` at image-build time, and doesn't expose
MySQL's port to the host.

---

## Known gaps & next steps

Being direct about what's a solid foundation vs. what still needs work
before this is genuinely production-ready:

- **Camera-based barcode scanning isn't wired up.** Cashier Mode's scan
  field works great with a USB/Bluetooth barcode scanner (they emulate a
  keyboard), but there's no camera/webcam scanning library integrated yet —
  that'd mean adding something like `@zxing/browser` to the scan input.
- **Receipts are browser-print only**, not real ESC/POS thermal-printer
  byte output. Fine for a USB/network thermal printer that has its own
  print-driver, not fine for raw serial ESC/POS control.
- **E2E coverage via `scripts/e2e_test.py` (96 checks) exercises the API
  against the live stack, including a concurrent-checkout race — run it
  against a scratch database if you want it fully side-effect free; add
  focused unit tests around `TransactionService` and discount math for CI.
- **Excel/PDF report exports are not implemented** in the .NET backend yet
  (reports are API/JSON only for now).
- **`npm audit` will flag Next.js/PostCSS advisories.** `next` is pinned to
  14.2.35 (patched against the Dec 2025 RSC DoS CVEs), but the advisory
  database's ranges are broad — run `npm audit` yourself and update before
  shipping to production, rather than treating this pin as a permanent fix.

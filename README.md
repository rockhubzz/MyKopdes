# Koperasi Store — Village Cooperative Management System

A full-stack app for running a village cooperative (koperasi) store: inventory,
Cashier Mode POS, member enrollment & SHU profit-sharing, discounts, staff
management, and financial reporting — with four role tiers (Admin, Shop
Owner, Employee, Member) enforced at both the API and the frontend.

**Stack:** MySQL · Laravel 12 (REST API, Sanctum token auth) · Next.js 14
(App Router, TypeScript, Tailwind) · Docker Compose · Nginx.

---

## What's been verified

This stack has been booted end-to-end with Docker Compose and exercised
through the browser:

- `docker compose up -d --build` succeeds from a clean checkout (plus the
  three `cp .env.example` steps below). Backend dependencies install from the
  committed `backend/composer.lock`, so builds are reproducible.
- First-run (`scripts/first-run.sh`) migrates + seeds cleanly; the demo
  accounts in the table below log in, and staff/member login, Cashier Mode
  checkout + thermal receipt printing, and the admin/owner/member screens have
  all been walked through against the running API.
- `next build` compiles all frontend routes clean.

Still treat the security-sensitive spots with care (see [Known gaps &
next steps](#known-gaps--next-steps)).

---

## Quick Start (local development)

**Requirements:** Docker & Docker Compose. That's it — MySQL, PHP, and Node
all run inside containers.

```bash
git clone <this-repo> koperasi-app && cd koperasi-app

# 1. Root env (Docker Compose variables)
cp .env.example .env

# 2. Backend env
cp backend/.env.example backend/.env

# 3. Frontend env
cp frontend/.env.example frontend/.env

# 4. Build and start everything
docker compose up -d --build

# 5. First-run setup: generate app key, run migrations, seed demo data
docker compose cp scripts/first-run.sh backend:/tmp/first-run.sh
docker compose exec backend bash /tmp/first-run.sh
```

Then open **http://localhost**. Demo accounts (see
`backend/database/seeders/DatabaseSeeder.php`):

| Role       | Login                  | Password   |
|------------|-------------------------|------------|
| Admin      | admin@koperasi.test      | password   |
| Shop Owner | owner@koperasi.test      | password   |
| Employee   | kasir1@koperasi.test     | password   |
| Member     | (seeded phone number — printed by the seeder / shown in the DB `members` table) | password |

Notes:

- Backend dependencies are pinned in `backend/composer.lock`; both the image
  build and the container entrypoint install from it, so redeploys resolve
  identical versions. Never commit `vendor/` or `.env` files — they're
  gitignored (see `.gitignore`).
- Opening the app from another machine on the LAN? The frontend calls the
  API same-origin (`NEXT_PUBLIC_API_BASE_URL=/api` through Nginx), so
  `http://<server-ip>` just works — no per-client URL to configure.

---

## Project Structure

```
koperasi-app/
├── docker-compose.yml         # local dev stack
├── docker-compose.prod.yml    # production stack (SSL-ready Nginx)
├── nginx/                     # reverse proxy configs for both
├── scripts/first-run.sh       # migrate + seed + storage:link
├── backend/                   # Laravel 11 API
│   ├── app/Models/            # User, Member, Item, Transaction, ...
│   ├── app/Http/Controllers/Api/
│   ├── app/Http/Middleware/   # EnsureRole, LogActivity
│   ├── app/Services/          # SettingsService, DiscountService, ReportService
│   ├── app/Console/Commands/  # backup, restore, alert sweep
│   ├── database/migrations/
│   ├── database/seeders/DatabaseSeeder.php
│   └── routes/api.php         # every endpoint, grouped by minimum role
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

- **Backend:** `App\Http\Middleware\EnsureRole` (aliased `role`) takes a
  single "minimum role" per route group — e.g. `role:employee` lets admin,
  shop_owner, *and* employee through; `role:admin` lets only admin through.
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
needed. See the long comment in `backend/app/Services/SettingsService.php`
for the one caveat: the *bootstrap* DB connection itself (which host/port
the container connects to on boot) still needs a container restart to
change, for the unavoidable reason that you can't re-point a live PDO
connection mid-request — the admin UI still lets you record/document that
value, but a real host migration remains an ops action, not a click.

## Cashier Mode (POS)

`/employee/cashier` — the daily-driver screen: scan a barcode or type a SKU
(works with plain USB keyboard-wedge scanners, since they just type text +
Enter), or search by name; build a cart; look up a member by phone or
membership ID; the backend auto-applies whichever active discount saves the
customer the most (or the cashier can pick one explicitly); choose a
payment method; charge. Receipt is a printable on-screen view
(`window.print()`) styled like a thermal ticket.

## Backups

Admin → Backup & Restore triggers `php artisan koperasi:backup`
(`mysqldump | gzip`) on demand, or automatically per the cron expression set
in Settings → Backup Schedule (re-read every scheduler tick — see
`routes/console.php`). Files land in `backend/storage/app/backups/` and are
downloadable/restorable from the same screen. **Run `docker compose exec
backend php artisan schedule:work` (or an actual cron entry calling
`artisan schedule:run` every minute) for the automatic schedule to fire** —
Laravel's scheduler doesn't run itself.

## Production deployment

```bash
cp .env.production.example .env.production   # fill in DB creds + your domain
# Build once so the backend image exists, then generate the app key:
docker compose -f docker-compose.prod.yml --env-file .env.production build backend
docker compose -f docker-compose.prod.yml --env-file .env.production run --rm backend php artisan key:generate --show
# ^ paste the printed key into APP_KEY in .env.production
# Drop TLS certs into nginx/certs/ (fullchain.pem, privkey.pem — see
# nginx/certs/README.md) and set server_name in nginx/prod.conf.
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
docker compose -f docker-compose.prod.yml exec backend php artisan migrate --force
# Optional demo data (skip on a real store):
# docker compose -f docker-compose.prod.yml exec backend php artisan db:seed --force
```

The prod compose file builds without bind-mounting source into the
containers, runs `composer install --no-dev --optimize-autoloader` and
`next build` at image-build time, and doesn't expose MySQL's port to the
host.

---

## Known gaps & next steps

Being direct about what's a solid foundation vs. what still needs work
before this is genuinely production-ready:

- **Laravel 12 is current and `composer audit` is clean.** The old
  `--no-security-blocking` install flag (needed while on Laravel 11) has been
  removed from the backend Dockerfile — a blocked audit will now fail the
  build loudly instead of being bypassed.
- **Camera-based barcode scanning isn't wired up.** Cashier Mode's scan
  field works great with a USB/Bluetooth barcode scanner (they emulate a
  keyboard), but there's no camera/webcam scanning library integrated yet —
  that'd mean adding something like `@zxing/browser` to the scan input.
- **Receipts are browser-print only**, not real ESC/POS thermal-printer
  byte output. Fine for a USB/network thermal printer that has its own
  print-driver, not fine for raw serial ESC/POS control.
- **No automated tests.** Given the financial logic (checkout, discounts,
  SHU accrual, stock locking), a PHPUnit suite around
  `TransactionController::checkout()` and `DiscountService` should be the
  first thing added.
- **Excel/PDF exports are installed but smoke-test before relying on them**
  (`maatwebsite/excel`, `barryvdh/laravel-dompdf` are in the committed
  `composer.lock` and install cleanly, but large-report exports haven't been
  exercised end-to-end here).
- **`npm audit` will flag Next.js/PostCSS advisories.** `next` is pinned to
  14.2.35 (patched against the Dec 2025 RSC DoS CVEs), but the advisory
  database's ranges are broad — run `npm audit` yourself and update before
  shipping to production, rather than treating this pin as a permanent fix.

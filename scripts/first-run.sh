#!/usr/bin/env bash
# Run once after `docker compose up -d --build`, from the repo root:
#   docker compose exec backend bash /var/www/html/../scripts/first-run.sh
# or, more simply, copy it in and run inside the backend container:
#   docker compose cp scripts/first-run.sh backend:/tmp/first-run.sh
#   docker compose exec backend bash /tmp/first-run.sh
set -euo pipefail

cd /var/www/html

if [ ! -f .env ]; then
  cp .env.example .env
  echo "Created backend/.env from .env.example — edit it, then re-run this script."
fi

if ! grep -q '^APP_KEY=base64' .env 2>/dev/null; then
  php artisan key:generate --force
fi

echo "Waiting for MySQL..."
until php artisan db:show > /dev/null 2>&1; do
  sleep 2
done

php artisan migrate --force
php artisan db:seed --force
php artisan storage:link || true

echo ""
echo "Done. Demo accounts (see database/seeders/DatabaseSeeder.php for the full list):"
echo "  Admin:      admin@koperasi.test / password"
echo "  Shop Owner: owner@koperasi.test / password"
echo "  Employee:   kasir1@koperasi.test / password"
echo "  Member:     log in with the seeded phone number / password"

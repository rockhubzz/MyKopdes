#!/usr/bin/env sh
# Backend entrypoint: ensure PHP dependencies exist, then start php-fpm.
# Needed because the dev compose bind-mounts ./backend over /var/www/html,
# which hides the vendor/ directory baked into the image on first checkout
# (fresh clones have no vendor/ until `composer install` runs).
set -eu

cd /var/www/html

if [ ! -f vendor/autoload.php ]; then
  echo "vendor/autoload.php missing — running composer install..."
  composer install --no-interaction --no-progress --no-security-blocking
fi

# Never fail container startup on artisan cache commands; the app can boot
# without cached config.
php artisan config:clear || true

# Fresh checkouts have no public/storage symlink (it's gitignored) — create
# it so /storage URLs (item images, receipts) work without manual steps.
php artisan storage:link || true

exec "$@"

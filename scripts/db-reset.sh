#!/usr/bin/env bash
# Reset the MySQL database to the demo schema + seed data from
# ./mysql/init/01_koperasi_schema_seed.sql (the same file docker loads
# automatically on the first boot of an empty data volume).
#
# Run from the repo root:
#   docker compose up -d mysql          # ensure mysql is running
#   bash scripts/db-reset.sh
#
# Everything in the database is replaced: users, members, items,
# transactions, loans, etc. all go back to the demo state. Test/demo logins
# after the reset (see `mysql/init/01_koperasi_schema_seed.sql`):
#   Admin / Shop Owner / Cashier:  admin@koperasi.test | owner@koperasi.test
#                                  | kasir1@koperasi.test — password: password
#   Member:                        phone 081399990001 (first member) — password
#
set -euo pipefail

# Load the DB credentials from the root .env (DB_DATABASE/DB_USERNAME/...)
if [ -f .env ]; then
  set -a; source .env; set +a
fi

DB_NAME="${DB_DATABASE:-koperasi}"
ROOT_PASSWORD="${DB_ROOT_PASSWORD:-root_secret}"
SEED_FILE="$(dirname "$0")/../mysql/init/01_koperasi_schema_seed.sql"

if [ ! -f "$SEED_FILE" ]; then
  echo "error: seed file not found: $SEED_FILE" >&2
  exit 1
fi

echo "Resetting database '$DB_NAME' to the demo seed state..."
docker compose exec -T mysql sh -c \
  "exec mysql -uroot -p\"$ROOT_PASSWORD\" -e \"DROP DATABASE IF EXISTS \`$DB_NAME\`; CREATE DATABASE \`$DB_NAME\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci; GRANT ALL PRIVILEGES ON \`$DB_NAME\`.* TO '\${MYSQL_USER:-koperasi}'@'%';\"" \
  < /dev/null

docker compose exec -T mysql sh -c \
  "exec mysql -uroot -p\"$ROOT_PASSWORD\" '$DB_NAME'" \
  < "$SEED_FILE"

echo "Done. Database reset to the demo seed state."

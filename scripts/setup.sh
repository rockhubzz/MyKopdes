#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# MyKopdes — first-deploy setup wizard.
#
# Asks for the connection secrets that let the three containers talk to each
# other (MySQL <-> backend-dotnet <-> frontend), generates strong values for
# anything the user leaves blank, validates everything, and writes:
#     .env            — read by docker compose automatically (root .env)
#     frontend/.env   — consumed at NEXT build time by the Next.js image
#
# Run BEFORE the first `docker compose up -d --build`:
#     bash scripts/setup.sh
#
# Non-interactive: every blank answer falls back to a sensible default, so
# you can also feed it answers from a file / CI:
#     printf '\n\n\n\n' | bash scripts/setup.sh --yes
#
# Options:
#   --file PATH        destination env file (default: .env at the repo root).
#                      For production pass --file .env.production and start
#                      with `docker compose -f docker-compose.prod.yml
#                      --env-file .env.production ...`.
#   --force            overwrite the destination without asking.
#   --yes | -y         accept every default (fresh secrets generated) and
#                      write immediately.
#   env TLS_ANSWER=y   pre-answer the TLS question (scripted/CI runs).
# ----------------------------------------------------------------------------
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
ENV_FILE="${ENV_FILE:-$REPO_DIR/.env}"   # set internally; may be replaced by --file
ASSUME_YES=false
FORCE=false

usage() {
  sed -n '2,30p' "$0" | sed 's/^# \{0,1\}//'
  exit 0
}
while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) usage;;
    -y|--yes) ASSUME_YES=true; shift;;
    --force) FORCE=true; shift;;
    --file) ENV_FILE="$2"; shift 2;;
    *) echo "unknown option: $1" >&2; usage;;
  esac
done

errors=()

# ---------------------------------------------------------------- helpers ---
rand_alnum() {  # 24 hex chars — safe in .env, the MySQL CLI and the connection string
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -hex 24
  else
    tr -dc 'A-Za-z0-9' </dev/urandom | head -c 24
  fi
}

# Read a line: interactive runs use the terminal; piped/read-file runs (CI,
# scripted installs) consume stdin instead so `printf '...\n...' | setup` works.
read_line() {
  read -r "$1" || return 0   # EOF (piped input exhausted): leave empty -> defaults/confirm "yes"
}

prompt() {  # prompt <label> <default>  -> var REPLY
  local label="$1" def="${2:-}"
  if [ "$ASSUME_YES" = true ]; then REPLY="$def"; return 0; fi
  if [ -n "$def" ]; then
    read_line REPLY
    if [ -z "$REPLY" ]; then REPLY="$def"; fi
  else
    read_line REPLY
  fi
  return 0
}

prompt_secret() {  # prompt_secret <label>  -> var REPLY (default: generated)
  local label="$1" gen
  gen="$(rand_alnum)"
  if [ "$ASSUME_YES" = true ]; then REPLY="$gen"; return 0; fi
  echo "  $label"
  echo "    Press [Enter] to accept a freshly generated value,"
  echo "    or type your own (min 12 chars: A-Z a-z 0-9 !@#%^&*_-+= only —"
  echo "    no spaces, quotes, backslash, \$, ';', ':' or ',')."
  if [ -t 0 ]; then
    read -r -s -p "    > " REPLY && echo || true
  else
    read_line REPLY
  fi
  if [ -z "$REPLY" ]; then REPLY="$gen"; fi
  return 0
}

validate_secret() {  # validate_secret <name> <value>
  local name="$1" value="$2"
  if [ ${#value} -lt 12 ]; then
    errors+=("$name: too short (min 12 characters)"); return
  fi
  case "$value" in
    *[!A-Za-z0-9!@#%^*\_\-\+]*)
      errors+=("$name: contains characters not allowed in a .env / connection \
string (allowed: A-Z a-z 0-9 ! @ # % ^ * _ - + ) — regenerate or edit $ENV_FILE manually") ;;
  esac
}

validate_identifier() {
  case "$1" in
    *[!A-Za-z0-9_]*|"") errors+=("$2: only A-Z a-z 0-9 _ allowed") ;;
  esac
}

validate_port() {
  case "$1" in
    ''|*[!0-9]*) errors+=("$2: not a number: '$1'") ;;
    *) [ "$1" -ge 1 ] && [ "$1" -le 65535 ] || errors+=("$2: out of range 1-65535") ;;
  esac
}

# ------------------------------------------------------------------ intro ---
echo
echo "=========================================================="
echo "  MyKopdes setup — database / backend / frontend secrets"
echo "=========================================================="
echo "Values below configure: MySQL credentials, the backend's"
echo "database connection string + JWT signing key, and the"
echo "frontend's API base URL."
echo "Everything blank gets a strong default or generated value."
echo "Target: $ENV_FILE"
echo

# ---------------------------------------------------------------- collect ---
prompt "HTTP port for the site (nginx)" "${APP_PORT:-80}"
APP_PORT="$REPLY"
validate_port "$APP_PORT" "APP_PORT"

echo
if [ -n "${TLS_ANSWER:-}" ]; then # scripted/CI override: TLS_ANSWER=y|n (wins over --yes)
  TLS_ANS="$TLS_ANSWER"
elif [ "$ASSUME_YES" = true ]; then
  TLS_ANS="n"
else
  echo "HTTPS/TLS (optional): nginx serves TLS from certs in ./nginx/certs."
  echo "The wizard can generate a SELF-SIGNED pair (fine for internal/testing"
  echo "use — browsers will show a warning). Without TLS the site works over"
  echo "plain HTTP only. Real deployments should drop in real certs instead."
  if [ -t 0 ]; then
    read -r -p "Enable TLS? [y/N]: " TLS_ANS && echo || TLS_ANS=""
  else
    TLS_ANS=""
  fi
fi
case "${TLS_ANS:-n}" in [Yy]*) TLS_ENABLED="true";; *) TLS_ENABLED="false";; esac

if [ "$TLS_ENABLED" = true ]; then
  prompt "HTTPS port (used by docker-compose.prod-https.yml)" "${TLS_PORT:-443}"
  TLS_PORT="$REPLY"
  validate_port "$TLS_PORT" "TLS_PORT"

  CERT_DIR="$REPO_DIR/nginx/certs"
  if [ -f "$CERT_DIR/fullchain.pem" ] && [ -f "$CERT_DIR/privkey.pem" ]; then
    echo "  Existing certificates found in $CERT_DIR — keeping them."
  else
    mkdir -p "$CERT_DIR"
    GEN_OK=false
    # Prefer host openssl; fall back to a throwaway alpine container (docker
    # is required for this stack anyway).
    if command -v openssl >/dev/null 2>&1; then
      if openssl req -x509 -newkey rsa:2048 -nodes \
            -keyout "$CERT_DIR/privkey.pem" -out "$CERT_DIR/fullchain.pem" \
            -days 825 -subj "/CN=MyKopdes" \
            -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" >/dev/null 2>&1; then
        GEN_OK=true
      fi
    elif command -v docker >/dev/null 2>&1; then
      if docker run --rm -v "$CERT_DIR:/certs" alpine:3 sh -c \
        "apk add --no-cache openssl >/dev/null 2>&1 && openssl req -x509 -newkey rsa:2048 -nodes -keyout /certs/privkey.pem -out /certs/fullchain.pem -days 825 -subj '/CN=MyKopdes' -addext 'subjectAltName=DNS:localhost,IP:127.0.0.1'" >/dev/null 2>&1; then
        GEN_OK=true
      fi
    fi
    if [ "$GEN_OK" = true ]; then
      echo "  Generated self-signed certificate in $CERT_DIR (825 days)."
      echo "  Replace it with a real one for public/production domains."
    else
      errors+=("TLS: could not generate a self-signed certificate (no openssl/docker) — place fullchain.pem + privkey.pem in $CERT_DIR manually")
    fi
  fi
else
  TLS_PORT="${TLS_PORT:-443}"
fi

if [ "$TLS_ENABLED" = true ]; then
  prompt "Public URL users visit (JWT issuer/audience)" "https://localhost"
else
  if [ "$APP_PORT" = "80" ]; then
    prompt "Public URL users visit (JWT issuer/audience)" "http://localhost"
  else
    prompt "Public URL users visit (JWT issuer/audience)" "http://localhost:$APP_PORT"
  fi
fi
APP_URL="$REPLY"

prompt "Database name" "${DB_DATABASE:-koperasi}"
DB_DATABASE="$REPLY"
validate_identifier "$DB_DATABASE" "db name"

prompt "Database application user" "${DB_USERNAME:-koperasi}"
DB_USERNAME="$REPLY"
validate_identifier "$DB_USERNAME" "db user"

echo
echo "MySQL secrets — shared by the MySQL container's bootstrap,"
echo "the backend connection string, and admin tooling:"
prompt_secret "Database app password (DB_PASSWORD)"            # -> REPLY
DB_PASSWORD="$REPLY"
validate_secret "DB_PASSWORD" "$DB_PASSWORD"
prompt_secret "Database ROOT password (DB_ROOT_PASSWORD)"      # -> REPLY
DB_ROOT_PASSWORD="$REPLY"
validate_secret "DB_ROOT_PASSWORD" "$DB_ROOT_PASSWORD"

echo
if [ "$ASSUME_YES" = true ]; then
  DB_FORWARD_MODE="local"
else
  echo "Expose MySQL outside the Docker network? The backend talks to it over"
  echo "the internal docker network regardless."
  echo "  1) localhost only (recommended)   - host admin tools can connect"
  echo "  2) all interfaces                 - LAN/IPs can connect directly"
  echo "  3) not exposed                    - internal docker network only"
  read -r -p "    choice [1]: " DB_FORWARD_MODE; DB_FORWARD_MODE="${DB_FORWARD_MODE:-1}"
fi
case "${DB_FORWARD_MODE:-1}" in
  2) DB_FORWARD_HOST="";;
  3) DB_FORWARD_HOST=""; DB_FORWARD_PORT=""; DB_FORWARD_UNPUBLISHED="true";;
  *) DB_FORWARD_HOST="127.0.0.1:"; DB_FORWARD_UNPUBLISHED="";;
esac
if [ "$ASSUME_YES" = true ] || [ "${DB_FORWARD_MODE:-1}" != "3" ]; then
  prompt "Host port for MySQL (empty once option 3 chosen)" 3306
  DB_FORWARD_PORT="${REPLY:+$REPLY}"  # empty answer -> keep empty (unpublished)
  [ -n "$DB_FORWARD_PORT" ] && validate_port "$DB_FORWARD_PORT" "DB_FORWARD_PORT"
fi
[ -n "${DB_FORWARD_UNPUBLISHED:-}" ] && DB_FORWARD_PORT=""

echo
echo "JWT / API configuration:"
prompt_secret "JWT signing key (JWT_KEY)"                      # -> REPLY
JWT_KEY="$REPLY"
validate_secret "JWT_KEY" "$JWT_KEY"
prompt "JWT token lifetime (minutes)" "${JWT_EXPIRY_MINUTES:-1440}"
JWT_EXPIRY_MINUTES="$REPLY"
validate_port "$JWT_EXPIRY_MINUTES" "JWT_EXPIRY_MINUTES"

echo
echo "Frontend — the browser base URL for API calls:"
echo "  /api (same-origin via nginx) is the recommended default; it works on"
echo "  every host/IP. Note: built INTO the image, so a rebuild is needed"
echo "  when this changes."
prompt "API base URL" "${NEXT_PUBLIC_API_BASE_URL:-/api}"
NEXT_PUBLIC_API_BASE_URL="$REPLY"

# --------------------------------------------------------------- validate ---
echo
if [ ${#errors[@]} -gt 0 ]; then
  echo "Fix these, then re-run the wizard:" >&2
  for e in "${errors[@]}"; do echo "  - $e" >&2; done
  exit 1
fi

echo "------------- summary ($ENV_FILE) -------------"
mask() { local v="$1" n=${#1}; if [ $n -le 4 ]; then echo "****"; else echo "${v:0:3}****"; fi; }
cat <<EOF
  APP_PORT                 = $APP_PORT
  TLS                      = $(if [ "$TLS_ENABLED" = true ]; then echo "enabled (TLS_PORT=$TLS_PORT)"; else echo "disabled"; fi)
  APP_URL                  = $APP_URL
  DB_DATABASE/USER         = $DB_DATABASE / $DB_USERNAME
  DB_PASSWORD              = $(mask "$DB_PASSWORD")
  DB_ROOT_PASSWORD         = $(mask "$DB_ROOT_PASSWORD")
  MySQL host binding       = ${DB_FORWARD_HOST:-all interfaces}${DB_FORWARD_PORT:+:${DB_FORWARD_PORT}}
  JWT_KEY                  = $(mask "$JWT_KEY")
  JWT_EXPIRY_MINUTES       = $JWT_EXPIRY_MINUTES
  NEXT_PUBLIC_API_BASE_URL = $NEXT_PUBLIC_API_BASE_URL
----------------------------------------------

EOF

confirm() {
  [ "$ASSUME_YES" = true ] && return 0
  read_line a
  case "${a:-y}" in [Yy]*|"") return 0;; *) echo "Aborted — nothing written."; exit 1;; esac
}

if [ -f "$ENV_FILE" ] && [ "$FORCE" = false ]; then
  if [ "$ASSUME_YES" = true ]; then
    cp "$ENV_FILE" "$ENV_FILE.bak.$(date +%Y%m%d%H%M%S)"
    echo "auto-confirmed: backing up existing $ENV_FILE and overwriting (--yes)."
  else
    read_line a
    case "${a:-n}" in [Yy]*) cp "$ENV_FILE" "$ENV_FILE.bak.$(date +%Y%m%d%H%M%S)";;
      *) echo "Aborted — existing $ENV_FILE kept."; exit 1;; esac
  fi
elif [ "$FORCE" = true ] && [ -f "$ENV_FILE" ]; then
  cp "$ENV_FILE" "$ENV_FILE.bak.$(date +%Y%m%d%H%M%S)"
fi

confirm

# ----------------------------------------------------------------- write ---
cat > "$ENV_FILE" <<EOF
# MyKopdes — generated by scripts/setup.sh on $(date '+%Y-%m-%d %H:%M:%S').
# Values below wire the three containers to each other. To regenerate, rerun
# \`bash scripts/setup.sh\` (your previous file is kept as *.bak.<stamp>).
#
# NOTE: MySQL only applies DB_DATABASE/DB_USERNAME/DB_PASSWORD on the FIRST
# boot of an empty docker volume. To re-apply credentials to an already
# initialized database, stop the stack and run scripts/db-reset.sh (wipes
# demo data) or create users manually.

APP_PORT=$APP_PORT

DB_DATABASE=$DB_DATABASE
DB_USERNAME=$DB_USERNAME
DB_PASSWORD=$DB_PASSWORD
DB_ROOT_PASSWORD=$DB_ROOT_PASSWORD

# Host-side MySQL exposure: empty host = all interfaces; 127.0.0.1: = only the
# host loopback. Backend and other containers always reach MySQL via the
# internal docker network (host name: mysql).
DB_FORWARD_HOST=$DB_FORWARD_HOST
DB_FORWARD_PORT=$DB_FORWARD_PORT

# JWT signing key shared by the backend (HS256 token signing/validation).
JWT_KEY=$JWT_KEY
JWT_EXPIRY_MINUTES=$JWT_EXPIRY_MINUTES
APP_URL=$APP_URL

# TLS: docker-compose.prod.yml boots HTTP-only; docker-compose.prod-https.yml
# (extra -f file) swaps in nginx/prod-tls.conf and publishes TLS_PORT.
TLS_PORT=$TLS_PORT

# Browser base URL for API calls. /api = same-origin via nginx
# (recommended); must be baked in at image build time (rebuild to change).
NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL

# Shared network name (only needed if several stacks share a docker network).
STACK_NETWORK=koperasi_net
EOF

# Frontend build-time env: used when the Next.js image is built.
FE_ENV="$REPO_DIR/frontend/.env"
KEEP=false
if [ -f "$FE_ENV" ] && [ "$ASSUME_YES" = false ] && [ "$FORCE" = false ] && [ -t 0 ]; then
  read_line a
  case "${a:-y}" in [Yy]*) KEEP=false;; *) KEEP=true;; esac
fi
if [ "$KEEP" = false ]; then
  printf '%s\n' \
  "# Base URL the browser uses to reach the backend API." \
  "# Relative (same-origin): the browser calls <origin>/api, which nginx" \
  "# proxies to the backend. Chosen by scripts/setup.sh: $NEXT_PUBLIC_API_BASE_URL" \
  "NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL" > "$FE_ENV"
fi

# ------------------------------------------------------------------ done ---
echo
echo "Wrote $ENV_FILE"
[ "$KEEP" = false ] && echo "Wrote $FE_ENV"
echo
echo "Next steps:"
echo "  1) start the stack:   docker compose up -d --build"
echo "     (prod variant:     docker compose -f docker-compose.prod.yml --env-file $ENV_FILE up -d --build)"
if [ "$TLS_ENABLED" = true ]; then
  echo "     (HTTPS variant:    docker compose -f docker-compose.prod.yml -f docker-compose.prod-https.yml --env-file $ENV_FILE up -d --build)"
else
  echo "     (HTTPS later: generate certs in ./nginx/certs and add -f docker-compose.prod-https.yml)"
fi
echo "  2) open http://localhost:$APP_PORT and log in (demo logins: README)."
echo
echo "  The database is seeded with demo data automatically on the first boot"
echo "  of an empty volume. Later resets: bash scripts/db-reset.sh."
if [ -n "${DB_FORWARD_UNPUBLISHED:-}" ]; then
  echo
  echo "  NOTE: MySQL is NOT port-mapped to the host (internal network only)."
  echo "  Host-side DB tools need \`docker exec koperasi_mysql mysql ...\`."
fi
echo

# TLS certificates for the production Nginx (`docker-compose.prod-https.yml`
# mounts this directory read-only at `/etc/nginx/certs`).
#
# Place exactly these two files here before starting with the TLS variant:
#
#   fullchain.pem   — the certificate chain
#   privkey.pem     — the private key
#
# (e.g. copy them from Let's Encrypt: `/etc/letsencrypt/live/<domain>/`,
# or let scripts/setup.sh generate a SELF-SIGNED pair when you answer
# "Enable TLS? [y/N]" with y and no certs exist yet.)
#
# Key/cert files are gitignored on purpose — they must never be committed.
#
# The default prod config (nginx/prod.conf) is HTTP-only and needs no certs;
# the TLS variant nginx/prod-tls.conf requires both files present.

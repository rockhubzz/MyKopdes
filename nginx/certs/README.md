# TLS certificates for the production Nginx (`docker-compose.prod.yml` mounts
# this directory read-only at `/etc/nginx/certs`).
#
# Place exactly these two files here before booting prod:
#
#   fullchain.pem   — the certificate chain
#   privkey.pem     — the private key
#
# (e.g. copy them from Let's Encrypt: `/etc/letsencrypt/live/<domain>/`.)
# Key/cert files are gitignored on purpose — they must never be committed.
#
# Until real certs exist, keep only the port-80 block of `nginx/prod.conf`
# (comment out the 443 server) so the stack still boots for testing.

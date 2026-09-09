#!/usr/bin/env bash
set -euo pipefail
cd /opt/syncbarservice
exec 9>/var/lock/syncbar-https.lock
flock -w 300 9
compose=(docker compose -f docker-compose.yml -f docker-compose.https.yml)
certbot_image=certbot/certbot:v5.4.0
mkdir -p letsencrypt

if [[ "${1:-}" == renew ]]; then
  docker run --rm -p 80:80 -v "$PWD/letsencrypt:/etc/letsencrypt" "$certbot_image" \
    renew --standalone --non-interactive
  "${compose[@]}" exec -T frontend nginx -t
  "${compose[@]}" exec -T frontend nginx -s reload
  exit 0
fi

# HTTP-01 validation needs public port 80. Failure leaves the current deployment running.
if ! pgrep -x cron >/dev/null && ! pgrep -x crond >/dev/null; then
  echo "Cron deve estar ativo para renovar o certificado de seis dias." >&2
  exit 1
fi
"${compose[@]}" config --quiet
docker run --rm -p 80:80 -v "$PWD/letsencrypt:/etc/letsencrypt" "$certbot_image" \
  certonly --standalone --non-interactive --agree-tos --register-unsafely-without-email \
  --preferred-profile shortlived --ip-address 9.205.156.87 --cert-name syncbar-ip --keep-until-expiring
"${compose[@]}" pull
"${compose[@]}" run --rm --no-deps frontend nginx -t
"${compose[@]}" run --rm --no-deps edge haproxy -c -f /usr/local/etc/haproxy/haproxy.cfg
"${compose[@]}" up -d --force-recreate

# IP certificates expire after six days. Check renewal twice daily; reload Nginx afterwards.
cat > /etc/cron.d/syncbar-https <<'CRON'
17 */12 * * * root /bin/bash /opt/syncbarservice/deploy/https.sh renew >> /var/log/syncbar-https-renew.log 2>&1
CRON
chmod 644 /etc/cron.d/syncbar-https
curl --fail --silent --show-error --retry 30 --retry-all-errors --retry-delay 3 \
  --connect-timeout 5 --max-time 10 https://9.205.156.87:84/health

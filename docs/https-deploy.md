# HTTPS na porta 84

O workflow publica `docker-compose.https.yml` e executa `deploy/https.sh` na VM.
Certbot 5.4 emite um certificado público para `9.205.156.87` com o perfil
shortlived. A porta TCP 80 deve estar livre na VM e liberada no firewall público
para emissão e renovação HTTP-01. A porta 84 deve continuar liberada.
É necessário Docker Compose >= 2.24.4, Bash, flock e cron ativo na VM.

HAProxy identifica TLS e encaminha para Nginx:443, mantendo requisições HTTP
em Nginx:80 na mesma porta pública 84. Assim o webhook HTTP existente do Asaas
continua recebendo POSTs sem redirecionamento. A URL do iFood passa a ser
`https://9.205.156.87:84/api/webhook/ifood`.

Os certificados e a chave privada ficam em `/opt/syncbarservice/letsencrypt`,
fora da imagem e do Git. A tarefa `/etc/cron.d/syncbar-https` tenta renovar duas
vezes por dia e recarrega Nginx; erros ficam em `/var/log/syncbar-https-renew.log`.
Esses certificados duram seis dias, portanto a execução do cron deve ser monitorada.

Antes da troca, o script exige emissão válida e valida Nginx/HAProxy. Conflito
na porta 80 ou falha de validação ACME interrompem a publicação antes de recriar
os serviços. O health check HTTPS verifica o certificado, sem usar `-k`.

Para voltar ao transporte HTTP: `sudo docker compose -f docker-compose.yml
-f docker-compose.https.yml stop edge`, seguido de `sudo docker compose -f
docker-compose.yml up -d --force-recreate frontend`. Os certificados permanecem
no disco. Desative o webhook HTTPS no iFood ou volte a Polling antes do rollback.

Referência: https://letsencrypt.org/2026/03/11/shorter-certs-certbot

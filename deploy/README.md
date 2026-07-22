# CI/CD - SyncBar

Pipeline no mesmo padrao dos servicos irmaos (EmailSendingService, SmsSendingService,
WhatsAppSendingService, Parking). A SyncBar.API e publicada na **mesma VM**, na **porta 84**.

Mapa de portas na VM:

| Servico                 | Porta host |
|-------------------------|------------|
| EmailSendingService     | 80         |
| SmsSendingService       | 81         |
| WhatsAppSendingService  | 82         |
| Parking                 | 83         |
| **SyncBar**             | **84**     |

## Estrutura do backend

A solution e o Dockerfile ficam em `backend/src` (codigo em `backend/src`, testes em
`backend/test`), no mesmo layout do Parking:

```
backend/
  src/   -> SyncBar.API | SyncBar.Application | SyncBar.Domain | SyncBar.Infrastructure | SyncBar.sln | Dockerfile
  test/  -> SyncBar.Tests | SyncBar.ArchTests | SyncBar.Specs
```

## Fluxo

1. **CI** (`.github/workflows/ci.yml`) — em cada push/PR na `main` (e `master`/`develop`):
   builda e testa o backend (`backend/src/SyncBar.sln`, .NET 9) e o frontend (React + Vite),
   e valida o build das imagens Docker.
2. **CD** (`.github/workflows/deploy.yml`) — dispara automaticamente quando o **CI**
   termina verde na `main` (ou manualmente em Actions):
   - builda a imagem Docker (contexto `backend/src`) e faz push no GHCR
     (`ghcr.io/rodrigofurlaneti/syncbar:latest`);
   - conecta via SSH na VM, faz `docker pull` e recria o container `syncbar-api`
     em `-p 84:8080` com `--env-file /opt/syncbarservice/.env`;
   - valida o health em `http://localhost:84/health`.

## Secrets do repositorio (Settings > Secrets and variables > Actions)

Os mesmos usados pelos servicos irmaos:

- `VM_HOST` — IP/host da VM
- `VM_USER` — usuario SSH
- `VM_SSH_KEY` — chave privada SSH
- `GHCR_PAT` — Personal Access Token com escopo `read:packages` (pull na VM)

## Preparacao na VM (uma vez)

```bash
sudo mkdir -p /opt/syncbarservice
sudo cp deploy/.env.example /opt/syncbarservice/.env
sudo nano /opt/syncbarservice/.env   # preencher conn string do SQL, Jwt__Secret e CORS
```

A porta 84 precisa estar liberada no firewall / NSG da VM.

> A SyncBar.API depende de um SQL Server. O `.env` deve apontar
> `ConnectionStrings__DefaultConnection` para uma instancia acessivel pela VM.
> **Diferente do Parking, a SyncBar.API nao aplica migrations no startup** — garanta
> que o banco `BarRestauranteDb` esteja criado e atualizado (scripts em `/sql` ou
> `dotnet ef database update`) antes de expor o servico.

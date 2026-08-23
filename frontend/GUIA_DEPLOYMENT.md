# 🚀 GUIA DE DEPLOYMENT & TESTES

## ✅ Compilação

### Build Frontend
```bash
cd frontend
npm run build
```

**Resultado Esperado:**
```
✅ Build sucesso
✅ Sem warnings TypeScript
✅ Assets otimizados
```

### Build Backend (se necessário)
```bash
cd backend
dotnet build
dotnet publish -c Release -o ./publish
```

---

## 🧪 Testes Locais

### 1. Iniciar Frontend em Dev Mode
```bash
cd frontend
npm run dev
```

Acesse: `http://localhost:5173`

### 2. Iniciar Backend API
```bash
cd backend
dotnet run --project src/SyncBar.API
```

API em: `http://localhost:5000`

### 3. Testar Novas Páginas

#### Dashboard
```
URL: http://localhost:5173/integracoes/ifood/dashboard
Esperado:
  ✅ 4 cards de métricas de pedidos
  ✅ 3 cards de breakdown por tipo
  ✅ Card de financeiro
  ✅ Card de reviews
  ✅ Tabela de últimos pedidos
  ✅ 6 links rápidos
  ✅ Auto-refresh a cada 15s
```

#### Status Detalhado
```
URL: http://localhost:5173/integracoes/ifood/status
Esperado:
  ✅ Card grande com status
  ✅ Botão toggle (🟢/🔴)
  ✅ Resumo de erros/avisos
  ✅ Aba de validações gerais
  ✅ Aba de validações por operação
  ✅ Seletor de operação (Delivery/Takeout)
```

#### Reviews
```
URL: http://localhost:5173/integracoes/ifood/avaliacoes
Esperado:
  ✅ 4 cards de métricas
  ✅ Aba de não respondidas
  ✅ Aba de respondidas
  ✅ Cards de review com estrelas
  ✅ Botão "Responder" (só em aberta)
  ✅ Modal ao clicar em responder
  ✅ Validação de caracteres
```

#### Analytics
```
URL: http://localhost:5173/integracoes/ifood/indicadores
Esperado:
  ✅ Filtros de data
  ✅ Botão "Filtrar"
  ✅ Cards de KPIs (se houver dados)
  ✅ JSON bruto visível
  ✅ Nota sobre Analytics
```

---

## 🐛 Checklist de Testes

### Teste Funcional

- [ ] Dashboard carrega sem erros
- [ ] Status muda ao clicar toggle
- [ ] Review responde com sucesso
- [ ] Analytics filtra por período
- [ ] Links rápidos navegam corretamente
- [ ] Auto-refresh funciona
- [ ] Error states exibem mensagens

### Teste Responsividade

- [ ] Mobile (320px): Layout em 1 coluna
- [ ] Tablet (768px): Layout em 2 colunas
- [ ] Desktop (1024px+): Layout em 3+ colunas
- [ ] Botões clickáveis em mobile
- [ ] Modals se ajustam ao tamanho

### Teste de Performance

- [ ] Primeira carga < 3s
- [ ] Auto-refresh não trava interface
- [ ] Múltiplas queries em paralelo
- [ ] Sem memory leaks (DevTools)
- [ ] Cache funciona (network tab)

### Teste de Dados

- [ ] Formatadores aplicam cores corretas
- [ ] Trends mostram percentual correto
- [ ] Datas formatadas em pt-BR
- [ ] Moedas formatadas (R$ 0,00)
- [ ] Emojis exibem corretamente

### Teste de Acessibilidade

- [ ] Navegação por tab funciona
- [ ] Cores têm contraste WCAG AA
- [ ] Alternativas de texto para ícones
- [ ] Modals têm focus trap
- [ ] Teclado entra em inputs

---

## 📋 Verificação de Tipos

### TypeScript Strict Mode
```bash
cd frontend
npx tsc --noEmit
```

**Resultado Esperado:**
```
✅ 0 erros
✅ 0 warnings
```

### Lint & Format
```bash
# Verificar código
npx eslint src --fix

# Formatar
npx prettier --write src
```

---

## 🔗 Verificação de APIs

### Endpoints Necessários (Backend)

Certifique-se que estes endpoints estão retornando dados:

```bash
# Status
GET http://localhost:5000/api/integrations/ifood/status/branch/{branchId}

# Reviews
GET http://localhost:5000/api/integrations/ifood/reviews/branch/{branchId}

# Financial Summary
GET http://localhost:5000/api/integrations/ifood/financial/branch/{branchId}/summary

# Analytics KPIs
GET http://localhost:5000/api/integrations/ifood/analytics/kpis/branch/{branchId}

# Disponibilidade (toggle)
PUT http://localhost:5000/api/integrations/ifood/merchants/{branchId}/availability

# Responder Review
POST http://localhost:5000/api/integrations/ifood/reviews/branch/{branchId}/{reviewId}/respond
```

### Teste com cURL

```bash
# Status da loja
curl -H "Authorization: Bearer {token}" \
  http://localhost:5000/api/integrations/ifood/status/branch/1

# Resultado esperado:
{
  "operationState": "OPEN",
  "available": true,
  "validations": [...]
}
```

---

## 🚨 Troubleshooting

### Problema: Dashboard não carrega

**Sintomas:**
- Tela em branco
- Console: erro 404 em API
- Loading spinner nunca desaparece

**Solução:**
```bash
1. Verificar se backend está rodando
2. Confirmar branchId no authStore
3. Ver console do navegador (F12)
4. Verificar CORS headers
5. Checar chamadas de rede (Network tab)
```

### Problema: Toggle de disponibilidade não funciona

**Sintomas:**
- Botão não responde
- Toast de erro
- Status não muda

**Solução:**
```bash
1. Verificar permissões (ManagerGate)
2. Confirmar endpoint PUT existe no backend
3. Ver erro completo no console
4. Testar com cURL (veja acima)
```

### Problema: Reviews não aparecem

**Sintomas:**
- Aba vazia
- "Carregando..." indefinido
- Erro 500

**Solução:**
```bash
1. Confirmar há reviews no BD
2. Verificar endpoint GET /reviews
3. Checar formato de resposta
4. Validar tipos TypeScript
```

### Problema: Analytics mostra "—" ou undefined

**Sintomas:**
- KPIs não parseiam
- JSON bruto inválido
- Buckets vazios

**Solução:**
```bash
1. Verificar schema de resposta do iFood
2. Confirmar período tem dados
3. Validar parse de JSON em JS
4. Ver resposta bruta em Network tab
```

---

## 📊 Monitoramento

### Console do Navegador (F12)

#### Logs Normais
```
[React Query] Fetching /api/integrations/ifood/status/branch/1
[React Query] Query cached: integrations/ifood/status
[React Query] Refetching in 30000ms
```

#### Logs de Erro (Procure)
```
❌ TypeError: Cannot read property 'data' of undefined
❌ 404 Not Found: /api/integrations/ifood/status/branch/1
❌ CORS error: No 'Access-Control-Allow-Origin'
```

### DevTools React Query
```bash
# Instalar devtools
npm install @tanstack/react-query-devtools

# Usar em dev.tsx
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'

# Acessar em http://localhost:5173
# Botão 🔭 no canto inferior direito
```

---

## 📦 Deploy para Produção

### 1. Build Otimizado
```bash
cd frontend
npm run build
# Gera pasta dist/ com assets minificados
```

### 2. Testar Build
```bash
npm run preview
# Simula servidor de produção em http://localhost:4173
```

### 3. Upload para CDN/Servidor
```bash
# Copiar dist/ para servidor web
scp -r dist/* user@server:/var/www/syncbar-frontend/

# Ou usando docker
docker build -t syncbar-frontend:latest .
docker push registry.example.com/syncbar-frontend:latest
```

### 4. Validar em Produção
```bash
1. Acessar https://seu-dominio.com/integracoes/ifood/dashboard
2. Testar cada página
3. Verificar console (F12) em produção
4. Monitora erros (Sentry/LogRocket)
```

---

## 🔐 Segurança

### Checklist de Segurança

- [ ] Tokens salvos em `httpOnly` cookies (não localStorage)
- [ ] API calls incluem `Authorization` header
- [ ] CORS corretamente configurado no backend
- [ ] Inputs validados antes de enviar
- [ ] Senhas/tokens não logados em console
- [ ] CSP headers configurados
- [ ] Dependências auditadas (`npm audit`)

### Testar Segurança
```bash
# Audit de dependências
npm audit

# Scan de vulnerabilidades
npx snyk test
```

---

## 📈 Performance

### Métricas Esperadas

| Métrica | Esperado | Ferramenta |
|---------|----------|-----------|
| FCP | < 1.5s | Lighthouse |
| LCP | < 2.5s | Lighthouse |
| CLS | < 0.1 | Lighthouse |
| TTI | < 3s | Lighthouse |
| Lighthouse Score | > 90 | Lighthouse |

### Testar Performance
```bash
1. Abrir DevTools (F12)
2. Aba "Lighthouse"
3. Clicar "Analyze page load"
4. Ver scores por categoria
```

---

## ✅ Checklist Final

Antes de fazer deploy:

- [ ] TypeScript sem erros: `npx tsc --noEmit`
- [ ] Build sucesso: `npm run build`
- [ ] Testes locais OK
- [ ] APIs respondendo corretamente
- [ ] Sem console warnings
- [ ] Responsividade verificada
- [ ] Performance OK (Lighthouse > 90)
- [ ] Documentação atualizada
- [ ] Changelong preenchido

---

## 🎉 Pronto para Produção!

```bash
✅ Compilação: OK
✅ Testes: OK
✅ Performance: OK
✅ Segurança: OK
✅ Documentação: OK

🚀 READY FOR PRODUCTION
```

---

## 📞 Suporte

Encontrou problema?

1. Verifique este guia
2. Procure no console (F12)
3. Verifique Network tab
4. Leia documentação completa (MODERNIZACAO_UX_README.md)
5. Contacte desenvolvedor

---

**Última atualização**: 2024
**Versão**: 1.0
**Status**: ✅ Pronto

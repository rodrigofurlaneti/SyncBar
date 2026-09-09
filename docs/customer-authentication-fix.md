# Correção do cadastro e autenticação do cliente web

## Diagnóstico

O cadastro do React cria o cliente, faz login e salva o endereço em requisições
separadas. O login usava CustomerAppUser.Id em RefreshToken.AppUserId e
AccessLog.AppUserId, cujas chaves estrangeiras apontam para AppUser. O esquema do
Dump20260909.sql confirma esses vínculos. Clientes sem um usuário administrativo
de mesmo ID recebem erro de integridade referencial, apresentado como erro 500.
O cliente pode já estar cadastrado quando o login falha.

Esse defeito depende dos dados, não do sistema operacional. O log de cancelamento
de AuthController.Refresh enviado pelo usuário não confirma a causa do incidente
no aparelho; falta correlacionar o log da tentativa de customer-login na Azure.

## Alterações

- CustomerRefreshToken tem tabela e repositório próprios, com FK para CustomerAppUser.
- AccessLog tem CustomerAppUserId separado e mantém AppUserId nulo para clientes.
- UserName do log aceita 150 caracteres, compatível com o e-mail do cliente.
- A auditoria genérica não interpreta a identidade Customer como AppUserId.
- POST /api/auth/customer-refresh renova apenas sessões de clientes e emite perfil
  Customer, sem cargos, permissões ou recursos administrativos.
- O frontend renova a sessão do cliente pelo novo endpoint, em memória, e repete
  uma única vez a requisição que retornou 401. A sessão administrativa fica separada.
- A migração revoga refresh tokens antigos: não existe informação suficiente para
  distinguir os tokens administrativos dos tokens de clientes gravados anteriormente
  com IDs coincidentes. Tokens antigos não são migrados por suposição.

## Publicação

Aplicar a migration `202609090004_SeparateCustomerAuthentication` junto da atualização
da API e do frontend, evitando tráfego com versões incompatíveis durante a troca.
A API nova exige o esquema novo. A revogação dos refresh tokens antigos pode exigir
novo login de usuários internos quando a sessão atual precisar ser renovada.
O rollback remove a tabela/coluna de clientes, mas não restaura tokens revogados nem
reduz o tamanho do campo de e-mail, para evitar truncamento de dados.

Nenhuma alteração foi aplicada na Azure ou no banco de produção por esta tarefa.

## Validação

- Testes relacionais SQLite com chaves estrangeiras ativas durante autenticação:
  reprodução da rejeição antiga, login válido e senha incorreta sem AppUser,
  rotação de token, rejeição de reutilização e isolamento das sessões.
- SQL da migração executado em MySQL 8 descartável, com dados sintéticos:
  criação das FKs, revogação de tokens antigos e gravação de cliente de ID 901
  sem AppUser correspondente.
- 409 testes de regressão de autenticação, cadastro e endereços no backend passaram.
- Playwright: cadastro/checkout nos perfis desktop Chromium, Android Chromium e
  iPhone WebKit, incluindo renovação da sessão sem chamar refresh administrativo.
  Os 66 cenários passaram. Esses testes usam API simulada; não equivalem a teste
  no Android físico/Azure. TypeScript e build Vite também passaram.

Após publicar, repetir seleção de produto → cadastro → pedido no Chrome do Android
e conferir as três requisições de cadastro/login/endereço. Se houver falha, registrar
endpoint, horário e exceção interna, sem copiar senhas ou tokens.

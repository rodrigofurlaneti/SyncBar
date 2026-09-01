// Esta interface foi removida (Fase 12 — revisão de duplicidade no polling de eventos).
//
// Ela duplicava IIfoodTokenProvider (SyncBar.Application.Abstractions.Integrations.Ifood — já
// implementado, cacheado em memória por empresa, e em uso desde a Fase 2) com uma assinatura
// incompatível com o resto do projeto (GetAccessTokenAsync(Guid companyId, ...) — em todo o
// resto do SyncBar, CompanyId é long, não Guid) e sem nenhuma implementação registrada no DI:
// qualquer código que pedisse IIfoodAuthService via injeção de dependência quebraria em tempo de
// execução (InvalidOperationException ao resolver o serviço).
//
// Foi criada junto com SyncBar.Infrastructure/Workers/IfoodPollingWorker.cs (ver comentário lá
// para o restante dos problemas daquele arquivo, incluindo os motivos pelos quais nem compilava).
//
// Este arquivo pode ser excluído do projeto com segurança.

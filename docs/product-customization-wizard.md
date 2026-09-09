# Wizard de personalização de produtos

O seletor compartilhado agora usa um dialog nativo, com tema claro, foto/descrição
do produto, navegação por etapas e subtotal atualizado a cada seleção. Produtos
sem foto exibem um placeholder; nenhuma imagem de produto é inventada.

Acima de 640px, o layout tem duas colunas. No mobile, foto, identificação e etapas
ficam no topo; a lista central rola e subtotal/ação permanecem no rodapé, incluindo
a área segura do dispositivo. Há adaptação adicional para telas de pouca altura.

Opcionais gratuitos e adicionais pagos são etapas separadas no atendimento. Os
grupos de complementos existentes também viram etapas, tanto no atendimento quanto
nos fluxos públicos que já usam o seletor. Os contratos e o estado de envio do
carrinho foram preservados; esta mudança não cria novos endpoints públicos.
Foi corrigida a adaptação do carrinho do cliente: os nomes, preços e IDs de grupo
agora são mantidos. Personalizações diferentes do mesmo produto têm linhas próprias;
alterar a quantidade ou remover uma linha não afeta as demais personalizações.

Limites de grupos vêm de minSelection/maxSelection. Opcionais e boosts não possuem
limite próprio no modelo atual: o máximo exibido corresponde às opções disponíveis.
Ao atingir o limite, opções não selecionadas ficam desabilitadas; desmarcar libera
novas escolhas. Etapas obrigatórias impedem a confirmação se estiverem incompletas.

A ação mostra Pular em etapas opcionais vazias, Avançar após escolher e Adicionar
ao pedido na etapa final. É possível voltar ou usar a navegação lateral mantendo as
seleções. Preços, descontos existentes e IDs enviados ao backend são preservados.

O dialog usa aria-labelledby, foco contido e restauração ao fechar. Checkboxes
nativos permanecem acessíveis, com representação visual compacta e foco visível.
Tab, Shift+Tab, Espaço e Esc são cobertos por testes. Carregamento mostra skeleton;
falhas permitem tentar novamente. Durante envio, ações ficam bloqueadas.

Arquitetura: useProductWizard concentra seleção, limites e subtotal; ProductWizard
renderiza a interface; ProductIdentity e WizardSidebar são memoizados. Os wrappers
adaptam os contratos atuais sem duplicar a lógica do wizard.

Validação: executar `node node_modules/@playwright/test/cli.js test --config playwright.wizard.config.ts`
no frontend. Os perfis cobrem Chromium desktop/tablet,
Android pequeno e WebKit/iPhone, com API simulada. Isso não substitui teste em
aparelho físico após publicação.

Foram validados os 64 cenários de wizard/carrinho nos quatro perfis, incluindo
limites, retorno de foco via teclado, listas longas, cancelamento, repetição após
erro, subtotal, personalizações distintas e IDs enviados no checkout. TypeScript
e build de produção Vite passaram. As capturas desktop e Android foram revisadas.

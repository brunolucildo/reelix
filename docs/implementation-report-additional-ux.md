# Relatório de implementação — ajustes adicionais de UX

## Escopo e critérios de classificação

Este relatório cobre os cinco ajustes complementares de acompanhamento, conquistas e listas. Alterações anteriores presentes no mesmo worktree não são consideradas novas entregas deste relatório.

- **Concluída:** implementação com comportamento central verificado pelos testes indicados.
- **Implementada, mas não validada:** implementação com verificações automatizadas parciais, ainda sem validação integral da experiência/persistência real.
- Nenhum teste com mocks é apresentado como execução autenticada contra a API, PostgreSQL ou dispositivo.

| Ajuste | Classificação | Validação ainda necessária |
|---|---|---|
| 1. Episódios restantes da série inteira | Concluída | Conferência integrada com catálogo real e Home autenticada |
| 2. Medalhas no perfil e menu | Implementada, mas não validada | Layout, acessibilidade e modal sobre o drawer em Android/iOS |
| 3. Voltar no topo de `/lists` | Concluída | Conferência da pilha no aplicativo em execução |
| 4. Criação de listas sob demanda | Implementada, mas não validada | Teclado, modal e criação autenticada persistida após recarregar |
| 5. Card único, edição e exclusão contextual | Implementada, mas não validada | Operações autenticadas e isolamento dos dados em banco real |

## 1. Episódios restantes da série inteira

**Problema e causa:** o card de Continuar Assistindo consultava progresso da temporada identificada pelo episódio mais recente. O contrato de progresso da série não possuía inventário completo nem quantidade de restantes.

**Solução:**

- O serviço consulta as temporadas do catálogo e agrega episódios de todas as temporadas válidas, inclusive temporada 0/especiais.
- Temporadas negativas e datas de lançamento futuras são ignoradas. Episódios com data futura também não entram no denominador, mesmo quando já possuem registro de tracking.
- A agregação utiliza IDs únicos, evitando duplicatas dentro e entre temporadas. A contagem assistida usa a interseção com o tracking da conta autenticada.
- Episódios sem número convencional continuam identificados por GUID. Episódios sem data seguem a política já existente de disponibilidade: são incluídos. Isso preserva consistência, mas não permite garantir a data real de lançamento quando o provedor não a informa.
- Sem inventário conhecido, totais, restantes e percentual continuam desconhecidos; inventário vazio retorna zero disponíveis/restantes sem declarar conclusão.
- Uma temporada que não pode ser carregada gera erro, em vez de um total parcial apresentado como completo.
- O contrato inclui `pendingEpisodeCount`; o card utiliza progresso da série inteira. A temporada identificada continua apenas na legenda.
- Marcar/desmarcar episódios invalida o prefixo de tracking existente, incluindo o novo resumo. O provedor faz parte da chave de cache.

**Arquivos/componentes:**

- [TrackingContracts.cs](../backend/Reelix.Application/Tracking/TrackingContracts.cs), [TrackingService.cs](../backend/Reelix.Application/Tracking/TrackingService.cs).
- [TrackingCatalog.cs](../backend/Reelix.Infrastructure/Persistence/Tracking/TrackingCatalog.cs).
- [TrackingEndpoints.cs](../backend/Reelix.Api/Tracking/TrackingEndpoints.cs), [Tracking.http](../backend/Reelix.Api/Tracking.http).
- [tracking-api.ts](../mobile/src/services/tracking-api.ts), [use-tracking.ts](../mobile/src/hooks/use-tracking.ts), [tracked-series-section.tsx](../mobile/src/components/discovery/tracked-series-section.tsx).
- [TrackingServiceTests.cs](../backend/Reelix.UnitTests/Tracking/TrackingServiceTests.cs).

**Resultado verificado:** inventário de 10 + 12 + 8 episódios com 15 assistidos retorna **15 assistidos · 15 restantes**, com 50% de progresso. Os testes também cobrem marcar/desmarcar, conclusão em 100%, outra conta, especiais, duplicatas, metadados ausentes, futuros, inventário desconhecido/vazio e falha de temporada.

**Classificação:** **Concluída** quanto à regra de cálculo e integração no código, com testes automatizados aprovados. A aparência e atualização na Home autenticada não foram verificadas em execução real.

## 2. Conquistas somente como medalhas

**Problema e causa:** o perfil renderizava cards com título e descrição ao lado da medalha; o menu não mostrava as conquistas junto ao avatar.

**Solução:**

- Grade compartilhada exibe somente medalhas clicáveis; nomes permanecem nos rótulos de acessibilidade, não como texto visual na grade.
- Medalhas bloqueadas mantêm a distinção visual do componente existente.
- Selecionar abre modal com nome, descrição, critério, progresso, estado e datas disponíveis de desbloqueio/reativação.
- O contrato não possui um campo independente para critério. A descrição real do catálogo, que informa a regra, é reutilizada; não foi criada uma regra fictícia.
- O menu mostra uma grade compacta de conquistas ativas ao lado do avatar, com carregamento e retentativa em caso de erro.
- O modal compartilhado possui fechamento, rolagem, área segura e tratamento de teclado. O viewport de toast foi reutilizado dentro dos modais para manter erros visíveis.

**Arquivos/componentes:**

- [achievement-grid.tsx](../mobile/src/components/gamification/achievement-grid.tsx), reutilizando [achievement-medal.tsx](../mobile/src/components/gamification/achievement-medal.tsx).
- [profile.tsx](../mobile/src/app/profile.tsx), [user-drawer.tsx](../mobile/src/components/navigation/user-drawer.tsx).
- [modal-dialog.tsx](../mobile/src/components/modal-dialog.tsx), [toast-provider.tsx](../mobile/src/providers/toast-provider.tsx).

**Resultado e validação:** TypeScript e lint aprovados. Um script de sessão transpila o componente real e exercita handlers com hooks/elementos simulados: ausência de textos fora do modal, conquistas desbloqueadas/bloqueadas, abertura, conteúdo e fechamento. O wireframe abriu no navegador, carregou os SVGs e apresentou os detalhes simulados.

**Classificação:** **Implementada, mas não validada** integralmente. O teste de handlers não renderiza React Native. Permanecem pendentes proporções em telas pequenas, leitor de tela e apresentação de um modal de detalhes sobre o modal do drawer em Android/iOS.

## 3. Voltar no topo de `/lists`

**Problema e causa:** o botão compartilhado de voltar estava ao final do conteúdo rolável.

**Solução:** o mesmo `BackButton` usado nas demais telas foi colocado no início do cabeçalho, antes do título, dentro da área segura. Mantém ícone e alvo de toque do Design System. O componente usa `router.back()`, sem redirecionamento fixo para Home.

Não foi encontrado um cabeçalho genérico de telas internas que precisasse ser conectado. O `AppHeader` existente é específico da Home; seus atalhos não foram duplicados em listas.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx), reutilizando [back-button.tsx](../mobile/src/components/navigation/back-button.tsx), sem novo contrato de navegação.

**Resultado e validação:** inspeção do componente compartilhado confirmou retorno pela pilha. O script de handlers verificou que o botão aparece antes dos campos; TypeScript e lint aprovados.

**Classificação:** **Concluída** quanto à posição, reutilização e ligação com a navegação existente. Não houve teste de navegação E2E entre telas reais.

## 4. Criação de listas somente sob demanda

**Problema e causa:** nome, descrição e ações de criação ficavam permanentemente na tela.

**Solução:**

- O formulário inicia oculto e é aberto pelo ícone existente de adicionar lista.
- Criação/edição usam o mesmo modal, com cancelar, fechar e campos preservados em caso de falha.
- Nome obrigatório normalizado de 1 a 80 caracteres e descrição de até 500 seguem as regras existentes.
- Durante a persistência, campos e fechamento ficam bloqueados para evitar interrupção e envio concorrente pela interface.
- Sucesso fecha o formulário, atualiza os caches e seleciona a lista salva. Erros de API permanecem explícitos em toast sobre o modal.
- Nenhum endpoint ou mecanismo de persistência novo foi introduzido.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx), [modal-dialog.tsx](../mobile/src/components/modal-dialog.tsx), [toast-provider.tsx](../mobile/src/providers/toast-provider.tsx).

**Resultado e validação:** script de handlers aprovado para estado inicialmente oculto, abertura, validação, loading, falha mantendo o rascunho, callback de criação e invalidação de cache. TypeScript e lint aprovados.

**Classificação:** **Implementada, mas não validada** de ponta a ponta. Criação autenticada, recarga posterior e interação com teclado no dispositivo não foram executadas.

## 5. Lista privada em uma única área visual

**Problema e causa:** seleção/ações e apresentação dos conteúdos eram renderizadas em áreas separadas, fazendo uma mesma lista parecer dois cards.

**Solução:**

- Cada lista possui um card com nome, descrição disponível, quantidade combinada de filmes/séries e menu de três pontos.
- Ao selecionar, conteúdos e paginação são exibidos dentro do próprio card, mantendo remoção e reordenação.
- O menu abre edição preenchida ou confirmação de exclusão nomeando explicitamente a lista.
- Editar modifica apenas nome/descrição pelo endpoint existente; não substitui os itens.
- Cancelar não exclui. Falha de exclusão mantém a confirmação; sucesso atualiza seleção, paginação e caches.
- A exclusão usa apenas o endpoint da lista personalizada. Não dispara operações de Favoritos, Watchlist ou catálogo.
- O backend existente filtra a lista pelo proprietário. A configuração de persistência aplica cascade aos vínculos da lista e restrict às referências do catálogo.

**Arquivos/componentes:** [lists.tsx](../mobile/src/app/lists.tsx) e modal compartilhado. Não foi necessário alterar os endpoints nem a configuração de banco das listas.

**Resultado e validação:** script de handlers aprovado para card unificado, edição preservando os conteúdos simulados, confirmação nomeada, cancelamento, falha, exclusão e invalidação. Testes existentes de serviço e configuração de listas aprovados, incluindo `Apply_CascadesOwnedListItemsButPreservesCatalogReferences`.

**Classificação:** **Implementada, mas não validada** integralmente. Os testes verificam regras/configuração e callbacks; não substituem uma exclusão em PostgreSQL real seguida da conferência de Favoritos, Watchlist e catálogo.

## Validações executadas

- **Backend:** 44 testes aprovados, zero falhas, nos filtros `TrackingServiceTests`, `UserListsServiceTests`, `ListsPersistenceConfigurationTests` e `AuthenticationEndpointTests`. O projeto da API também foi compilado pelas referências dos testes.
- **Mobile:** `npx tsc --noEmit` e `npm run lint` aprovados.
- **UX com mocks:** script de sessão aprovado para listas e medalhas; não é uma suíte E2E nem teste de renderização React Native.
- **Wireframes:** sintaxe JavaScript verificada e estados de detalhes/criação/edição/exclusão exercitados no navegador, com assets de medalhas carregados.
- **Patch:** `git diff --check` sem erros.
- Avisos de SDK .NET preview e de consulta indisponível ao feed NuGet (`NU1900`) não impediram compilação/testes. A consulta de vulnerabilidades não foi validada por esse feed.

## Documentação e pendências operacionais

- Atualizados [especificação de tracking](specs/05-tracking.md), [guia da Home](home-discovery-setup.md) e [wireframes](wireframes/reelix-visual-wireframes.html).
- **Sincronização documental em 02/11/2026:** README, arquitetura, especificações dos módulos, guias de configuração e páginas de planos foram revisados para separar código presente, dependências por ambiente e funcionalidades ainda não implementadas. O wireframe central agora cobre 31 telas em 11 módulos, incluindo Social, Notificações, Gamificação e a lacuna explícita de Widgets.
- **Recompilar e reiniciar a API antes de testar o mobile:** o novo contrato contém `pendingEpisodeCount`. Uma instância antiga não entrega o novo contrato. Não foram interrompidos processos do usuário.
- A agregação consulta temporadas sequencialmente, respeitando o contexto de persistência scoped. Séries longas podem exigir muitas chamadas ao provedor; não foi introduzido cache adicional nem realizado teste de carga.
- Nenhuma migração, dependência ou dado artificial foi adicionado à aplicação. Não houve commit.
- Para homologar os itens pendentes: abrir perfil/menu em Android e iOS; criar, editar e excluir uma lista autenticada; recarregar; conferir manutenção dos conteúdos e dos relacionamentos independentes; marcar/desmarcar episódios em temporadas diferentes e conferir a Home.

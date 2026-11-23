# 0009 — Social

> Spec retroativa: documenta o que foi definido e implementado antes da adoção do fluxo `.specs/`, no formato anterior ao `_template.md` (seções de escopo, requisitos, restrições e critérios). Specs novas, a partir da `0013`, seguem o template.

- **Status:** concluída (implementada; limites de validação em `.docs/ARCHITECTURE.md` §2 e §6 e em `.docs/QA.md`)
- **Slug:** social
- **Data:** 2026-10-25
- **Atualizado em:** 2026-11-19
- **Owner:** Produto Reelix
- **Relacionados:** `.docs/ARCHITECTURE.md`, `.docs/BUSINESS.md`, `.docs/QA.md`

## Escopo

Front-End, Back-End e banco de dados, com integração aos módulos de Perfil, Tracking, Avaliações e Listas.

## Contexto

O Reelix deverá permitir que os usuários se conectem com amigos e compartilhem atividades relacionadas a filmes e séries.

O objetivo é criar uma experiência social inspirada em plataformas de acompanhamento de entretenimento, sem tornar automaticamente públicos os dados pessoais ou o histórico de visualização.

A primeira versão deverá priorizar amizades e atividades entre usuários autorizados. Seguidores públicos poderão ser adicionados posteriormente, caso sejam aprovados como requisito do produto.

## Requisitos Funcionais

1. Permitir pesquisar usuários por nome de exibição.
2. Restringir os resultados aos campos públicos e permitidos.
3. Permitir enviar solicitações de amizade.
4. Permitir aceitar ou recusar solicitações recebidas.
5. Permitir cancelar solicitações enviadas.
6. Permitir consultar a lista de amigos.
7. Permitir remover um amigo.
8. Permitir consultar solicitações pendentes.
9. Impedir solicitações duplicadas.
10. Impedir que um usuário envie solicitações de amizade para si mesmo.
11. Permitir consultar atividades compartilhadas por amigos.
12. Exibir atividades como filmes assistidos ou avaliações publicadas somente quando autorizadas.
13. Respeitar as configurações de privacidade de cada usuário.
14. Permitir acessar o perfil de um amigo respeitando as informações compartilháveis.
15. Tratar contas bloqueadas ou relações removidas, caso o bloqueio seja implementado nesta etapa.
16. Exibir estados vazios quando o usuário ainda não possuir amigos ou atividades disponíveis.
17. Permitir paginação da lista de amigos e do feed de atividades.
18. Garantir que a remoção de uma amizade não exclua o histórico de nenhum dos usuários.
19. Preparar os eventos necessários para o módulo de Notificações.
20. Evitar exibir atividades duplicadas no feed.

## Requisitos Não Funcionais

- Aplicar autorização no backend.
- Proteger dados pessoais.
- Evitar enumeração excessiva de usuários.
- Aplicar limites de requisições em pesquisas e solicitações de amizade.
- Garantir consistência das relações entre usuários.
- Evitar consultas N+1 no feed.
- Respeitar as regras de privacidade em todas as consultas.
- Não presumir que uma amizade concede acesso irrestrito ao histórico do outro usuário.

## Você Deve

- Criar um modelo de relacionamento entre usuários.
- Definir estados válidos para solicitações de amizade.
- Implementar regras para evitar relações duplicadas.
- Validar todas as operações no servidor.
- Consultar os módulos de Perfil, Tracking e Avaliações para compor atividades.
- Implementar paginação.
- Criar testes de autorização e isolamento de dados.
- Definir como a privacidade é aplicada a cada tipo de atividade.
- Documentar as regras de transição de estado das amizades.

## Você Não Deve

- Tornar o histórico público automaticamente.
- Permitir que um usuário altere a lista de amigos de terceiros.
- Expor e-mails ou outros identificadores privados em pesquisas públicas.
- Criar um sistema de mensagens privadas nesta etapa.
- Implementar seguidores públicos sem decisão explícita de produto.
- Permitir acesso a dados privados apenas porque existe uma amizade.
- Criar notificações completas neste módulo; ele deve fornecer os eventos necessários ao módulo próprio.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- xUnit

## Critérios de aceitação

- O usuário consegue enviar, aceitar, recusar e cancelar solicitações.
- A lista de amigos é exibida corretamente.
- Não existem solicitações ou amizades duplicadas.
- O usuário não consegue alterar relações de terceiros.
- O feed apresenta somente atividades autorizadas.
- A remoção de uma amizade não exclui os dados dos envolvidos.
- Listas e feeds possuem paginação.
- Os testes de autorização e consistência passam.

## Resultado

Módulo social implementado, com amizades, solicitações e compartilhamento controlado de atividades.

## Decisões implementadas na versão inicial

- A busca não diferencia maiúsculas/minúsculas, exige ao menos dois caracteres e procura nomes que começam pelo termo informado. Ela retorna somente nome e ID Reelix de perfis públicos ou que habilitaram descoberta; e-mail, ID Supabase, biografia e contagens não são retornados.
- Uma relação canônica por par impede pedidos duplicados. Pedidos inversos enquanto existe uma solicitação pendente retornam conflito e exigem resposta explícita do destinatário. Solicitações recusadas, canceladas ou amizades removidas podem ser iniciadas novamente, respeitando o limite de escrita da API.
- Perfil público não ativa compartilhamento social automaticamente. O histórico e as avaliações só entram no feed entre amigos quando o respectivo consentimento está ativo; a consulta usa a data de ativação para não importar atividades antigas. Desativar a opção remove imediatamente as atividades das consultas seguintes.
- Favoritos de amigos só aparecem com consentimento próprio, com limite de 100 itens. Biografia só aparece no perfil contextual se o perfil também for público. Histórico, comentários privados, watchlist e listas privadas nunca são expostos pelo vínculo de amizade.
- O feed usa cursor composto por data/ID e lote fixo de 20 atividades. Amigos e solicitações também usam cursor; os cartões recebem dados de catálogo em lote, sem uma chamada por atividade.
- Eventos `friend.requested` e `friend.accepted` são gravados na mesma transação que a mudança de relação, com ID, versão, ator, destinatário e horário. A etapa Social não entrega notificações nem push.
- Busca e mutações de amizade têm limites por usuário. A tabela de relações e a tabela de eventos ficam com RLS habilitado e sem privilégios para os papéis da Data API; o backend usa sua conexão PostgreSQL.
- Bloqueio, seguidores públicos, chat e histórico retroativo não fazem parte desta entrega.

Os endpoints autenticados estão em `/api/v1/social`: `GET /users`, `POST /friend-requests`, `GET /friend-requests`, ações `PUT .../{id}/accept|decline`, cancelamento `DELETE /friend-requests/{id}`, `GET /friends`, perfil contextual `GET /friends/{accountId}/profile`, remoção `DELETE /friends/{relationshipId}` e `GET /feed`. A migration EF Core precisa ser aplicada ao banco antes de usar os endpoints.

## Estado atual · 02/11/2026

API, persistência/modelo, cliente mobile e telas de busca, solicitações, amizades,
perfil contextual e feed estão presentes no código. A migration deve ser aplicada e
validada em cada PostgreSQL alvo; a presença de endpoints não significa que o schema
do ambiente já foi atualizado. Busca/feed respeitam autorização e consentimentos.
Chat, bloqueio e seguidores públicos continuam fora do escopo.

## Redesign mobile · 03/11/2026

- A tela Amigos abre no feed cronológico; Feed, Solicitações, Amigos, Descobrir e Atividades são destinos exclusivos de navegação compacta por ícones.
- O cabeçalho oferece atalhos para adicionar e pesquisar pessoas. Busca por nome mantém os estados do relacionamento e as ações existentes; não há sugestões artificiais.
- Solicitações separa recebidas e enviadas. O indicador conta recebidas carregadas, com `+` quando existe outra página, sem afirmar um total que a API não retorna.
- O feed usa lista virtualizada, paginação por cursor, atualização por gesto e acesso ao perfil e aos detalhes de filmes/séries. Cartões visíveis consultam o catálogo com cache por título para nome e pôster reais. Episódios não possuem metadados de série suficientes nesse contrato e não geram um link presumido.
- Atividades apresenta o mesmo feed autorizado em formato compacto; filtros de assistidos e avaliações operam sobre as páginas carregadas, com opção de carregar mais.
- Carregamento, vazio e nova tentativa são distintos; erros permanecem no toast. Remoção de amizade usa confirmação no modal compartilhado, inclusive na versão web.
- O contrato atual não fornece nota, texto de comentário, avatar do autor ou conquistas no feed. Curtidas, comentários em publicações e salvamento de posts não foram simulados. O avatar do feed usa iniciais; o nome e o pôster vêm do catálogo quando disponíveis.
- Nenhuma regra de autorização, consentimento ou persistência foi alterada.

## Perfil social contextual · 03/11/2026

- O perfil usa cabeçalho com avatar/iniciais, nome, biografia permitida, estado de amizade e quatro abas: Visão geral, Atividades, Favoritos e Conquistas. Não é uma página pública anônima: os endpoints continuam autenticados.
- `GET /api/v1/social/friends/{accountId}/profile` acrescenta `relationshipStatus`, `relationshipId`, `requestSentByMe`, `isSelf`, `favoritesShared`, `watchHistoryShared` e `ratingsShared`. A identidade básica pode ser consultada pelo próprio titular, por participantes de uma relação aceita/pendente ou quando o perfil é público/descobrível. Biografia continua restrita a perfil público; descoberta não concede acesso a ela.
- Favoritos exigem amizade aceita e consentimento próprio, com limite existente de 100. A aplicação também elimina favoritos e flags sociais quando não existe amizade aceita. Os tipos de conteúdo são normalizados para `movie`/`series` na resposta.
- `GET /api/v1/social/friends/{accountId}/activities?cursor=...` filtra o autor no servidor, preservando cursor, amizade aceita e consentimentos com data de ativação. Um autor indisponível retorna uma coleção vazia, sem confirmar existência de conta.
- Foi corrigido o feed de avaliações, inclusive o feed geral: além do consentimento social, `Audience` deve ser `Public`. Avaliações privadas não podem gerar atividades visíveis. Comentários não entram nesta consulta.
- Adicionar, cancelar, aceitar, recusar e remover usam as mutações existentes. A tela invalida as consultas sociais após a operação; remoção limpa imediatamente o conteúdo compartilhado em cache. Solicitações duplicadas continuam bloqueadas pelas regras do domínio.
- A grade usa `TitlePosterCard` e dados reais do catálogo, filtros de filmes/séries e carregamento incremental em lotes de 12. Visão geral limita favoritos a quatro e atividades a duas. A lista de atividades é virtualizada e usa o mesmo `SocialActivityCard` de Amigos.
- Medalhas públicas usam `AchievementMedal`; somente a interação revela nome, descrição/critério textual e data no `ModalDialog`. Não são exibidos progresso ou conquistas privadas.
- Indicadores mostram apenas a quantidade disponível de favoritos explicitamente compartilhados e medalhas públicas; o limite de favoritos não representa uma contagem irrestrita do banco. Username, número de amigos, filmes assistidos e séries acompanhadas não são inferidos.
- Avatar personalizado não é fornecido por esse backend; o perfil usa iniciais. Notas em estrelas, comentários, números de temporada/episódio, início/fim de série e conquistas como eventos do feed também não estão disponíveis no contrato social. Não são simulados.
- Não houve alteração de schema ou nova migration. Aplicação/backend precisam ser atualizados junto com o cliente porque o perfil acrescenta campos de contrato.

Validação detalhada, limitações e classificação de cada requisito estão no
[relatório do redesign do perfil social](0009-social.md).

---

## Anexo — Relatório do redesign do perfil social de amigos

> Migrado de `docs/implementation-report-social-profile.md` em 2026-11-19, sem alteração de conteúdo além dos links.


Data: 03/11/2026. A implementação é do perfil contextual autenticado, não de uma página acessível anonimamente na internet. Nenhum dado de demonstração foi incluído no app.

### Arquivos e Componentes

- Tela: [mobile/src/app/social-profile/[accountId].tsx](../mobile/src/app/social-profile/%5BaccountId%5D.tsx).
- Integração com Amigos: [mobile/src/app/social.tsx](../mobile/src/app/social.tsx).
- Cartão reutilizável de atividade: [mobile/src/components/social/social-activity-card.tsx](../mobile/src/components/social/social-activity-card.tsx).
- Cliente e validação de contratos: [mobile/src/services/api-client.ts](../mobile/src/services/api-client.ts).
- Contratos: [backend/Reelix.Application/Social/SocialContracts.cs](../backend/Reelix.Application/Social/SocialContracts.cs).
- Aplicação: [backend/Reelix.Application/Social/SocialService.cs](../backend/Reelix.Application/Social/SocialService.cs).
- Consultas: [backend/Reelix.Infrastructure/Persistence/Social/SocialRepository.cs](../backend/Reelix.Infrastructure/Persistence/Social/SocialRepository.cs).
- Endpoints: [backend/Reelix.Api/Social/SocialEndpoints.cs](../backend/Reelix.Api/Social/SocialEndpoints.cs).
- Testes: [SocialServiceTests.cs](../backend/Reelix.UnitTests/Social/SocialServiceTests.cs), [SocialPersistenceModelTests.cs](../backend/Reelix.UnitTests/Social/SocialPersistenceModelTests.cs) e [AuthenticationEndpointTests.cs](../backend/Reelix.UnitTests/Api/AuthenticationEndpointTests.cs).
- Documentação: [especificação social](0009-social.md) e [wireframe](../.docs/design/wireframes/reelix-visual-wireframes.html), agora com entrada própria para o perfil.

Reutilizados sem alteração: `TitlePosterCard`, `AchievementMedal`, `ModalDialog`, `PrimaryButton`, `BackButton`, tokens, ícones da marca e toast. Não foram adicionadas dependências ou migrations.

### Endpoints

| Operação | Endpoint | Alteração |
| --- | --- | --- |
| Perfil contextual | `GET /api/v1/social/friends/{accountId}/profile` | Acrescenta estado da relação, direção do pedido, identificação de perfil próprio e flags de compartilhamento. Permite identidade básica pública/descobrível ou em relação aceita/pendente. |
| Atividades da pessoa | `GET /api/v1/social/friends/{accountId}/activities?cursor=...` | Novo; filtro de autor no servidor, mesmas regras de amizade/consentimento do feed. |
| Feed de Amigos | `GET /api/v1/social/feed` | Também passa a excluir avaliações de audiência privada. |
| Adicionar | `POST /api/v1/social/friend-requests` | Existente, reutilizado. |
| Aceitar/recusar | `PUT /api/v1/social/friend-requests/{id}/accept` ou `/decline` | Existentes, reutilizados. |
| Cancelar pedido | `DELETE /api/v1/social/friend-requests/{id}` | Existente, reutilizado. |
| Remover amizade | `DELETE /api/v1/social/friends/{relationshipId}` | Existente, reutilizado com confirmação. |
| Medalhas | `GET /api/v1/gamification/users/{accountId}/badges` | Existente, sem exposição de progresso privado. |
| Metadados e pôsteres | `GET /api/v1/catalog/{provider}/{contentType}/{externalId}?language=pt-BR` | Existente, cache compartilhado por título. |

### Requisitos Individuais

#### 1. Cabeçalho do Perfil

**Concluído com as informações disponíveis.**

- Problema anterior: avatar, nome e descrição sem hierarquia social ou interação.
- Solução e resultado: avatar maior com contorno da marca, identidade, biografia permitida, ação de amizade e indicadores de favoritos compartilhados e medalhas públicas. Iniciais são fallback real, não um avatar inventado.
- Arquivos/componentes: tela, cliente, contratos, serviço e repositório; tokens e ícones existentes.
- Endpoints: perfil contextual e medalhas públicas.
- Testes: perfil real com amizade aceita e seis favoritos; verificações visuais em 320, 390 e 1440 px, sem overflow horizontal.
- Limite: não há username ou avatar personalizado nesse contrato. Contagens de amigos, filmes assistidos e séries acompanhadas não foram criadas nem estimadas.

#### 2. Estado de Relacionamento

**Implementado, mas não validado integralmente com contas reais.**

- Problema anterior: ausência de estado e ações no perfil; somente amigos aceitos conseguiam abri-lo.
- Solução e resultado: estado fornecido pelo servidor; adicionar, solicitação enviada/cancelar, aceitar/recusar e Amigos/gerenciar. Remoção exige modal e limpa o conteúdo social em cache. Controles ficam desabilitados durante operações e atualização do perfil; conflitos provocam nova consulta.
- Arquivos/componentes: tela, cliente, serviço, contratos, repositório, endpoints e navegação em Amigos. `ModalDialog` e `PrimaryButton` reutilizados.
- Endpoints: perfil e todas as mutações de amizade da tabela acima; regras de duplicidade permanecem no domínio.
- Testes: estado aceito e abertura/cancelamento do menu conferidos na conta real; testes de domínio de amizade e autorização executados. Não foram enviadas, aceitas, recusadas ou removidas relações reais apenas para testar a interface.

#### 3. Navegação do Perfil

**Concluído.**

- Problema anterior: coleções completas empilhadas em uma única página.
- Solução e resultado: Visão geral, Atividades, Favoritos e Conquistas com ícones, nomes acessíveis e sublinhado de seleção. Visão geral limita medalhas e favoritos a quatro e atividades a duas.
- Arquivos/componentes: tela; os dados continuam nos mesmos hooks TanStack Query, não em estado global duplicado.
- Endpoints: perfil, atividades por pessoa e medalhas.
- Testes: alternância das quatro abas na web, incluindo filtros internos e estados selecionados visualmente; áreas de toque medidas acima de 44 px em 320 px.

#### 4. Atividades Recentes

**Parcialmente concluído.**

- Problema anterior: perfil não tinha atividades; copiar a apresentação de Amigos criaria duas implementações divergentes.
- Solução e resultado: `SocialActivityCard` extraído e usado nas duas telas; perfil consulta feed cronológico por autor, com cursor, atualização por gesto, estado vazio, carregamento, toast e nova tentativa. Filmes e séries usam metadados/pôsteres reais e abrem detalhes.
- Arquivos/componentes: cartão compartilhado, tela, Amigos, cliente, contratos, serviço, repositório e endpoint novo de atividades.
- Endpoints: atividades por pessoa e catálogo; feed geral reutiliza o cartão e a consulta de autorização.
- Testes: resposta real com uma atividade e somente o autor solicitado; outro autor indisponível retornou lista vazia. Testes verificam resolução do ID do chamador, filtro de autor e SQL com amizade/consentimentos/audiência pública.
- Limite: o contrato não fornece valor da nota, comentário, temporada/episódio ou série-pai do episódio. Não existem eventos de início/fim de série, crítica ou conquista no feed. Esses itens não foram inferidos de contagens ou páginas incompletas.

#### 5. Favoritos Compartilhados

**Concluído.**

- Problema anterior: links textuais repetidos, sem reconhecimento visual dos gostos.
- Solução e resultado: grade com `TitlePosterCard`, pôster 2:3, título real, ano quando disponível e indicação de filme/série. Filtros Todos/Filmes/Séries; lotes locais de 12 evitam carregar simultaneamente todos os metadados. O limite do contrato permanece 100.
- Arquivos/componentes: tela e cliente; cartão existente e cache do catálogo reutilizados.
- Endpoints: perfil contextual fornece somente referências autorizadas; catálogo resolve os metadados.
- Testes: seis favoritos reais, três filmes e três séries após os carregamentos; abertura de detalhes e retorno ao perfil. Um título sem pôster usa o fallback existente, sem imagem fictícia.

#### 6. Conquistas

**Concluído para o contrato público existente.**

- Problema anterior: nome, descrição e data permanentemente expostos em uma lista extensa.
- Solução e resultado: grade de medalhas da marca; nomes permanecem nos rótulos acessíveis, não como texto visual. Modal revela nome, descrição usada como critério textual e data de desbloqueio. Não consulta dashboard ou progresso privado de terceiros.
- Arquivos/componentes: tela; `AchievementMedal` e `ModalDialog` reutilizados sem duplicação.
- Endpoint: medalhas públicas existente; `isPublic=false` tem mensagem própria, sem contagem presumida.
- Testes: três medalhas reais; texto visual vazio no botão da medalha; modal aberto e conteúdo de critério/data conferido; fechamento testado.
- Limite: não existe campo separado para regra/limiar nesse DTO público; o critério exibido é a descrição fornecida pelo backend, sem percentual de progresso.

#### 7. Identidade Visual

**Concluído.**

- Problema anterior: hierarquia semelhante a uma listagem administrativa.
- Solução e resultado: fundo e superfícies existentes, Sora, acento vermelho/rosa, contorno de avatar, ícones do kit, separação por abas e pôsteres reais. Nenhum tema de terceiros ou biblioteca web foi incorporado ao React Native.
- Arquivos/componentes: tela e cartão social compartilhado; tokens e assets Reelix existentes.
- Endpoints: nenhuma alteração específica de identidade visual.
- Testes: typecheck, lint e capturas do app na web em mobile/desktop.

#### 8. Experiência Mobile

**Parcialmente concluído: web responsiva validada; builds nativos pendentes.**

- Problema anterior: scroll longo, links repetidos e pouca organização por tarefa.
- Solução e resultado: cabeçalho compacto, quatro destinos, áreas de toque, grid flexível, lista virtualizada para atividades, carregamentos independentes e recuperação de erros via toast. `BackButton` permanece compartilhado; no link direto, retorna a Amigos quando não há histórico de navegação.
- Arquivos/componentes: tela, cartão compartilhado e componentes existentes.
- Endpoints: os mesmos do perfil; paginação das atividades no servidor e lotes locais de favoritos são mecanismos distintos.
- Testes: 320×740, 390×844 e 1440×1000 sem overflow horizontal; abas medidas em aproximadamente 64×80 px no menor viewport; retorno para Amigos validado.
- Pendente: aparelhos e builds iOS/Android, leitor de tela nativo e comportamento de teclado/gestos nesses ambientes.

#### 9. Privacidade e Regras de Negócio

**Implementado, mas não validado integralmente com múltiplas contas em banco real.**

- Problema identificado: além da necessidade de separar identidade básica de conteúdo entre amigos, o feed anterior considerava consentimento geral de avaliações sem filtrar `UserRating.Audience`.
- Solução e resultado: biografia somente com perfil público; descoberta só permite identidade básica. Favoritos e atividades continuam exigindo amizade aceita e consentimentos específicos. Avaliações privadas foram excluídas do feed geral e individual. Comentários, listas privadas e estatísticas pessoais não são consultados por essa tela. Medalhas respeitam a resposta pública.
- Arquivos/componentes: contratos, serviço, repositório, endpoints, cliente e tela.
- Endpoints: perfil/atividades/feed; todos autenticados. Perfil inacessível continua indisponível; feed inacessível retorna vazio sem confirmar existência da conta.
- Testes: defesa da aplicação para estados sem amizade/sem consentimento; comandos SQL reais gerados pelo repositório interceptados sem banco para verificar filtros; rejeição anônima dos endpoints; autor indisponível testado na API real.
- Pendente: matriz manual completa de consentimentos, audiência e estados de duas ou mais contas. Os testes SQL verificam os filtros, não equivalem a essa matriz com dados persistidos.

#### 10. Diretrizes Técnicas

**Concluído no escopo implementado.**

- Problema anterior: componentes úteis já existiam, mas o perfil não os utilizava para favoritos ou detalhes de medalhas.
- Solução e resultado: reutilização de componentes, serviços e tokens; novo cartão compartilhado; autorização em consultas e aplicação; UI em React state e dados remotos em TanStack Query. Sem mocks no app, dependências adicionais, acesso direto mobile ao banco ou migrations.
- Arquivos/componentes: todos os arquivos listados no início; limites dos DTOs documentados abaixo.
- Endpoints: extensões aditivas do módulo Social; sem mudar autenticação ou ampliar CORS nesta entrega.
- Testes: 37 verificações focadas no backend; TypeScript e Expo lint executados. Avisos NuGet `NU1900` relativos a um feed inacessível não impediram a compilação/testes.

#### 11. Critérios de Aceitação

Os números a seguir correspondem à lista de aceitação do pedido, não às seções anteriores.

| Critério | Classificação | Evidência ou ressalva |
| --- | --- | --- |
| 1. Avatar, nome, biografia e informações sociais | Concluído | Perfil real e indicadores autorizados; sem username ou estatísticas inventadas. |
| 2. Estado de amizade correto | Implementado, mas não validado | Estado aceito validado; demais estados integrados, sem ciclo manual completo. |
| 3. Enviar/gerenciar solicitações | Implementado, mas não validado | Endpoints reais, controles pendentes, invalidação e regras de domínio; sem modificar relações reais para teste. |
| 4. Abas/ícones | Concluído | Quatro abas operacionais na web. |
| 5. Atividades no padrão do feed | Parcialmente concluído | Componente e endpoint reais; metadados de episódio/nota/comentário e outros eventos ausentes. |
| 6. Favoritos com pôsteres | Concluído | Grade, filtros 3/3 e navegação testados. |
| 7. Conquistas como medalhas | Concluído | Três medalhas públicas reais. |
| 8. Nomes após interação | Concluído | Sem nomes visuais nos botões; modal testado. |
| 9. Privacidade | Implementado, mas não validado | Cobertura automatizada e consulta real; matriz manual com múltiplas contas pendente. |
| 10. Componentes e padrões Reelix | Concluído | Reutilização, tokens, toast e lint. |
| 11. Voltar compartilhado | Concluído | `BackButton` reutilizado e fallback para Amigos testado. |
| 12. Diferentes dimensões | Parcialmente concluído | Três viewports web validados; builds nativos pendentes. |

#### 12. Relatório Final

**Concluído.** Este documento registra problemas, causas, soluções, componentes, endpoints, resultados, testes e classificações individualmente. Especificação e wireframe foram atualizados na mesma entrega.

### Verificações Executadas

```powershell
# Na raiz, ou ajustando o caminho a partir de mobile:
dotnet test backend/Reelix.UnitTests/Reelix.UnitTests.csproj --filter "FullyQualifiedName~Social|FullyQualifiedName~FriendRelationship|FullyQualifiedName~UserEndpoints_RejectAnonymousRequests" --artifacts-path "$env:TEMP\reelix-validation" --no-restore

# Em mobile:
npx tsc --noEmit
npx expo lint
```

Resultado backend: **37 aprovados, zero falhas**. A suíte completa da solução não foi executada. As verificações Playwright utilizaram dados reais e não alteraram amizades ou preferências para produzir cenários artificiais. O wireframe foi aberto e a entrada de perfil/grade foi validada no navegador.

### Dados Ausentes e Alterações Necessárias

- **Username e avatar personalizado:** exigem suporte persistido e contrato público próprio, com regras de edição e exposição; não usar e-mail ou IDs internos como username.
- **Contagens de amigos, filmes assistidos e séries acompanhadas:** exigem consentimentos próprios e agregação no servidor. Não contar itens de um feed paginado para apresentar totais.
- **Nota, temporada/episódio e série-pai:** exigem enriquecer o DTO de atividades em lote, preservando audiência pública e os consentimentos. Não resolver um episódio como se fosse um filme.
- **Comentários e críticas no feed:** exigem eventos e filtragem por audiência, moderação e spoilers, além de uma decisão explícita de produto. Conteúdo privado não deve ser reaproveitado como descrição.
- **Início/finalização de série e conquista como evento social:** exigem fatos/eventos reais e política própria de compartilhamento. A interface não os deduz de progresso, medalhas públicas ou estatísticas.
- **Critério estruturado da conquista:** caso seja necessário exibir regra e limiar separadamente da descrição, acrescentar apenas os metadados públicos da definição ao DTO de medalhas, sem progresso individual.

Essas extensões não foram implementadas nem simuladas.

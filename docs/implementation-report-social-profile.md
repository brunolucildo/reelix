# Relatório: Redesign do Perfil Social

Data: 03/11/2026. A implementação é do perfil contextual autenticado, não de uma página acessível anonimamente na internet. Nenhum dado de demonstração foi incluído no app.

## Arquivos e Componentes

- Tela: [mobile/src/app/social-profile/[accountId].tsx](../mobile/src/app/social-profile/%5BaccountId%5D.tsx).
- Integração com Amigos: [mobile/src/app/social.tsx](../mobile/src/app/social.tsx).
- Cartão reutilizável de atividade: [mobile/src/components/social/social-activity-card.tsx](../mobile/src/components/social/social-activity-card.tsx).
- Cliente e validação de contratos: [mobile/src/services/api-client.ts](../mobile/src/services/api-client.ts).
- Contratos: [backend/Reelix.Application/Social/SocialContracts.cs](../backend/Reelix.Application/Social/SocialContracts.cs).
- Aplicação: [backend/Reelix.Application/Social/SocialService.cs](../backend/Reelix.Application/Social/SocialService.cs).
- Consultas: [backend/Reelix.Infrastructure/Persistence/Social/SocialRepository.cs](../backend/Reelix.Infrastructure/Persistence/Social/SocialRepository.cs).
- Endpoints: [backend/Reelix.Api/Social/SocialEndpoints.cs](../backend/Reelix.Api/Social/SocialEndpoints.cs).
- Testes: [SocialServiceTests.cs](../backend/Reelix.UnitTests/Social/SocialServiceTests.cs), [SocialPersistenceModelTests.cs](../backend/Reelix.UnitTests/Social/SocialPersistenceModelTests.cs) e [AuthenticationEndpointTests.cs](../backend/Reelix.UnitTests/Api/AuthenticationEndpointTests.cs).
- Documentação: [especificação social](specs/09-social.md) e [wireframe](wireframes/reelix-visual-wireframes.html), agora com entrada própria para o perfil.

Reutilizados sem alteração: `TitlePosterCard`, `AchievementMedal`, `ModalDialog`, `PrimaryButton`, `BackButton`, tokens, ícones da marca e toast. Não foram adicionadas dependências ou migrations.

## Endpoints

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

## Requisitos Individuais

### 1. Cabeçalho do Perfil

**Concluído com as informações disponíveis.**

- Problema anterior: avatar, nome e descrição sem hierarquia social ou interação.
- Solução e resultado: avatar maior com contorno da marca, identidade, biografia permitida, ação de amizade e indicadores de favoritos compartilhados e medalhas públicas. Iniciais são fallback real, não um avatar inventado.
- Arquivos/componentes: tela, cliente, contratos, serviço e repositório; tokens e ícones existentes.
- Endpoints: perfil contextual e medalhas públicas.
- Testes: perfil real com amizade aceita e seis favoritos; verificações visuais em 320, 390 e 1440 px, sem overflow horizontal.
- Limite: não há username ou avatar personalizado nesse contrato. Contagens de amigos, filmes assistidos e séries acompanhadas não foram criadas nem estimadas.

### 2. Estado de Relacionamento

**Implementado, mas não validado integralmente com contas reais.**

- Problema anterior: ausência de estado e ações no perfil; somente amigos aceitos conseguiam abri-lo.
- Solução e resultado: estado fornecido pelo servidor; adicionar, solicitação enviada/cancelar, aceitar/recusar e Amigos/gerenciar. Remoção exige modal e limpa o conteúdo social em cache. Controles ficam desabilitados durante operações e atualização do perfil; conflitos provocam nova consulta.
- Arquivos/componentes: tela, cliente, serviço, contratos, repositório, endpoints e navegação em Amigos. `ModalDialog` e `PrimaryButton` reutilizados.
- Endpoints: perfil e todas as mutações de amizade da tabela acima; regras de duplicidade permanecem no domínio.
- Testes: estado aceito e abertura/cancelamento do menu conferidos na conta real; testes de domínio de amizade e autorização executados. Não foram enviadas, aceitas, recusadas ou removidas relações reais apenas para testar a interface.

### 3. Navegação do Perfil

**Concluído.**

- Problema anterior: coleções completas empilhadas em uma única página.
- Solução e resultado: Visão geral, Atividades, Favoritos e Conquistas com ícones, nomes acessíveis e sublinhado de seleção. Visão geral limita medalhas e favoritos a quatro e atividades a duas.
- Arquivos/componentes: tela; os dados continuam nos mesmos hooks TanStack Query, não em estado global duplicado.
- Endpoints: perfil, atividades por pessoa e medalhas.
- Testes: alternância das quatro abas na web, incluindo filtros internos e estados selecionados visualmente; áreas de toque medidas acima de 44 px em 320 px.

### 4. Atividades Recentes

**Parcialmente concluído.**

- Problema anterior: perfil não tinha atividades; copiar a apresentação de Amigos criaria duas implementações divergentes.
- Solução e resultado: `SocialActivityCard` extraído e usado nas duas telas; perfil consulta feed cronológico por autor, com cursor, atualização por gesto, estado vazio, carregamento, toast e nova tentativa. Filmes e séries usam metadados/pôsteres reais e abrem detalhes.
- Arquivos/componentes: cartão compartilhado, tela, Amigos, cliente, contratos, serviço, repositório e endpoint novo de atividades.
- Endpoints: atividades por pessoa e catálogo; feed geral reutiliza o cartão e a consulta de autorização.
- Testes: resposta real com uma atividade e somente o autor solicitado; outro autor indisponível retornou lista vazia. Testes verificam resolução do ID do chamador, filtro de autor e SQL com amizade/consentimentos/audiência pública.
- Limite: o contrato não fornece valor da nota, comentário, temporada/episódio ou série-pai do episódio. Não existem eventos de início/fim de série, crítica ou conquista no feed. Esses itens não foram inferidos de contagens ou páginas incompletas.

### 5. Favoritos Compartilhados

**Concluído.**

- Problema anterior: links textuais repetidos, sem reconhecimento visual dos gostos.
- Solução e resultado: grade com `TitlePosterCard`, pôster 2:3, título real, ano quando disponível e indicação de filme/série. Filtros Todos/Filmes/Séries; lotes locais de 12 evitam carregar simultaneamente todos os metadados. O limite do contrato permanece 100.
- Arquivos/componentes: tela e cliente; cartão existente e cache do catálogo reutilizados.
- Endpoints: perfil contextual fornece somente referências autorizadas; catálogo resolve os metadados.
- Testes: seis favoritos reais, três filmes e três séries após os carregamentos; abertura de detalhes e retorno ao perfil. Um título sem pôster usa o fallback existente, sem imagem fictícia.

### 6. Conquistas

**Concluído para o contrato público existente.**

- Problema anterior: nome, descrição e data permanentemente expostos em uma lista extensa.
- Solução e resultado: grade de medalhas da marca; nomes permanecem nos rótulos acessíveis, não como texto visual. Modal revela nome, descrição usada como critério textual e data de desbloqueio. Não consulta dashboard ou progresso privado de terceiros.
- Arquivos/componentes: tela; `AchievementMedal` e `ModalDialog` reutilizados sem duplicação.
- Endpoint: medalhas públicas existente; `isPublic=false` tem mensagem própria, sem contagem presumida.
- Testes: três medalhas reais; texto visual vazio no botão da medalha; modal aberto e conteúdo de critério/data conferido; fechamento testado.
- Limite: não existe campo separado para regra/limiar nesse DTO público; o critério exibido é a descrição fornecida pelo backend, sem percentual de progresso.

### 7. Identidade Visual

**Concluído.**

- Problema anterior: hierarquia semelhante a uma listagem administrativa.
- Solução e resultado: fundo e superfícies existentes, Sora, acento vermelho/rosa, contorno de avatar, ícones do kit, separação por abas e pôsteres reais. Nenhum tema de terceiros ou biblioteca web foi incorporado ao React Native.
- Arquivos/componentes: tela e cartão social compartilhado; tokens e assets Reelix existentes.
- Endpoints: nenhuma alteração específica de identidade visual.
- Testes: typecheck, lint e capturas do app na web em mobile/desktop.

### 8. Experiência Mobile

**Parcialmente concluído: web responsiva validada; builds nativos pendentes.**

- Problema anterior: scroll longo, links repetidos e pouca organização por tarefa.
- Solução e resultado: cabeçalho compacto, quatro destinos, áreas de toque, grid flexível, lista virtualizada para atividades, carregamentos independentes e recuperação de erros via toast. `BackButton` permanece compartilhado; no link direto, retorna a Amigos quando não há histórico de navegação.
- Arquivos/componentes: tela, cartão compartilhado e componentes existentes.
- Endpoints: os mesmos do perfil; paginação das atividades no servidor e lotes locais de favoritos são mecanismos distintos.
- Testes: 320×740, 390×844 e 1440×1000 sem overflow horizontal; abas medidas em aproximadamente 64×80 px no menor viewport; retorno para Amigos validado.
- Pendente: aparelhos e builds iOS/Android, leitor de tela nativo e comportamento de teclado/gestos nesses ambientes.

### 9. Privacidade e Regras de Negócio

**Implementado, mas não validado integralmente com múltiplas contas em banco real.**

- Problema identificado: além da necessidade de separar identidade básica de conteúdo entre amigos, o feed anterior considerava consentimento geral de avaliações sem filtrar `UserRating.Audience`.
- Solução e resultado: biografia somente com perfil público; descoberta só permite identidade básica. Favoritos e atividades continuam exigindo amizade aceita e consentimentos específicos. Avaliações privadas foram excluídas do feed geral e individual. Comentários, listas privadas e estatísticas pessoais não são consultados por essa tela. Medalhas respeitam a resposta pública.
- Arquivos/componentes: contratos, serviço, repositório, endpoints, cliente e tela.
- Endpoints: perfil/atividades/feed; todos autenticados. Perfil inacessível continua indisponível; feed inacessível retorna vazio sem confirmar existência da conta.
- Testes: defesa da aplicação para estados sem amizade/sem consentimento; comandos SQL reais gerados pelo repositório interceptados sem banco para verificar filtros; rejeição anônima dos endpoints; autor indisponível testado na API real.
- Pendente: matriz manual completa de consentimentos, audiência e estados de duas ou mais contas. Os testes SQL verificam os filtros, não equivalem a essa matriz com dados persistidos.

### 10. Diretrizes Técnicas

**Concluído no escopo implementado.**

- Problema anterior: componentes úteis já existiam, mas o perfil não os utilizava para favoritos ou detalhes de medalhas.
- Solução e resultado: reutilização de componentes, serviços e tokens; novo cartão compartilhado; autorização em consultas e aplicação; UI em React state e dados remotos em TanStack Query. Sem mocks no app, dependências adicionais, acesso direto mobile ao banco ou migrations.
- Arquivos/componentes: todos os arquivos listados no início; limites dos DTOs documentados abaixo.
- Endpoints: extensões aditivas do módulo Social; sem mudar autenticação ou ampliar CORS nesta entrega.
- Testes: 37 verificações focadas no backend; TypeScript e Expo lint executados. Avisos NuGet `NU1900` relativos a um feed inacessível não impediram a compilação/testes.

### 11. Critérios de Aceitação

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

### 12. Relatório Final

**Concluído.** Este documento registra problemas, causas, soluções, componentes, endpoints, resultados, testes e classificações individualmente. Especificação e wireframe foram atualizados na mesma entrega.

## Verificações Executadas

```powershell
# Na raiz, ou ajustando o caminho a partir de mobile:
dotnet test backend/Reelix.UnitTests/Reelix.UnitTests.csproj --filter "FullyQualifiedName~Social|FullyQualifiedName~FriendRelationship|FullyQualifiedName~UserEndpoints_RejectAnonymousRequests" --artifacts-path "$env:TEMP\reelix-validation" --no-restore

# Em mobile:
npx tsc --noEmit
npx expo lint
```

Resultado backend: **37 aprovados, zero falhas**. A suíte completa da solução não foi executada. As verificações Playwright utilizaram dados reais e não alteraram amizades ou preferências para produzir cenários artificiais. O wireframe foi aberto e a entrada de perfil/grade foi validada no navegador.

## Dados Ausentes e Alterações Necessárias

- **Username e avatar personalizado:** exigem suporte persistido e contrato público próprio, com regras de edição e exposição; não usar e-mail ou IDs internos como username.
- **Contagens de amigos, filmes assistidos e séries acompanhadas:** exigem consentimentos próprios e agregação no servidor. Não contar itens de um feed paginado para apresentar totais.
- **Nota, temporada/episódio e série-pai:** exigem enriquecer o DTO de atividades em lote, preservando audiência pública e os consentimentos. Não resolver um episódio como se fosse um filme.
- **Comentários e críticas no feed:** exigem eventos e filtragem por audiência, moderação e spoilers, além de uma decisão explícita de produto. Conteúdo privado não deve ser reaproveitado como descrição.
- **Início/finalização de série e conquista como evento social:** exigem fatos/eventos reais e política própria de compartilhamento. A interface não os deduz de progresso, medalhas públicas ou estatísticas.
- **Critério estruturado da conquista:** caso seja necessário exibir regra e limiar separadamente da descrição, acrescentar apenas os metadados públicos da definição ao DTO de medalhas, sem progresso individual.

Essas extensões não foram implementadas nem simuladas.

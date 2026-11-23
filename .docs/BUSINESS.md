# Negócio

> **Público:** qualquer pessoa que precise entender o produto antes de decidir ou codar.
> **Verificado contra o código em:** 2026-11-19. Descreve intenção e vocabulário; o que está efetivamente implementado e validado está em `ARCHITECTURE.md` §2 e `QA.md`.

## 1. Problema

Quem acompanha filmes e séries espalha o histórico por memória, planilhas, notas e plataformas de streaming: não sabe ao certo qual episódio parou, o que quer ver depois, nem consegue compartilhar impressões com amigos sem tomar ou dar spoiler. Descobrir o que assistir também depende de recomendações genéricas, sem relação com o que a pessoa já viu e gosta.

## 2. Proposta de valor

Um aplicativo mobile onde a pessoa **descobre** títulos reais de um catálogo licenciado, **registra** o que assistiu (filmes e episódios, com progresso por série), **organiza** em favoritos, watchlist e listas privadas, **avalia e comenta** com proteção contra spoilers e **compartilha** apenas o que autorizar com amigos. O diferencial não é um catálogo próprio, e sim um histórico confiável e privado por padrão, com controle explícito do que cada amigo pode ver.

## 3. Conceitos do domínio

| Termo                  | Definição                                                                                                                                                                         |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Conta**              | Registro interno Reelix (`user_accounts`) ligado ao usuário do Supabase pelo `sub` do JWT. Todo dado pessoal pertence a uma conta; o dono nunca vem do cliente.                  |
| **Título**             | Filme ou série do catálogo. Tem ID interno Reelix estável e uma referência externa (`provider`, `external_id`, `content_type`).                                                  |
| **Temporada / Episódio** | Partes de uma série, identificadas por ID interno; o número do episódio pode ser nulo e a temporada 0 representa especiais.                                                    |
| **Catálogo**           | Metadados vindos de TMDB (principal) ou OMDb (opcional), consultados sob demanda; o Reelix guarda só as referências, não uma cópia dos metadados.                                |
| **Tracking**           | Registro de que a conta assistiu a um filme ou episódio, com data editável. Fonte dos dados de progresso, Home, perfil, conquistas e notificações. Nunca é inferido pela data de lançamento. |
| **Progresso da série** | Episódios assistidos × episódios disponíveis (lançados, segundo o inventário do provedor). Desconhecido quando o provedor não informa o inventário.                              |
| **Favoritos / Watchlist (“Quero assistir”) / Lista** | Coleções privadas da conta; uma lista é nomeada, tem descrição e títulos ordenáveis.                                                              |
| **Avaliação**          | Nota de 0 a 5 em passos de meia estrela para título ou episódio.                                                                                                                  |
| **Comentário**         | Texto sobre um título/episódio, com audiência privada ou pública e marcação de spoiler; o conteúdo com spoiler fica oculto até a revelação explícita.                            |
| **Amizade**            | Relação bilateral criada por solicitação aceita. Habilita o compartilhamento **autorizado** de atividade.                                                                        |
| **Consentimento social** | Três permissões independentes: compartilhar histórico, avaliações e favoritos com amigos. Tudo começa desligado.                                                              |
| **Feed**               | Atividades de amigos (apenas o que o amigo consentiu), paginadas.                                                                                                                 |
| **Notificação / Inbox** | Aviso persistido para a conta (hoje, eventos sociais). Push remoto é um canal opcional, desligado por padrão.                                                                    |
| **Conquista (badge)**  | Medalha determinística concedida por regras sobre o uso real (tracking, social etc.). O ranking mensal entre amigos é opt-in.                                                    |
| **Onboarding**         | Fluxo retomável de 5 etapas (boas-vindas, gêneros, filmes favoritos, séries favoritas, revisão) que alimenta as sugestões da Home.                                              |

## 4. Escopo

### 4.1 Dentro do escopo (implementado)

- Cadastro, login, confirmação de e-mail, recuperação de senha e logout; sessão restaurada com segurança.
- Onboarding retomável com gêneros TMDB e títulos favoritos; edição posterior das preferências.
- Perfil com nome, biografia, privacidade e avatar fixo (ou iniciais); perfil público de outra conta só se ela o tornou público.
- Pesquisa e detalhes de filmes e séries, temporadas e episódios; Home separada por filmes e séries (destaques, favoritos, sugestões por gênero, próximos lançamentos, continuar assistindo, calendário de episódios).
- Marcar/desmarcar filmes e episódios, corrigir data, histórico paginado e progresso por temporada e por série.
- Favoritos, watchlist e listas privadas (criar, editar, excluir, ordenar).
- Avaliações e comentários com audiência e proteção contra spoilers; "Minhas avaliações".
- Amizades, busca controlada de pessoas, feed e perfil social contextual, sempre sob consentimento.
- Inbox de notificações sociais, preferências e registro privado de dispositivos.
- Conquistas, estatísticas e ranking mensal opt-in entre amigos.
- Builds Android de development (EAS) para validação em dispositivo.

### 4.2 Fora do escopo (não aprovado ou não implementado)

- Widgets Android (aprovado, **não implementado**); widgets iOS e builds iOS (adiados).
- Push remoto habilitado e alertas de novos episódios/estreias (dependem de credenciais, hosting e de uma fonte confiável de datas).
- Recomendações por IA; análise automática de spoilers por IA (existe, **desativada**).
- Posts autorais, curtidas e comentários em posts; denúncias e moderação administrativa.
- Upload de avatar; exclusão e exportação de conta (decisão de privacidade pendente).
- Streaming, reprodução ou disponibilidade por plataforma; qualquer cópia integral de metadados ou imagens do catálogo.
- Recursos web/admin; monetização.

## 5. Regras de negócio transversais

1. **Privado por padrão.** Perfil, favoritos e histórico começam privados; o compartilhamento exige consentimento explícito e revogável.
2. **O cliente nunca define o dono.** Toda operação usa a conta resolvida do JWT; recurso de outra conta responde como inexistente quando privado.
3. **Dados reais apenas.** Não se inventam títulos, contagens, datas de estreia, estatísticas ou módulos; quando não há dado, mostra-se estado vazio ou "desconhecido".
4. **Tracking é ação da pessoa.** Nenhum episódio é marcado como assistido automaticamente nem por data de lançamento.
5. **Spoilers ocultos até a revelação.** Conteúdo marcado como spoiler só aparece após ação explícita de quem lê.
6. **Atribuição do catálogo.** O app exibe o logo e o aviso do TMDB; o uso comercial depende de confirmação com o provedor (REELIX-018).
7. **Copy em pt-BR** e sem revelar informação sensível (por exemplo, se um e-mail tem conta).

## 6. Roadmap

Ordem recomendada desde a auditoria de 2026-11-04 (não é compromisso de prazo). As fases 1 e 2 estão concluídas para o escopo atual (`Development`).

| Fase | Objetivo                                              | Itens                                                  | Estado                  |
| ---- | ----------------------------------------------------- | ------------------------------------------------------ | ----------------------- |
| 1    | Confiabilidade do schema e da autorização             | REELIX-001, 002                                        | Concluída               |
| 2    | Quality gates reproduzíveis (CI, supply chain)        | REELIX-004 (003 e 019 fora ou bloqueados)              | Concluída               |
| 3    | Validar distribuição e operação móvel                 | REELIX-005, 006, 015                                   | Em andamento (Android)  |
| 4    | Decisões de privacidade, conteúdo e provedores        | REELIX-008, 009, 010, 018                              | Aguardando decisão      |
| 5    | Recursos opcionais e expansão                         | REELIX-007, 011, 012, 013, 016, 017                    | Aguardando decisão      |
| 6    | **Fundação de engenharia** (specs `0013` em diante)   | Refatorar a arquitetura (Clean Architecture + SOLID) → refatorar o design visual → camada de testes (Playwright e demais) | Specs `0013`–`0023` criadas; implementação não iniciada |

A fase 6 é a prioridade atual: ela reduz o custo das fases 3 a 5 (um código testável, com camadas claras e um design system) antes de ampliar funcionalidades. Os itens de cada fase estão detalhados em `ARCHITECTURE.md` §6.3; o mapa e a ordem das specs da fase 6 estão em `.plans/arquitetura-design-testes-breakdown.md`.

## 7. Decisões abertas de produto

Não são defeitos; são decisões que bloqueiam trabalho e precisam de dono:

| Decisão                                                                    | Impacto                                                  | Bloqueante para            |
| -------------------------------------------------------------------------- | -------------------------------------------------------- | -------------------------- |
| Política de exclusão, exportação e retenção de dados da conta (LGPD)       | Contrato de dados, social, conteúdo e provedores          | Lançamento público         |
| Uso comercial de TMDB/OMDb e atribuição                                    | Distribuição e eventual licença                           | Lançamento comercial       |
| Moderação: denúncias, revisão humana, papéis administrativos               | Publicação pública de comentários                         | Conteúdo público em escala |
| Reativar a análise de spoilers por IA (custo, consentimento, precisão)     | Experiência de comentários e custo                        | —                          |
| Recomendações por IA                                                       | Orçamento, privacidade e explicabilidade                  | —                          |
| Posts e reações no Social                                                  | Escopo, moderação e persistência                          | REELIX-010                 |
| Upload de avatar ou manter avatares fixos                                  | Storage, moderação e privacidade                          | —                          |
| Hosting da API/workers, observabilidade e estratégia de migrations por ambiente | Push, alertas e operação contínua                    | Push e alertas             |
| Plataformas nativas oficialmente suportadas e IDs de distribuição          | Builds de release e lojas                                 | Distribuição               |

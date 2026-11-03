# Reelix — status do projeto

**Fotografia do repositório em 03/11/2026.** Este documento descreve o código e a
documentação encontrados; não atesta disponibilidade em produção. A presença de uma
tela, endpoint, teste de modelo ou migration não comprova integração ponta a ponta nem
implantação em um ambiente.

## Visão geral

Reelix é um aplicativo para descobrir filmes e séries, acompanhar o que foi assistido,
organizar títulos e compartilhar atividades autorizadas com amigos. O repositório
contém um app React Native/Expo e uma API ASP.NET Core em uma solução modular, com
PostgreSQL como persistência.

| Área | Implementação encontrada |
|---|---|
| Mobile | React Native, Expo SDK 58, TypeScript estrito, Expo Router, TanStack Query, React Hook Form, Zod e `StyleSheet`. |
| API | ASP.NET Core / .NET 10, endpoints HTTP em `backend/Reelix.Api`. |
| Camadas | `Reelix.Domain`, `Reelix.Application`, `Reelix.Infrastructure` e `Reelix.Api`; monólito modular, não microserviços. |
| Persistência | PostgreSQL, EF Core e Npgsql; migrations versionadas em `Reelix.Infrastructure`. |
| Identidade | Supabase Auth no app; a API valida JWT e resolve a conta Reelix. Não há ASP.NET Identity. |
| Catálogo | TMDB como provedor principal e OMDb opcional, chamados pelo backend. Referências externas são persistidas; não há cópia integral de metadados. |
| Serviços condicionais | Expo Push no servidor; análise de spoilers via OpenAI configurável, mas temporariamente desativada por padrão. |
| Qualidade | xUnit no backend e workflow GitHub Actions versionado para testes backend, lint/type-check mobile e auditoria de dependências. Testes automatizados mobile continuam ausentes. |

O desenho geral é consistente com uma arquitetura de camadas e monólito modular:
endpoints entregam HTTP, Application coordena casos de uso, Domain contém regras e
Infrastructure implementa persistência e integrações. Os projetos separam essas
responsabilidades, mas os módulos de produto ainda compartilham projetos e um contexto
EF; não há um projeto isolado por módulo. Isso não é, por si só, uma falha arquitetural.

## Estado dos módulos

**“Parcialmente concluído” significa que existe um vertical slice no código, mas falta
validação relevante ou alguma capacidade documentada.** A coluna de validação impede
que implementação seja confundida com operação verificada.

| Módulo | Estado | Implementação encontrada | Validação e limites conhecidos |
|---|---|---|---|
| Autenticação e conta | Parcialmente concluído | Cadastro, login, confirmação e recuperação de senha, callbacks/deep links, sessão e logout no mobile; validação JWT Supabase e conta interna na API. | Testes de autenticação existem, mas não há validação E2E contra um projeto Supabase neste levantamento. Exclusão/exportação da conta não está definida. |
| Onboarding | Parcialmente concluído | Fluxo retomável, gêneros TMDB, seleção de títulos, gravação de rascunho/conclusão e edição de preferências. | Requer credenciais e migrations do ambiente; testes de integração contra PostgreSQL real não foram confirmados. |
| Perfil e privacidade | Parcialmente concluído | Perfil próprio, biografia, nome, preferências de privacidade, avatares locais fixos/iniciais e perfis sociais contextuais. | Upload de avatar não existe. Retenção, exportação e exclusão da conta não têm fluxo implementado. |
| Catálogo | Parcialmente concluído | Pesquisa, detalhes, gêneros, temporadas e episódios via adaptadores TMDB/OMDb; IDs externos persistidos e cache temporário em memória para algumas consultas. | Requer credenciais, disponibilidade e atribuição compatíveis com o uso. Não há fallback automático, snapshot integral ou verificação live dos provedores nesta auditoria. |
| Home e descoberta | Parcialmente concluído | Home separada por filme/série, descoberta, pesquisa, detalhes, calendário de episódios/estreias e filtros determinísticos por preferências; usa dados do catálogo. | Recomendações por IA não estão implementadas. Datas dependem dos metadados disponíveis; comportamento em dispositivos não foi validado manualmente. |
| Tracking | Parcialmente concluído | Histórico paginado, marcar/desmarcar filmes e episódios, editar horário e calcular progresso de temporadas/séries a partir do inventário disponível. | A cobertura depende do inventário do provedor. Não há suíte ampla de integração HTTP/PostgreSQL que valide o fluxo completo. |
| Avaliações e comentários | Parcialmente concluído | Notas de meia estrela, comentários, edição/exclusão próprias, audiência privada e ocultação/revelação de spoilers, inclusive para episódios. | Classificação automática via OpenAI está desativada; denúncias e moderação administrativa não estão no escopo implementado. |
| Favoritos, watchlist e listas | Parcialmente concluído | Favoritos, watchlist e listas privadas com edição, exclusão, ordenação e paginação no código/API. | Migrations precisam estar sincronizadas com cada banco; não há verificação completa de persistência real ou testes mobile automatizados. |
| Social e amizades | Parcialmente concluído | Busca controlada, solicitações, amizades, feed paginado e perfis contextuais com consentimentos aplicados. | Migration/contratos precisam estar sincronizados por ambiente. Feed não oferece publicações autorais, curtidas ou comentários em posts; tais recursos não foram aprovados nas specs atuais. |
| Notificações | Parcialmente concluído | Inbox, preferências, registro privado de dispositivos e processamento de eventos sociais/tickets/receipts no backend. | Push remoto está desligado por padrão e depende de projeto/credenciais/build/hosting. Alertas de catálogo não têm fonte e agendamento implementados. |
| Gamificação | Parcialmente concluído | Conquistas determinísticas, estatísticas e ranking entre amigos no backend e mobile. | A migration deve ser aplicada por ambiente. Reconciliação ocorre ao consultar o dashboard; metadados de duração/gêneros têm cobertura incompleta. |
| IA | Parcialmente concluído | Adaptador de classificação de spoiler existe no servidor. | Está desativado por padrão; não há recomendações por IA. Reativação, custo, política de dados e necessidade de revisão dependem de decisão/configuração. |
| Widgets mobile | Pendente | Há pesquisa/spike documentado, sem widget funcional. | Sem IDs finais de distribuição e sem build/validação em Xcode ou Android SDK. |
| Testes mobile | Pendente | Scripts de lint e type-check existem. | Não foi encontrado runner/suíte de testes mobile. |
| CI/CD e release | Parcialmente implementado | `.github/workflows/ci.yml` executa testes backend, lint/type-check mobile, audits NuGet/npm e publica script idempotente de migrations para revisão. | Workflow ainda não foi executado no GitHub; branch protection e secret scanning precisam ser habilitados. Hosting, ambientes e release nativo não foram inspecionados. |

## O que já existe no código

- API com rotas autenticadas para autenticação básica, catálogo, onboarding, perfil,
  tracking, listas, avaliações, social, notificações e gamificação.
- Validação JWT com emissor, audiência, validade e assinatura; rotas de desenvolvimento
  para Swagger/autenticação auxiliar são mapeadas somente em Development.
- Endpoints usam identidade extraída do `sub` validado; operações de usuário aplicam
  regras de propriedade/visibilidade na camada de aplicação.
- Persistência EF Core com migrations e regras de unicidade/referência para relações
  de usuário e dados de catálogo.
- Clientes mobile usam API autenticada, React Query e validação de payloads. Existem
  estados de carregamento/erro/vazio e componentes/tokens compartilhados.
- Limites de requisição são configurados para escritas de avaliações, busca/escritas
  sociais e escritas de notificações; não se deve inferir proteção uniforme de todas
  as rotas a partir dessas políticas seletivas.
- Workers de moderação de comentários, notificações sociais e entrega de notificações
  são registrados na API.

## Lacunas, riscos e débitos confirmados

1. **Validação operacional do banco:** o script idempotente das 10 migrations foi
   gerado/revisado sem conexão; sua aplicação varia por ambiente e não foi verificada.
   Social, notificações e gamificação ainda dependem de aplicação autorizada.
2. **Cobertura de integração/autorização:** 47 testes focados exercitam rejeição anônima
   em rotas HTTP; 10 testes Testcontainers aplicam as migrations num PostgreSQL efêmero
    e cobrem isolamento/consentimento em perfil, tracking, listas, avaliações, social,
    notificações e gamificação. Ainda não há testes mobile nem cobertura de integração de
    todos os endpoints e cenários concorrentes.
3. **Automação de qualidade:** workflow versionado executa os gates, gera artefato de
  migrations e inclui Gitleaks. A primeira execução remota no GitHub e branch protection
  ainda não foram verificadas. O audit npm encontrou 8 vulnerabilidades moderadas
  transitivas no Expo (`glob` e `minimatch`); correção forçada rebaixaria o SDK e não foi
  aplicada. O audit NuGet via nuget.org não encontrou vulnerabilidades.
4. **Entrega externa:** credenciais/projetos TMDB, OMDb, Expo e OpenAI, hosting dos
   workers e configuração de build não foram verificados em ambiente operacional.
5. **Privacidade de ciclo de vida:** exclusão, exportação e retenção de dados da conta
   permanecem sem fluxo confirmado; são necessárias decisões de produto e operação.
6. **Observabilidade:** não foi identificado setup de métricas distribuídas, tracing,
   monitoramento de crashes ou alertas operacionais. Os logs existentes não equivalem
   a uma estratégia de observabilidade validada.
7. **UX/acessibilidade:** o retry de inicialização e os itens da inbox receberam
   melhorias semânticas; ainda não houve testes com leitor de tela, contraste, Dynamic
   Type ou validação multiplataforma. A inspeção do navegador local não foi possível
   porque a URL compartilhada recusou conexão.
8. **Integrações sociais e catálogo:** publicação/reação em posts, busca de pessoas com
   escopo adicional, alertas de lançamento e recomendação por IA não devem ser tratados
   como recursos implementados; as specs atuais limitam ou não aprovam esses escopos.

O backlog registra evidências, ações, dependências e critérios de aceitação em
[DEVELOPMENT_BACKLOG.md](DEVELOPMENT_BACKLOG.md). Decisões que não são requisitos
aprovados estão identificadas como “Aguardando decisão”, e não como falhas.

## Integrações e dependências externas

| Serviço | Uso | Estado conhecido | Limite |
|---|---|---|---|
| Supabase Auth | Login, sessão e emissão de JWT | Integrado no código. | Chaves públicas, configuração de callback, JWT e projeto de cada ambiente são externos. |
| PostgreSQL gerenciado | Fonte de dados da aplicação | EF Core/Npgsql e migrations no repositório. | O projeto de desenvolvimento conferido tem as 10 migrations esperadas e RLS nas 24 tabelas da aplicação; outros ambientes não foram verificados. |
| TMDB | Catálogo principal, gêneros, temporadas e episódios | Adaptador implementado. | Credencial de servidor, atribuição, limites e direitos de uso devem ser confirmados para o ambiente e finalidade. |
| OMDb | Fonte opcional de catálogo | Adaptador implementado; sem fallback automático. | Credencial opcional e campos/imagens sujeitos às condições do provedor. |
| Expo Push | Entrega remota opcional | Código de registro e envio implementado, desativado por padrão. | Requer IDs/projeto EAS, APNs/FCM, segredo de servidor e hosting contínuo. |
| OpenAI | Análise opcional de spoilers | Integração no servidor, temporariamente desativada. | Reativar exige decisão, segredo server-side, controle de custo, consentimento e avaliação de falhas. |

Nenhuma credencial ou valor de segredo é reproduzido aqui.

## Validações e limites desta auditoria

- A suíte `backend/Reelix.UnitTests/Reelix.UnitTests.csproj` passou: **202 testes
  aprovados, 0 falhos, 0 ignorados**, incluindo 10 cenários HTTP com PostgreSQL efêmero.
  O ambiente reportou SDK .NET `11.0.100-rc` para projeto `net10.0`; a CI fixa .NET 10.
- `npx expo lint` e `npx tsc --noEmit` passaram. `npm audit --omit=dev --audit-level=high`
  reportou 8 vulnerabilidades moderadas e não falha nesse limiar; o downgrade forçado
  sugerido pelo npm não foi aplicado. O audit NuGet via `nuget.org` não encontrou pacotes
  vulneráveis.
- Gitleaks v8.30.1 verificou o histórico local (75 commits), sem findings. A CI agora
  executa o scanner com saída redigida; a primeira execução no GitHub ainda está pendente.
- O banco Supabase de desenvolvimento foi consultado para confirmar migrations, RLS e
  auditar marcadores de testes iniciais. Seis contas internas sem vínculo em `auth.users`
  e seus registros identificados por marcadores foram removidos em transação. Outros
  registros sem marcador não foram alterados. Nenhuma credencial ou token foi exibido.
- Não foi possível confirmar builds/instalações iOS e Android, push real, migrations
  aplicadas em cada ambiente, disponibilidade de workers, performance sob carga ou
  UX visual no navegador compartilhado (`localhost:8087` recusou conexão).
- A auditoria não faz avaliação jurídica de licenças/obrigações e não declara
  conformidade regulatória.

## Documentação de continuidade

- [Backlog técnico rastreável](DEVELOPMENT_BACKLOG.md)
- [Roadmap recomendado](ROADMAP.md)
- [Arquitetura e padrões](docs/architecture.md)
- [Especificações por módulo](docs/specs/)
- [Configuração local](docs/authentication-setup.md)

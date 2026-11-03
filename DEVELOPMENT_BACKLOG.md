# Reelix — backlog da auditoria

Pendências identificadas pela leitura do código, specs e documentação em 03/11/2026 e
atualizadas após as alterações de follow-up.
“Aguardando decisão” marca uma lacuna de escopo/infraestrutura ainda não aprovada;
“Bloqueado” indica dependência externa necessária antes de concluir. Prioridades são
relativas à prontidão e aos riscos visíveis no repositório, não estimativas de prazo.

**Escopo:** 17 pendências abertas, além de 2 itens resolvidos nesta etapa. As exceções
solicitadas — testes mobile (REELIX-003), IA (REELIX-011 e REELIX-012) e Widgets
(REELIX-013) — permanecem registradas sem implementação.

**Distribuição das abertas:** 5 Alta, 7 Média e 5 Baixa. Nenhuma foi classificada como
Crítica com a evidência disponível. Estados: 0 Pendente, 4 Parcialmente implementados,
5 Bloqueados e 8 Aguardando decisão.

---

### REELIX-001

**Título:** Validar e aplicar migrations por ambiente
**Módulo:** Persistência / todos
**Categoria:** Technical Debt
**Prioridade:** Alta
**Status:** Parcialmente implementado

**Descrição:** Estabelecer um processo controlado para comparar o histórico de migrations
com cada banco de destino e validar a atualização do schema em PostgreSQL real.

**Estado atual:** Migrations EF Core estão versionadas. Um script idempotente para as
10 migrations foi gerado e revisado sem conexão ao banco; a documentação e a CI agora
permitem revisar esse artefato antes da execução. Nenhuma migration foi aplicada a
ambiente compartilhado ou produção. Social, notificações e gamificação ainda dependem
de aplicação e verificação por ambiente.

**Evidências:** [ReelixDbContext.cs](backend/Reelix.Infrastructure/Persistence/ReelixDbContext.cs),
[README.md](README.md), [10-notifications.md](docs/specs/10-notifications.md) e
[11-gamification.md](docs/specs/11-gamification.md).

**Impacto:** Divergência entre modelo e schema pode impedir operações, causar falhas
em runtime ou deixar módulos indisponíveis após release.

**Solução proposta:** Definir a matriz de ambientes, revisar o script idempotente gerado,
executar migrations em PostgreSQL real autorizado e registrar resultado por ambiente.
Não aplicar em banco compartilhado ou produção sem autorização e processo de rollback.

**Plano técnico de implementação:**
1. Inventariar ambientes autorizados, conexão, versão do schema e responsável.
2. Revisar o artefato gerado localmente ou pela CI.
3. Aplicar primeiro em um PostgreSQL descartável/staging e verificar constraints,
   índices e dados existentes.
4. Aplicar ao ambiente aprovado pelo processo de release, registrando migration final.
5. Executar smoke tests de persistência e endpoints dos módulos alterados.
6. Atualizar status operacional sem armazenar connection strings ou segredos.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Infrastructure/Persistence/Migrations/`,
`backend/Reelix.Infrastructure/Persistence/ReelixDbContext.cs`,
`docs/authentication-setup.md`, `docs/specs/`.
**Dependências:** Acesso autorizado aos bancos, credenciais seguras e processo de release.
**Critérios de aceitação:** Cada ambiente tem migration final registrada; aplicação
inicial e atualização passam em PostgreSQL real; endpoints dos módulos persistem e leem
dados sem erro; nenhum segredo é versionado.
**Estimativa de complexidade:** Média — envolve vários ambientes e validação de schema,
mas usa migrations EF Core já existentes.

---

### REELIX-002

**Título:** Ampliar testes de integração e autorização da API
**Módulo:** API / segurança / persistência
**Categoria:** Testing
**Prioridade:** Alta
**Status:** Concluído

**Descrição:** Cobrir fluxos HTTP, autorização por recurso e isolamento entre contas
com banco PostgreSQL real, além dos testes unitários/modelo existentes.

**Estado atual:** A matriz HTTP verifica rejeição anônima nas rotas críticas e inclui
10 testes Testcontainers que aplicam as migrations em PostgreSQL efêmero. Os cenários
cobrem isolamento entre contas em perfil, listas, avaliações, tracking, inbox e preferências
de notificações, badges/ranking, pedidos de amizade e feed com consentimento; também verificam
tentativas por terceiros e gravações concorrentes idempotentes. Testes do provider cobrem
indisponibilidade de catálogo e resposta sanitizada. A API usa diretamente o container, sem
conectar ao Supabase. O job backend da CI está configurado para executar a suíte; a execução
local passou com 202 testes. A workflow do GitHub ainda precisa da primeira execução remota.

**Evidências:** [Reelix.UnitTests.csproj](backend/Reelix.UnitTests/Reelix.UnitTests.csproj),
[Program.cs](backend/Reelix.Api/Program.cs) e endpoints sob
[backend/Reelix.Api](backend/Reelix.Api/).

**Impacto:** Testes de regras isoladas não demonstram que middleware, contratos, banco
e autorização funcionam em conjunto nem que um usuário não lê/escreve dados de outro.

**Solução proposta:** Criar testes de integração HTTP em host de teste e, quando CI
permitir, PostgreSQL efêmero. Priorizar recursos privados, migrations e falhas
autenticadas/não autenticadas.

**Plano técnico de implementação:**
1. Ampliar fixtures de API e estratégias seguras para cenários autenticados.
2. Executar migrations em PostgreSQL de teste (considerar Testcontainers se o ambiente
   de CI suportar Docker).
3. Cobrir 401/400/404/409, ownership e isolamento para tracking, listas, ratings,
   social, notificações e gamificação.
4. Testar privacidade/consentimentos, duplicação, concorrência e comportamento de
   provedores indisponíveis.
5. Tornar as verificações reproduzíveis localmente e no pipeline.

**Arquivos provavelmente envolvidos:** `backend/Reelix.UnitTests/`,
`backend/Reelix.Api/Program.cs`, endpoints de `backend/Reelix.Api/` e configurações
de persistência em `backend/Reelix.Infrastructure/Persistence/`.
**Dependências:** PostgreSQL descartável ou estratégia equivalente de integração.
**Critérios de aceitação:** Atendidos para os fluxos críticos cobertos: requests HTTP reais,
schema/migrations em PostgreSQL, isolamento entre contas, respostas negativas de autorização,
concorrência/idempotência e execução automatizada documentada.
**Estimativa de complexidade:** Alta — vários módulos e contratos, além de infraestrutura
de banco efêmero.

---

### REELIX-003

**Título:** Criar testes automatizados do app mobile
**Módulo:** Mobile / qualidade
**Categoria:** Testing
**Prioridade:** Alta
**Status:** Parcialmente implementado

**Descrição:** Adicionar cobertura automatizada para fluxos móveis críticos sem
confundir lint e type-check com testes de comportamento.

**Estado atual:** `mobile/package.json` declara scripts de start, plataforma, lint e
geração de assets; não declara script ou runner de testes. Não foram encontrados testes
mobile nesta auditoria.

**Evidências:** [package.json](mobile/package.json), [src/app](mobile/src/app/) e
[mobile/README.md](mobile/README.md).

**Impacto:** Mudanças em auth, navegação, mutations, privacidade e estados de erro podem
regredir sem feedback automatizado.

**Solução proposta:** Escolher ferramenta compatível com Expo SDK atual e testar
componentes/hooks/serviços nos fluxos de maior risco; manter E2E de dispositivo como
etapa posterior se houver infraestrutura.

**Plano técnico de implementação:**
1. Definir runner compatível com a versão Expo/React Native instalada.
2. Criar helpers para sessão, API e estado de consulta sem chamadas reais a serviços.
3. Cobrir login/reset, onboarding retomável, tracking, listas, reviews, social e logout.
4. Verificar estados de carregamento, vazio, falha, retry e sessão expirada.
5. Incluir testes no script npm e no pipeline depois de REELIX-004.

**Arquivos provavelmente envolvidos:** `mobile/package.json`, `mobile/src/`, nova pasta
de testes mobile (ainda inexistente; caminho a definir).
**Dependências:** Escolha/compatibilidade do runner com Expo SDK 58.
**Critérios de aceitação:** Os principais fluxos e erros têm testes repetíveis; os testes
rodam por um comando documentado e não dependem de credenciais de produção.
**Estimativa de complexidade:** Alta — diversos fluxos e dependências nativas exigem
seleção e configuração cuidadosa da ferramenta.

---

### REELIX-004

**Título:** Automatizar quality gates e verificações de supply chain
**Módulo:** CI/CD / segurança
**Categoria:** Technical Debt
**Prioridade:** Alta
**Status:** Parcialmente implementado

**Descrição:** Criar uma pipeline versionada para build, testes, lint/type-check e
verificações de dependências/segredos.

**Estado atual:** `.github/workflows/ci.yml` agora executa testes backend, lint e
type-check mobile, gera o script idempotente de migrations e audita NuGet/npm. O workflow
agora inclui Gitleaks CLI com imagem pinada por digest e actions pinadas por SHA em runtime
Node 24. O Run #3 em `main` concluiu com sucesso: os jobs backend e mobile passaram e foram
publicados os artefatos `backend-test-results` e `reelix-idempotent-migrations`. Esse run
antecede os ajustes locais atuais: restore NuGet agora trata NU1903/NU1904 como erros, e o
audit npm cobre dependências de desenvolvimento e runtime com limiar high. O restore passou;
o audit completo reportou 24 advisories (3 low, 21 moderate), sem high/critical, e passou no
limiar configurado. Localmente, a suíte Release passou com 202 testes mesmo com as
configurações externas Supabase/PostgreSQL vazias; lint/type-check mobile também passaram.
O audit NuGet via nuget.org não encontrou vulnerabilidades. Gitleaks percorreu os 77 commits
locais sem encontrar segredos. Branch protection ainda não está configurada; a página de
configurações do GitHub exigiu autenticação.

**Evidências:** [Reelix.sln](Reelix.sln), [Reelix.UnitTests.csproj](backend/Reelix.UnitTests/Reelix.UnitTests.csproj),
[package.json](mobile/package.json) e
[architecture.md](docs/architecture.md#13-testing-and-quality).

**Impacto:** A qualidade depende de execução manual e o estado de vulnerabilidades/
**Impacto:** Sem checks obrigatórios na branch, mudanças podem ser integradas mesmo quando
um job falha; os advisories low/moderate permanecem registrados até correção upstream.

**Solução proposta:** Configurar checks obrigatórios de branch e reavaliar os advisories npm
quando houver versão Expo compatível que resolva as vulnerabilidades sem downgrade.

**Plano técnico de implementação:**
1. Configurar `backend` e `mobile` como checks obrigatórios de branch.
2. Manter Gitleaks no job backend e validar o artefato TRX quando houver falha.
3. Corrigir vulnerabilidades npm quando uma versão compatível com o SDK estiver publicada.
4. Garantir que falha de feed NuGet seja tratada como resultado inconclusivo.
5. Manter revisão do artefato SQL manual; CI não o aplica.

**Arquivos provavelmente envolvidos:** [.github/workflows/ci.yml](.github/workflows/ci.yml),
`Reelix.sln`, `mobile/package.json`, `mobile/package-lock.json` e configurações de
proteção do repositório (fora do Git).
**Dependências:** Acesso autenticado às configurações do GitHub e release upstream do Expo
que corrija os advisories sem downgrade.
**Critérios de aceitação:** Workflow executa checks em PR; vulnerabilidades altas/críticas
fazem CI falhar; secret scanning está ativo; o risco moderado atual é rastreado sem
downgrade forçado de Expo.
**Estimativa de complexidade:** Média — workflow foi criado; proteções do repositório e
correção de dependências dependem da plataforma e do upstream.

---

### REELIX-005

**Título:** Validar builds e release nativos do app
**Módulo:** Mobile / release
**Categoria:** Testing
**Prioridade:** Média
**Status:** Bloqueado

**Descrição:** Confirmar builds de distribuição e os fluxos de autenticação/deep link
em iOS e Android, além do comportamento em dispositivos reais.

**Estado atual:** Há configuração Expo e módulos nativos usados pelo app, mas esta
auditoria não executou build/instalação em iOS ou Android. As docs de widgets registram
limitações de ambiente (Xcode/Android SDK) para validação nativa.

**Evidências:** [app.json](mobile/app.json), [authentication-setup.md](docs/authentication-setup.md)
e [12-mobile-widgets.md](docs/specs/12-mobile-widgets.md).

**Impacto:** Fluxos que funcionam no Expo/web ou em type-check podem falhar em builds
nativos, inclusive callback de autenticação, push registration e armazenamento seguro.

**Solução proposta:** Definir perfis de desenvolvimento/produção, identifiers e ambiente
de build; validar instalação e navegação em iOS/Android antes da distribuição.

**Plano técnico de implementação:**
1. Confirmar bundle identifier, package name, esquema `reelix://` e projeto EAS.
2. Gerar builds compatíveis de desenvolvimento e release.
3. Validar login, callback/reset, sessão SecureStore, logout e navegação por links.
4. Testar permissões/registro de push quando REELIX-006 estiver configurado.
5. Registrar versões/limites suportados e resultados por plataforma.

**Arquivos provavelmente envolvidos:** `mobile/app.json`, `mobile/src/app/_layout.tsx`,
configuração de autenticação e arquivos de build (a definir).
**Dependências:** IDs de distribuição, credenciais de assinatura e acesso a ferramentas/
dispositivos iOS e Android.
**Critérios de aceitação:** Build instalável nas plataformas declaradas; fluxos essenciais
e deep links passam em ambiente nativo; nenhum segredo de servidor é distribuído.
**Estimativa de complexidade:** Média — depende de configuração/credenciais externas e
validação de duas plataformas.

---

### REELIX-006

**Título:** Habilitar e validar push remoto em ambiente operacional
**Módulo:** Notificações
**Categoria:** Feature
**Prioridade:** Média
**Status:** Bloqueado

**Descrição:** Completar a configuração de ponta a ponta de push remoto para dispositivos
suportados, sem afetar a inbox quando push estiver indisponível.

**Estado atual:** Registro de dispositivo, processamento e integração Expo existem no
código; push remoto é desativado por padrão. A ativação depende de EAS, APNs/FCM,
credenciais de servidor e API/worker ativos continuamente.

**Evidências:** [10-notifications.md](docs/specs/10-notifications.md),
[NotificationDeliveryWorker.cs](backend/Reelix.Api/Notifications/NotificationDeliveryWorker.cs),
[app.json](mobile/app.json) e [notifications API](backend/Reelix.Api/Notifications/).

**Impacto:** Sem configuração, o usuário não recebe alertas push fora do app; falhas de
entrega não podem ser avaliadas em condições reais.

**Solução proposta:** Configurar ambiente de staging com credenciais secretas server-side,
validar tickets/receipts e invalidar tokens expirados, antes de habilitar produção.

**Plano técnico de implementação:**
1. Definir projeto EAS, identificadores e credenciais APNs/FCM.
2. Configurar variáveis/segredos no hosting e `ExpoPush__Enabled` apenas no ambiente
   alvo; nunca no app.
3. Garantir disponibilidade do worker e observabilidade mínima dos estados de entrega.
4. Testar permissão negada, token inválido, retry, receipt e desregistro no logout.
5. Habilitar progressivamente, com rollback por configuração.

**Arquivos provavelmente envolvidos:** `mobile/app.json`, `mobile/src/services/push-registration.ts`,
`mobile/src/app/_layout.tsx`, `backend/Reelix.Api/Notifications/` e configurações
externas de EAS/hosting (não versionar segredos).
**Dependências:** REELIX-001 e REELIX-005, projeto EAS, APNs/FCM e hosting contínuo.
**Critérios de aceitação:** Dispositivo autorizado recebe evento de teste em build nativo;
preferências são respeitadas; receipts/erros são persistidos; tokens não aparecem em
respostas públicas ou logs.
**Estimativa de complexidade:** Alta — envolve backend, credenciais de plataforma,
dispositivos e operação contínua.

---

### REELIX-007

**Título:** Definir fonte e agendamento para alertas de catálogo
**Módulo:** Notificações / catálogo
**Categoria:** Feature
**Prioridade:** Média
**Status:** Bloqueado

**Descrição:** Implementar alertas confiáveis de novos episódios e estreias somente após
definir como obter e atualizar datas confirmadas.

**Estado atual:** A inbox e eventos sociais existem, mas as specs afirmam que alertas
de episódio/estreia não consultam alterações de catálogo e não devem gerar dados
fictícios. Não há fonte/scheduler durável aprovada.

**Evidências:** [10-notifications.md](docs/specs/10-notifications.md),
[catalog-setup.md](docs/catalog-setup.md) e [Catalog](backend/Reelix.Infrastructure/Catalog/).

**Impacto:** Uma data desatualizada ou não confirmada pode produzir notificação enganosa;
sem integração, a categoria de alertas de catálogo não entrega valor.

**Solução proposta:** Validar disponibilidade e termos de uma fonte de atualização,
definir política de sincronização/idempotência e só então ligar eventos ao sistema atual.

**Plano técnico de implementação:**
1. Aprovar fonte, cobertura regional, frequência e validade dos dados.
2. Definir agendamento, cursor/versionamento, deduplicação e comportamento em alteração
   ou cancelamento de datas.
3. Implementar sincronização resiliente e persistência de eventos.
4. Filtrar títulos conforme tracking/watchlist, consentimentos e preferências.
5. Testar mudanças de data, dados ausentes, duplicação, retry e envio opt-out.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Infrastructure/Catalog/`,
`backend/Reelix.Api/Notifications/`, `docs/specs/10-notifications.md` e nova
configuração de scheduler (a definir).
**Dependências:** Decisão de produto/provedor e infraestrutura de agendamento; REELIX-006
para push remoto.
**Critérios de aceitação:** Cada alerta aponta para evento/fonte confirmados, é idempotente,
respeita preferências e nunca declara disponibilidade não fornecida pela fonte.
**Estimativa de complexidade:** Alta — sincronização e scheduling dependem de fornecedor
e infraestrutura ainda não definidos.

---

### REELIX-008

**Título:** Definir ciclo de vida, exportação e exclusão da conta
**Módulo:** Autenticação / perfil / privacidade
**Categoria:** Architecture
**Prioridade:** Alta
**Status:** Aguardando decisão

**Descrição:** Definir comportamento de retenção e oferecer fluxo de encerramento/
exportação de conta se aprovado pela política do produto.

**Estado atual:** Auth e perfil não oferecem fluxo confirmado de exclusão/exportação.
As diretrizes de arquitetura mencionam retenção, consentimento e LGPD, mas a spec de
perfil condiciona exclusão a política ainda não definida.

**Evidências:** [06-profile.md](docs/specs/06-profile.md),
[01-authentication.md](docs/specs/01-authentication.md) e
[architecture.md](docs/architecture.md#8-authentication-and-security).

**Impacto:** Sem decisão, não há contrato claro para dados de usuário, registros sociais,
conteúdo gerado e credenciais no encerramento da conta.

**Solução proposta:** Obter decisão de produto/privacidade sobre exclusão, exportação,
retenção, dados agregados e propagação a Supabase/provedores; implementar somente após
aprovação.

**Plano técnico de implementação:**
1. Inventariar dados por conta e dependências entre tabelas/serviços.
2. Aprovar política de retenção, exportação, exclusão, auditoria e período de recuperação.
3. Definir API autenticada, confirmação/reautenticação e UX de solicitação/resultado.
4. Implementar operação idempotente e tratamento de dependências externas.
5. Testar isolamento, reversibilidade/irreversibilidade e remoção de caches/dispositivos.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Api/Users/`,
`backend/Reelix.Application/Users/`, `backend/Reelix.Infrastructure/Persistence/`,
`mobile/src/app/settings.tsx` e docs de autenticação/perfil (ajustar após decisão).
**Dependências:** Decisão de produto e revisão de privacidade; requer REELIX-002 para
testes de isolamento.
**Critérios de aceitação:** Política aprovada e documentada; fluxo implementado e
testado sem expor dados de terceiros ou reter cópias indevidas.
**Estimativa de complexidade:** Alta — atravessa identidade, dados sociais, persistência
e serviços externos.

---

### REELIX-009

**Título:** Decidir se avatar enviado será suportado
**Módulo:** Perfil / UX
**Categoria:** UX/UI
**Prioridade:** Baixa
**Status:** Aguardando decisão

**Descrição:** Resolver o requisito legado de upload de avatar versus o conjunto atual
de avatares locais persistidos.

**Estado atual:** O app e API suportam chave de avatar fixo/iniciais; upload, substituição
e remoção de imagem personalizada não estão implementados nem há armazenamento conectado.

**Evidências:** [06-profile.md](docs/specs/06-profile.md),
[onboarding-profile-setup.md](docs/onboarding-profile-setup.md) e
[avatars/README.md](mobile/assets/brand/avatars/README.md).

**Impacto:** A especificação antiga e a experiência atual diferem; iniciar upload sem
decidir armazenamento, moderação, limites e privacidade criaria custo e superfície de risco.

**Solução proposta:** Aprovar explicitamente manter apenas avatares fixos ou adicionar
upload; atualizar a spec e o status de acordo com a decisão.

**Plano técnico de implementação:**
1. Validar necessidade de produto, formatos, limites, moderação e remoção.
2. Se não aprovado, atualizar requisitos para refletir o conjunto fixo atual.
3. Se aprovado, selecionar storage, autorização, lifecycle e varredura/validação de arquivo.
4. Implementar API e UI com fallback seguro, limpeza e testes.

**Arquivos provavelmente envolvidos:** `docs/specs/06-profile.md`,
`backend/Reelix.Api/Users/`, `backend/Reelix.Infrastructure/` e
`mobile/src/app/profile.tsx`; nova integração de storage somente se aprovada.
**Dependências:** Decisão de produto e fornecedor de storage caso upload seja aprovado.
**Critérios de aceitação:** Spec e UI concordam sobre o comportamento; se upload existir,
arquivos são validados, autorizados, removíveis e não expõem URLs/credenciais privadas.
**Estimativa de complexidade:** Baixa para formalizar avatares fixos; alta para implementar
upload e storage.

---

### REELIX-010

**Título:** Decidir escopo de denúncias e moderação administrativa
**Módulo:** Avaliações / segurança de conteúdo
**Categoria:** Feature
**Prioridade:** Média
**Status:** Aguardando decisão

**Descrição:** Definir se comentários precisam de denúncia, revisão humana, bloqueio,
recurso e ferramentas administrativas.

**Estado atual:** Avaliação/comentário e sinalização/revelação de spoilers existem.
Denúncias e revisão administrativa são explicitamente excluídas do escopo atual; não
foi encontrado painel/fluxo correspondente.

**Evidências:** [07-ratings-comments.md](docs/specs/07-ratings-comments.md),
[RatingsCommentsEndpoints.cs](backend/Reelix.Api/Ratings/RatingsCommentsEndpoints.cs).

**Impacto:** Se publicação pública for oferecida sem processo aprovado de abuso/moderação,
fica indefinido como tratar conteúdo denunciado ou bloqueado.

**Solução proposta:** Definir política e responsáveis antes de implementar denúncia,
moderação manual e estados de conteúdo. Não tratar a classificação de spoiler como
moderação geral.

**Plano técnico de implementação:**
1. Definir categorias denunciáveis, limiares, prazos e ação em conteúdo.
2. Aprovar papéis/permissões administrativas e trilha de auditoria.
3. Projetar estados e endpoints sem revelar denúncia a outros usuários.
4. Implementar fila/ferramenta operacional e controles de abuso.
5. Testar autorização, privacidade, apelação e consistência com comentários privados.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Api/Ratings/`,
`backend/Reelix.Application/Ratings/`, `backend/Reelix.Domain/Ratings/`,
`backend/Reelix.Infrastructure/Persistence/` e `mobile/src/components/reviews/`.
**Dependências:** Política de conteúdo, decisão de operação/moderadores e REELIX-002.
**Critérios de aceitação:** Política aprovada e fluxo verificável por papel autorizado;
denúncias e dados de revisão não são expostos indevidamente.
**Estimativa de complexidade:** Alta — envolve política, persistência, autorização e
ferramenta operacional.

---

### REELIX-011

**Título:** Decidir e validar a ativação da análise de spoilers por IA
**Módulo:** Avaliações / OpenAI
**Categoria:** Feature
**Prioridade:** Baixa
**Status:** Aguardando decisão

**Descrição:** Determinar se o classificador automático deve ser reativado, mantendo
sinalização manual disponível independentemente da IA.

**Estado atual:** O adaptador/worker existe, mas a configuração temporária mantém a
classificação desativada. A spec documenta consentimento explícito, retries e publicação
conservadora; nenhuma chave é colocada no app.

**Evidências:** [07-ratings-comments.md](docs/specs/07-ratings-comments.md),
[Program.cs](backend/Reelix.Api/Program.cs) e
[OpenAiSpoilerOptions.cs](backend/Reelix.Infrastructure/Ratings/OpenAiSpoilerOptions.cs).

**Impacto:** A análise automática não está disponível, mas reativá-la sem avaliar custo,
consentimento, precisão e falhas pode alterar a experiência/publicação de comentários.

**Solução proposta:** Decidir sobre valor e política de dados; se aprovada, usar segredo
server-side, limites de custo e monitoramento sem bloquear comentários não dependentes.

**Plano técnico de implementação:**
1. Aprovar finalidade, consentimento, retenção e custo máximo.
2. Configurar chave no secret manager do ambiente, nunca em mobile/appsettings versionado.
3. Validar schema de resposta, timeout, rate limit, tentativas e fallback conservador.
4. Testar respostas inválidas, indisponibilidade, duplicação e consentimento ausente.
5. Habilitar gradualmente e monitorar sem registrar texto/chave desnecessários.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Application/Ratings/`,
`backend/Reelix.Infrastructure/` (adapter OpenAI), `backend/Reelix.Api/Program.cs`,
`docs/specs/07-ratings-comments.md`.
**Dependências:** Decisão de produto, credencial segura, orçamento e REELIX-002.
**Critérios de aceitação:** Se habilitada, análise só ocorre com consentimento explícito,
limites e fallback testados; a indisponibilidade não bloqueia o restante do módulo.
**Estimativa de complexidade:** Média — o fluxo já existe, mas precisa de decisão e
validação operacional.

---

### REELIX-012

**Título:** Definir se recomendações por IA fazem parte do produto
**Módulo:** Home / Discovery / IA
**Categoria:** Feature
**Prioridade:** Baixa
**Status:** Aguardando decisão

**Descrição:** Aprovar ou rejeitar recomendações com IA; não confundir filtros
determinísticos por gênero com recomendação por modelo.

**Estado atual:** Home oferece sugestões determinísticas com preferências e catálogo.
Não foi encontrada integração de recomendação por IA; arquitetura diz que IA é opcional.

**Evidências:** [04-home-discovery.md](docs/specs/04-home-discovery.md),
[architecture.md](docs/architecture.md#10-ai-and-recommendations) e
[home-discovery-setup.md](docs/home-discovery-setup.md).

**Impacto:** Sem decisão, estimativas e requisitos de custo/privacidade para recomendações
não podem ser planejados com confiabilidade.

**Solução proposta:** Validar se recomendações determinísticas atendem ao produto.
Se IA for aprovada, definir dados de entrada, explicabilidade, consentimento, custos,
latência e fallback antes de desenhar integração.

**Plano técnico de implementação:**
1. Aprovar objetivo/indicadores e segmento do recurso.
2. Definir fonte de dados permitida e retenção.
3. Comparar baseline determinístico com proposta de IA.
4. Se aprovado, implementar serviço no backend e validação de saída/catalog grounding.
5. Testar qualidade, falhas, custos e comportamento sem consentimento.

**Arquivos provavelmente envolvidos:** `docs/specs/04-home-discovery.md`,
`backend/Reelix.Application/Catalog/` e `mobile/src/app/home.tsx`; novos serviços só após
aprovação.
**Dependências:** Decisão de produto, provedor/modelo, política de dados e orçamento.
**Critérios de aceitação:** Decisão documentada; se implementado, recomendação é baseada
em dados reais e não inventa títulos/atributos, com fallback funcional.
**Estimativa de complexidade:** Média — pode permanecer fora do roadmap; implementação
depende do escopo aprovado.

---

### REELIX-013

**Título:** Implementar e validar widgets nativos
**Módulo:** Widgets mobile
**Categoria:** Feature
**Prioridade:** Baixa
**Status:** Bloqueado

**Descrição:** Implementar widgets compatíveis com os sistemas suportados, validar
atualização, deep links, privacidade e limpeza por troca de conta.

**Estado atual:** Existe somente spike/documentação. Não há widget funcional, dependência
ou plataforma declarada como suportada. Faltam bundle/package IDs e ambiente de build
nativo validado.

**Evidências:** [12-mobile-widgets.md](docs/specs/12-mobile-widgets.md),
[app.json](mobile/app.json) e [wireframes](docs/wireframes/reelix-visual-wireframes.html).

**Impacto:** A capacidade planejada não está disponível aos usuários; implementação
precipitada pode expor dados de conta anterior em snapshots do sistema.

**Solução proposta:** Completar primeiro os pré-requisitos nativos e validar a biblioteca
Expo escolhida em builds de desenvolvimento; não presumir suporte em Expo Go.

**Plano técnico de implementação:**
1. Definir plataformas/versões e bundle/package IDs.
2. Validar build nativo e integração da biblioteca em iOS e Android.
3. Projetar dados mínimos e snapshots protegidos, limpeza no logout/troca de conta.
4. Implementar widget vazio, episódios pendentes e deep links internos.
5. Testar atualização limitada pelo SO, offline, usuário trocado e acessibilidade.
6. Atualizar wireframe e documentar limitações verificadas em dispositivos.

**Arquivos provavelmente envolvidos:** `mobile/app.json`, configuração de build
(ainda não definida), `mobile/src/`, `docs/specs/12-mobile-widgets.md` e
`docs/wireframes/reelix-visual-wireframes.html`.
**Dependências:** REELIX-005, IDs de distribuição, biblioteca compatível e ambiente
Xcode/Android SDK.
**Critérios de aceitação:** Build testado por plataforma; widget mostra somente snapshot
do usuário atual, respeita limitações de atualização e abre destino correto.
**Estimativa de complexidade:** Alta — envolve extensão/runtime nativo e validação em
duas plataformas.

---

### REELIX-015

**Título:** Executar validação manual de acessibilidade e UX multiplataforma
**Módulo:** Mobile / Design System
**Categoria:** UX/UI
**Prioridade:** Média
**Status:** Parcialmente implementado

**Descrição:** Validar experiência com leitor de tela, tamanhos de texto, contraste,
gestos, estados de erro e touch targets em iOS, Android e web quando suportado.

**Estado atual:** Além dos padrões existentes, o botão de retry da inicialização agora
usa `Pressable` com semântica de botão e alvo de 44 px; cada item da inbox tem rótulo e
dica para leitor de tela. Ainda não houve validação com VoiceOver/TalkBack, auditoria
de contraste ou teste visual multiplataforma; a sessão do navegador não abriu.

**Evidências:** [tokens.ts](mobile/src/theme/tokens.ts),
[\_layout.tsx](mobile/src/app/_layout.tsx),
[architecture.md — UX](docs/architecture.md#ux-standards-for-every-mobile-screen) e
[reelix-visual-wireframes.html](docs/wireframes/reelix-visual-wireframes.html).

**Impacto:** Falhas de semântica, contraste, escalabilidade do texto ou conflitos de
gesto podem impedir o uso por pessoas com deficiência ou variar entre plataformas.

**Solução proposta:** Definir roteiro por fluxos prioritários e registrar achados
reproduzíveis antes de corrigir; diferenciar defeito confirmado de hipótese de gesto.

**Plano técnico de implementação:**
1. Definir matriz de plataforma, dispositivos e tamanho de texto.
2. Auditar auth, Home, tracking, listas, social, perfil e notificações com leitores de tela.
3. Medir contraste de textos/estados e dimensões dos controles.
4. Verificar foco, anúncio de loading/erro, navegação por teclado web e estados vazios.
5. Registrar bugs com evidência, corrigir os aprovados e atualizar wireframes.

**Arquivos provavelmente envolvidos:** `mobile/src/app/`, `mobile/src/components/`,
`mobile/src/theme/tokens.ts`, `docs/architecture.md` e wireframes existentes.
**Dependências:** Builds/dispositivos acessíveis; REELIX-005.
**Critérios de aceitação:** Fluxos definidos passam por checklist em plataformas
declaradas; achados de alto impacto são corrigidos e os controles têm semântica/contraste
verificados.
**Estimativa de complexidade:** Média — teste manual requer várias plataformas e fluxos.

---

### REELIX-016

**Título:** Definir logging, métricas e alertas operacionais
**Módulo:** Backend / operações
**Categoria:** Technical Debt
**Prioridade:** Média
**Status:** Aguardando decisão

**Descrição:** Acordar a estratégia de observabilidade da API e workers antes de depender
de entrega assíncrona contínua ou operação em produção.

**Estado atual:** Há logging e workers hospedados, mas não foi identificado setup de
tracing distribuído, endpoint de métricas, monitoramento de crashes ou alertas. A
arquitetura documenta opções futuras, não uma implantação atual.

**Evidências:** [Program.cs](backend/Reelix.Api/Program.cs),
[architecture.md](docs/architecture.md#13-testing-and-quality) e
[technology-stack-evaluation.md](docs/technology-stack-evaluation.md).

**Impacto:** Falhas de catálogo, workers, push e banco podem não ser detectadas ou
correlacionadas com rapidez; registrar conteúdo pessoal pode criar risco de privacidade.

**Solução proposta:** Definir hosting, consumidor das métricas, retenção e política de
redação; introduzir apenas o mínimo necessário e proteger endpoints de métricas.

**Plano técnico de implementação:**
1. Definir SLOs/alertas mínimos para API, banco e workers.
2. Escolher stack compatível com hosting e controles de acesso.
3. Adicionar correlation IDs e métricas sem labels de alta cardinalidade/pessoais.
4. Configurar redaction de tokens, segredos e payloads sensíveis.
5. Exercitar alertas com falhas controladas e documentar resposta operacional.

**Arquivos provavelmente envolvidos:** `backend/Reelix.Api/Program.cs`,
`backend/Reelix.Infrastructure/DependencyInjection.cs` e configuração de hosting
(ainda não versionada).
**Dependências:** Decisão de plataforma de hosting, rede e requisitos de operação.
**Critérios de aceitação:** Falhas relevantes são observáveis/alertáveis; logs e métricas
não expõem tokens, chaves ou dados pessoais desnecessários.
**Estimativa de complexidade:** Média — depende do hosting, mas pode começar com
instrumentação pequena.

---

### REELIX-017

**Título:** Decidir se o Social terá posts e reações
**Módulo:** Social
**Categoria:** Feature
**Prioridade:** Baixa
**Status:** Aguardando decisão

**Descrição:** Aprovar ou rejeitar publicações autorais, curtidas, comentários em posts
e salvamento de posts como parte do produto.

**Estado atual:** A implementação/documentação descreve amizades, feed de atividades,
avaliações e compartilhamento autorizado. O perfil social especifica que likes,
comentários em publicações e posts salvos não estão disponíveis; não devem ser
confundidos com comentários de avaliações, que existem.

**Evidências:** [09-social.md](docs/specs/09-social.md),
[implementation-report-social-profile.md](docs/implementation-report-social-profile.md)
e [SocialEndpoints.cs](backend/Reelix.Api/Social/SocialEndpoints.cs).

**Impacto:** Desenvolver sem decisão amplia escopo, moderação, privacidade e custos de
persistência; a UI não deve sugerir que os recursos existem.

**Solução proposta:** Decidir o modelo social (somente atividades ou conteúdo criado
por usuários), visibilidade, interação e moderação. Atualizar specs antes de implementar.

**Plano técnico de implementação:**
1. Definir casos de uso e públicos autorizados.
2. Definir relação com comentários/avaliações e política de moderação.
3. Estimar impacto de schema, paginação, denúncias e notificações.
4. Se aprovado, desenhar contratos/API e controles de autorização.
5. Atualizar wireframes e implementar/testar por fatias verticais.

**Arquivos provavelmente envolvidos:** `docs/specs/09-social.md`,
`backend/Reelix.Api/Social/`, `backend/Reelix.Domain/Social/` e `mobile/src/app/social.tsx`.
**Dependências:** Decisão de produto; REELIX-010 antes de conteúdo público com moderação.
**Critérios de aceitação:** Decisão explícita e specs coerentes; se implementado, posts e
reações têm privacidade, autorização e moderação testadas.
**Estimativa de complexidade:** Alta se aprovado — altera modelo de dados, endpoints,
moderação e UX.

---

### REELIX-018

**Título:** Confirmar termos e uso comercial dos dados de catálogo
**Módulo:** Catálogo / distribuição
**Categoria:** Technical Debt
**Prioridade:** Alta
**Status:** Aguardando decisão

**Descrição:** Confirmar que atribuição, finalidade de uso e distribuição dos dados/
imagens dos provedores são compatíveis com a finalidade de lançamento do Reelix.

**Estado atual:** A configuração documenta TMDB como fonte principal, atribuição exigida
e limitação de uso comercial, e OMDb como opcional; a finalidade comercial do produto
não foi confirmada nesta auditoria. A atribuição TMDB é documentada na interface.

**Evidências:** [catalog-setup.md](docs/catalog-setup.md),
[03-catalog.md](docs/specs/03-catalog.md) e
[onboarding-profile-setup.md](docs/onboarding-profile-setup.md).

**Impacto:** Uso incompatível com os termos ou atribuição incompleta pode bloquear a
distribuição; caches/imagens têm limites contratuais específicos.

**Solução proposta:** Confirmar finalidade/mercados e revisar termos oficiais vigentes
antes de comercializar ou ampliar armazenamento/redistribuição.

**Plano técnico de implementação:**
1. Definir finalidade comercial, mercados e dados que serão exibidos/armazenados.
2. Revisar termos de TMDB/OMDb e requisitos de atribuição com responsáveis apropriados.
3. Confirmar placement de créditos, uso de imagens e política de cache/retention.
4. Se necessário, obter licença/alternativa antes de habilitar o cenário.
5. Registrar aprovação, data de revisão e mudanças operacionais necessárias.

**Arquivos provavelmente envolvidos:** `docs/catalog-setup.md`,
`docs/specs/03-catalog.md`, `mobile/src/app/profile.tsx` e componentes do catálogo.
**Dependências:** Decisão sobre modelo comercial e confirmação atual dos provedores.
**Critérios de aceitação:** Uso pretendido e créditos foram confirmados; imagens/cache
respeitam regras documentadas; nenhuma aprovação é presumida por esta auditoria.
**Estimativa de complexidade:** Média — depende de revisão de termos e eventual negociação
externa; não é uma estimativa jurídica.

---

### REELIX-019

**Título:** Atualizar dependências Expo com advisories moderados
**Módulo:** Mobile / segurança de dependências
**Categoria:** Security
**Prioridade:** Média
**Status:** Bloqueado

**Descrição:** Remover vulnerabilidades moderadas transitivas reportadas no audit npm
para as dependências Expo sem degradar o SDK principal.

**Estado atual:** `npm audit --omit=dev` reportou 8 vulnerabilidades moderadas em
dependências transitivas `glob` e `minimatch`, alcançadas pelo Expo SDK 58 e seus
plugins. A correção automática sugere `npm audit fix --force` e `expo@51.0.39`, um
downgrade incompatível que não foi aplicado. Não foi identificada uma atualização
compatível que remova os advisories durante esta etapa.

**Evidências:** [package.json](mobile/package.json),
[package-lock.json](mobile/package-lock.json) e o resultado de
`npm audit --omit=dev --audit-level=high` em 03/11/2026.

**Impacto:** Os advisories reportam bypass de cancelamento por AbortSignal com possível
travessia de filesystem após timeout em `glob` e potencial DoS por amplificação de
partes de padrões em `minimatch`. A auditoria classifica os achados como moderados; não
se presume exploração no app distribuído.

**Solução proposta:** Atualizar Expo/plugins para releases compatíveis que incluam as
versões corrigidas. Não forçar override de major transitive sem validar todas as APIs
consumidas pelos plugins nativos.

**Plano técnico de implementação:**
1. Acompanhar releases compatíveis do Expo SDK 58 e changelogs de glob/minimatch.
2. Atualizar somente versões compatíveis via ferramentas apropriadas do Expo/npm.
3. Regenerar lockfile e executar lint, type-check, Expo Doctor, builds e prebuild.
4. Executar `npm audit --omit=dev` e confirmar que os advisories foram removidos.
5. Se o upstream não corrigir em prazo aceitável, avaliar override isolado e comprovar
   compatibilidade antes da adoção.

**Arquivos provavelmente envolvidos:** `mobile/package.json`,
`mobile/package-lock.json` e [.github/workflows/ci.yml](.github/workflows/ci.yml).
**Dependências:** Release upstream compatível do Expo ou validação completa de overrides.
**Critérios de aceitação:** Audit npm de produção sem advisories aplicáveis; SDK Expo
continua compatível com React Native; lint/type-check e builds nativos declarados passam.
**Estimativa de complexidade:** Média — aguarda upstream; um override major aumenta o
risco de quebrar plugins nativos.

---

## Decisões pendentes fora do backlog de implementação aprovado

- Se o MVP inclui recomendações por IA, posts/reactions, denúncias/moderação manual,
  upload de avatar ou exclusão/exportação de conta.
- Se e quando habilitar a análise de spoilers por OpenAI.
- Qual hosting, CI/CD, observabilidade e política de migrações serão usados.
- IDs de distribuição e plataformas nativas oficialmente suportadas.
- Se o uso do catálogo será comercial e quais direitos/atribuições serão necessários.

Esses itens aparecem como tarefas “Aguardando decisão”; não são afirmados como bugs ou
requisitos já aprovados.

## Resolvidas nesta etapa

### REELIX-014 — Navegação dos destinos atualmente suportados

A auditoria confirmou que os destinos internos atualmente emitidos são `/social` e
`/notifications`; o domínio rejeita destinos fora da allowlist e o cliente valida o
mesmo conjunto. A inbox e o handler de push navegam para esses destinos. Não existem
notificações de catálogo com destino específico no escopo atual. Assim, não era uma
lacuna funcional existente e não foi criada navegação para eventos futuros.

**Evidências:** `UserNotification.IsSafeDeepLink`,
`mobile/src/services/api-client.ts`, `mobile/src/app/_layout.tsx` e
`mobile/src/app/notifications.tsx`.

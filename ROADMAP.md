# Reelix — roadmap recomendado

Roadmap baseado no estado do código em 04/11/2026. A ordem prioriza validar o que já
existe antes de ampliar funcionalidades. Não representa compromisso de prazo.

## Situação do checkout

O checkout avaliado está na branch `build-mobile`, commit `98be2da`, dois commits à
frente de `main` (`2917e95`). O build EAS `b0c790c9-bc1c-4c76-82fa-33bf317c92b6` para
esse commit concluiu e o usuário confirmou conexão do Samsung ao Metro, login e navegação.
O smoke test básico Android passou; release, deep links, push e widgets não foram
validados. O usuário confirmou que existe apenas `Development`; seu histórico registra as
10 migrations locais e a cadeia também passa no PostgreSQL 17 dos testes. A execução
remota da CI nesta branch ainda não foi confirmada.

## Fase 1 — Confiabilidade do schema e autorização

**Objetivo:** reduzir o risco de release por divergência de banco ou isolamento
insuficientemente validado.

**Itens:** [REELIX-001](DEVELOPMENT_BACKLOG.md#reelix-001) e
[REELIX-002](DEVELOPMENT_BACKLOG.md#reelix-002), concluídos para o escopo atual.

**Ordem recomendada:**
1. Inventário do único ambiente e histórico de migrations concluídos (REELIX-001).
2. Testes HTTP/PostgreSQL e autorização por recurso concluídos (REELIX-002).
3. Ao criar outro ambiente, validar migrations e smoke tests antes de habilitá-lo.

**Dependências:** Um PostgreSQL descartável para validar migrations; o fixture
Testcontainers cobre esse requisito atualmente.

**Critérios de conclusão:** Schema por ambiente registrado; migrations e fluxos dos
módulos críticos passam em PostgreSQL real; testes verificam acessos negados/isolamento.

## Fase 2 — Quality gates reproduzíveis

**Objetivo:** garantir que regressões de backend/mobile e riscos de dependências sejam
detectados antes de integrar mudanças.

**Estado:** Concluída para o escopo atual. [REELIX-004](DEVELOPMENT_BACKLOG.md#reelix-004)
foi merged com 4/4 checks aprovados. [REELIX-003](DEVELOPMENT_BACKLOG.md#reelix-003)
continua fora do escopo combinado; os advisories moderados permanecem em
[REELIX-019](DEVELOPMENT_BACKLOG.md#reelix-019), bloqueado por atualização compatível.

**Resultado:** PR #11 foi merged em `main` como `20145fa`, com 4/4 checks aprovados;
os jobs `backend` e `mobile` passaram. REELIX-003 não foi iniciado, conforme o escopo
combinado. REELIX-019 permanece bloqueado, sem downgrade forçado.

**Dependências:** Ativação/configuração do GitHub e acesso aos feeds de pacotes;
REELIX-002 alimenta a execução automatizada de integração.

**Critérios de conclusão:** Pull requests executam checks reproduzíveis; scanners não
reportam falso sucesso quando a fonte está indisponível; nenhum segredo é emitido.

## Fase 3 — Validar distribuição e operação móvel

**Objetivo:** confirmar os fluxos nativos e tornar notificações remotas observáveis e
seguras antes de habilitá-las.

**Pendências:** [REELIX-005](DEVELOPMENT_BACKLOG.md#reelix-005),
[REELIX-006](DEVELOPMENT_BACKLOG.md#reelix-006),
[REELIX-015](DEVELOPMENT_BACKLOG.md#reelix-015).

**Ordem recomendada:**
1. Validar o APK EAS Android, os fluxos prioritários e a conexão com a API; IDs e perfis já estão configurados (REELIX-005). iOS segue fora do aceite atual dos widgets.
2. Executar a avaliação manual de UX/acessibilidade nas plataformas declaradas
   (REELIX-015).
3. Habilitar push em staging e validar permissões, retries e receipts (REELIX-006).

**Dependências:** EAS/APNs/FCM, dispositivos/builds, hosting contínuo e REELIX-004.

**Critérios de conclusão:** Builds instaláveis passam os fluxos prioritários; navegação
de push é restrita a destinos internos; acessibilidade básica é verificada; push pode
ser desabilitado sem indisponibilizar a inbox.

## Fase 4 — Decisões de privacidade, conteúdo e provedores

**Objetivo:** resolver decisões que alteram retenção, moderação ou direitos de uso antes
de expandir escopo ou lançar comercialmente.

**Pendências:** [REELIX-008](DEVELOPMENT_BACKLOG.md#reelix-008),
[REELIX-009](DEVELOPMENT_BACKLOG.md#reelix-009),
[REELIX-010](DEVELOPMENT_BACKLOG.md#reelix-010),
[REELIX-018](DEVELOPMENT_BACKLOG.md#reelix-018).

**Ordem recomendada:**
1. Definir ciclo de vida de conta e dados (REELIX-008).
2. Confirmar uso comercial/atribuição dos provedores (REELIX-018).
3. Decidir avatar personalizado e moderação administrativa (REELIX-009/010).
4. Implementar apenas o escopo aprovado, com autorização e testes.

**Dependências:** Decisões de produto/privacidade, revisão dos termos atuais dos
provedores e operação responsável por conteúdo.

**Critérios de conclusão:** Decisões documentadas; specs atualizadas; políticas e fluxos
de dados/moderação testados antes de ativação.

## Fase 5 — Recursos opcionais e expansão do produto

**Objetivo:** evoluir o produto sem atribuir compromissos a recursos ainda não aprovados.

**Pendências:** [REELIX-007](DEVELOPMENT_BACKLOG.md#reelix-007),
[REELIX-011](DEVELOPMENT_BACKLOG.md#reelix-011),
[REELIX-012](DEVELOPMENT_BACKLOG.md#reelix-012),
[REELIX-013](DEVELOPMENT_BACKLOG.md#reelix-013),
[REELIX-016](DEVELOPMENT_BACKLOG.md#reelix-016),
[REELIX-017](DEVELOPMENT_BACKLOG.md#reelix-017).

**Ordem recomendada:**
1. Definir hosting/observabilidade mínima para workers (REELIX-016).
2. Aprovar fonte e scheduler de catálogo antes de alertas (REELIX-007).
3. Revisar valor, privacidade e custo antes de reativar IA ou aprovar recomendações
   (REELIX-011/012).
4. Após validar o build Android, implementar e validar widgets Android (REELIX-013); decidir separadamente sobre conteúdo social autoral (REELIX-017). iOS permanece adiado.

**Dependências:** Decisões de produto, serviços externos e capacidades de hosting.

**Critérios de conclusão:** Cada recurso opcional tem decisão explícita, limites de
privacidade/custo, validação específica e documentação atualizada; itens não aprovados
permanecem fora da UI e não são simulados.

## Próximo passo

REELIX-001 foi concluído para o único ambiente informado (`Development`), e a Fase 2 foi
concluída: o PR #11 foi merged com 4/4 checks aprovados. Na Fase 3, REELIX-005 avançou:
login/navegação, logout e despacho de callback inválido passaram no Samsung. Falta validar
callback válido/reset e demais deep links internos; o APK de development já está disponível.
Push remoto (REELIX-006) segue condicionado a credenciais e hosting; widgets Android
(REELIX-013) continuam sem código funcional. Se um novo ambiente de banco for criado,
repetir a validação de migrations antes do uso.

REELIX-014 foi fechado após confirmar que os destinos internos implementados cobrem os
eventos atuais; novas categorias de notificação precisarão definir o próprio destino.

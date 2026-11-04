# Reelix — roadmap recomendado

Roadmap baseado no estado do código em 04/11/2026. A ordem prioriza validar o que já
existe antes de ampliar funcionalidades. Não representa compromisso de prazo.

## Situação do checkout

O checkout avaliado está na branch `build-mobile`, commit `98be2da`, dois commits à
frente de `main` (`2917e95`). O build EAS `b0c790c9-bc1c-4c76-82fa-33bf317c92b6` para
esse commit concluiu e o usuário confirmou conexão do Samsung ao Metro, login e navegação.
O smoke test básico Android passou; release, deep links, push e widgets não foram
validados. O endpoint OpenAPI respondeu HTTP 200 no PC; `adb` não está disponível
localmente. A execução remota da CI nesta branch também não foi confirmada. O risco de
schema por ambiente continua aberto e é o principal gate de prontidão.

## Fase 1 — Confiabilidade do schema e autorização

**Objetivo:** reduzir o risco de release por divergência de banco ou isolamento
insuficientemente validado.

**Pendências:** [REELIX-001](DEVELOPMENT_BACKLOG.md#reelix-001),
[REELIX-002](DEVELOPMENT_BACKLOG.md#reelix-002).

**Ordem recomendada:**
1. Inventariar bancos e aprovar processo seguro de migrations (REELIX-001).
2. Adicionar testes HTTP/PostgreSQL e autorização por recurso (REELIX-002).
3. Executar migrations e smoke tests em staging antes de cada release.

**Dependências:** Credenciais e autorização para ambiente de teste; banco PostgreSQL
descartável ou staging.

**Critérios de conclusão:** Schema por ambiente registrado; migrations e fluxos dos
módulos críticos passam em PostgreSQL real; testes verificam acessos negados/isolamento.

## Fase 2 — Quality gates reproduzíveis

**Objetivo:** garantir que regressões de backend/mobile e riscos de dependências sejam
detectados antes de integrar mudanças.

**Pendências:** [REELIX-003](DEVELOPMENT_BACKLOG.md#reelix-003),
[REELIX-004](DEVELOPMENT_BACKLOG.md#reelix-004),
[REELIX-019](DEVELOPMENT_BACKLOG.md#reelix-019).

**Ordem recomendada:**
1. Escolher testes mobile, priorizando fluxos críticos (REELIX-003; fora desta etapa).
2. Executar e ajustar a workflow, configurar proteções de branch e secret scanning
   (REELIX-004).
3. Atualizar Expo/plugins quando houver versão compatível sem os advisories moderados
   (REELIX-019).

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

O smoke test Android básico de REELIX-005 passou: o usuário conectou ao Metro, entrou no
app e navegou. O próximo passo é REELIX-001: inventariar bancos/ambientes e responsáveis,
comparar o estado de migrations e validar a atualização em staging ou PostgreSQL
descartável. Registrar o resultado por ambiente; não aplicar migrations em produção sem
autorização explícita. Depois, decidir e planejar a implementação Android de
`expo-widgets` (REELIX-013), que continua sem código funcional.

REELIX-014 foi fechado após confirmar que os destinos internos implementados cobrem os
eventos atuais; novas categorias de notificação precisarão definir o próprio destino.

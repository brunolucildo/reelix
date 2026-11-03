# Reelix — roadmap recomendado

Roadmap baseado no estado do código em 03/11/2026. A ordem prioriza validar o que já
existe antes de ampliar funcionalidades. Não representa compromisso de prazo.

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
1. Definir IDs e validar builds nativos (REELIX-005).
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
4. Só então decidir sobre widgets e conteúdo social autoral (REELIX-013/017).

**Dependências:** Decisões de produto, serviços externos e capacidades de hosting.

**Critérios de conclusão:** Cada recurso opcional tem decisão explícita, limites de
privacidade/custo, validação específica e documentação atualizada; itens não aprovados
permanecem fora da UI e não são simulados.

## Primeiro próximo passo

Começar por REELIX-001: confirmar quais bancos/ambientes existem e quem autoriza
migrations. Em seguida, priorizar REELIX-002 para demonstrar persistência e autorização
dos módulos antes de expandir o escopo.

REELIX-014 foi fechado após confirmar que os destinos internos implementados cobrem os
eventos atuais; novas categorias de notificação precisarão definir o próprio destino.

---
name: implement-me
description: Executes a spec end-to-end by loading project context, validating gaps with feature-concept, generating a plan, implementing it in vertical slices, running QA, and reporting. Use this skill whenever the user says "implement-me", "executa a spec", "implementa a spec", "rodar a spec", "aplicar o plano", "implement this spec", "build this feature from the spec", or passes a spec/plan name and expects the feature to be built—even if they do not mention the skill by name. Do not use it for planning only; use feature-concept for that.
---

# Implement Me

Executar uma spec de ponta a ponta: **contexto → validação → plano → implementação → QA → ADR → commit e push → relatório**.

Esta skill é a ponte entre uma spec escrita e código funcionando. Ela não substitui o julgamento: se a spec estiver incompleta, pare e complete antes de codar.

## Contrato da skill

- **Entrada:** nome da spec, caminho de arquivo ou plano existente.
- **Saída:** código implementado, plano atualizado, ADR (quando aplicável), commit e push feitos pela skill `commit-and-push` e relatório final no formato de `.docs/RULES.md`.
- **Não faz:** inventar requisitos, pular QA, declarar testes que não executou ou expandir escopo sem confirmação.

## 1. Resolver a spec

Aceite qualquer uma destas formas:

```text
implement-me auth-email-password
implement-me ./.specs/auth-email-password.md
implement-me ./.plans/auth-email-password-plan.md
```

Ordem de resolução para um slug `X`:

1. Caminho exato, se a entrada parecer um arquivo.
2. `.specs/X.md`
3. `.specs/X-spec.md`
4. `.plans/X-plan.md`
5. `.plans/X.md`
6. Busca por `**/X*.md`, ignorando `node_modules`, `bin`, `obj`, `.expo` e `dist`.

Se houver mais de um candidato plausível, mostre os caminhos e peça para escolher. Se não houver nenhum, peça a spec ou use `create-spec` (spec nova) / `feature-concept` (spec + plano) para criá-la.

Quando a spec usar `.specs/_template.md`, verifique se todas as seções estão preenchidas: Escopo, Contexto, Necessidade de negócio, Requisitos, Fluxos e estados, Acceptance Criteria, O que fazer, O que não fazer, Riscos e dependências, Resultado, Perguntas abertas e premissas. Seção `TODO` é lacuna, não requisito. Se houver lacuna estrutural, rode `refine-spec` antes de implementar.

## 2. Carregar contexto (obrigatório)

Leia primeiro, nesta ordem:

1. `AGENTS.md` da raiz — ponteiro curto para `.docs/AGENTS.md`.
2. `.docs/AGENTS.md` — guia operacional, comandos e regras não negociáveis.
3. `.docs/BUSINESS.md` — domínio, vocabulário e escopo.
4. `.docs/ARCHITECTURE.md` — limites, stack instalada vs. alvo e dívidas.
5. `.docs/DEVELOPMENT.md` — princípios, camadas e padrões.
6. `.docs/QA.md` — gates reais, baseline e checklist.
7. `.docs/RULES.md` — fluxo, templates de plano/ADR/relatório.
8. A spec/plano alvo.
9. `./.adrs/` relevantes.
10. Código e configs das áreas afetadas (`Reelix.sln`, `.csproj` e `appsettings*` no backend; `package.json`, `app.json` e `tsconfig.json` em `mobile/`).

**Regra de precedência:** código executável > `.docs` > `README.md` da raiz. Se a spec contradizer o código ou a arquitetura, pare e mostre o conflito.

Antes de qualquer edição, produza um **contexto de execução** curto:

```markdown
## Contexto de execução

- Spec: <caminho>
- Objetivo: ...
- Restrições: ...
- Baseline conhecido: build/testes do backend e lint/tipos/audit do mobile (ver QA)
- Áreas afetadas: ...
- Riscos: ...
```

## 3. Validar a spec com feature-concept

Leia a spec e monte uma lista de lacunas:

- Requisito sem critério de aceite.
- Fluxo sem estado vazio/erro/carregando.
- Permissão ou isolamento por usuário/recurso não definido (quem pode ler e alterar o quê).
- Dado sensível sem retenção/isolamento.
- Integração sem timeout/retry/falha.
- Não-objetivo ausente.
- Decisão arquitetural sem alternativa considerada.
- Critério de pronto impossível de verificar.

**Se houver lacuna bloqueante**, carregue a skill `feature-concept` e faça apenas as perguntas necessárias para fechar a lacuna. Não repita o interrogatório inteiro; a spec já existe.

**Se a spec estiver completa**, não faça perguntas de processo. Siga para o plano.

Atualize a spec/plano com as respostas. Marque defaults como premissas. Se a pessoa não estiver disponível e a lacuna for reversível, escolha o default mais conservador, registre a premissa e continue; se for irreversível (auth, dados, dinheiro, permissões), pare.

## 4. Gerar o plano

Crie ou atualize `.plans/<slug>-plan.md` usando o template de `.docs/RULES.md`. A spec correspondente fica em `.specs/<slug>.md` e segue `.specs/_template.md`.

O plano precisa conter:

- Contexto, objetivo e não-objetivos.
- Requisitos com IDs e critérios de aceite.
- Fluxos e estados.
- Dados, permissões e integrações.
- Decisões, premissas e dúvidas.
- Fatias verticais de implementação (não camadas horizontais).
- Arquivos/áreas afetadas.
- Estratégia de testes por fatia.
- Riscos, rollback e critério de pronto.

Se já existir um plano, **reconcilie** em vez de sobrescrever: preserve decisões anteriores, marque o que mudou e registre o motivo.

Apresente um resumo de 5–10 linhas. Prossiga para a implementação, salvo se houver:

- decisão de alto impacto ou irreversível;
- dependência nova relevante;
- mudança destrutiva;
- conflito entre spec e documentação;
- lacuna bloqueante sem resposta.

Nesses casos, peça confirmação explícita.

## 5. Implementar em fatias verticais

Trabalhe uma fatia por vez: **requisito → código → teste → verificação**. Evite escrever todo o código e só depois testar.

Antes de cada fatia, carregue as skills especializadas aplicáveis:

| Contexto                                                              | Skill                              |
| --------------------------------------------------------------------- | ---------------------------------- |
| Comportamento novo ou bug                                             | `tdd`                              |
| UI, identidade visual (React Native)                                  | `frontend-design`                  |
| Revisão de UI, a11y, UX                                               | `web-design-guidelines`            |
| Postgres/Supabase: migrations, RLS, índices, queries                  | `supabase-postgres-best-practices` |
| Fluxo E2E no browser (Expo web), quando o gate de E2E existir         | `playwright`                       |
| Mudança estrutural de camadas no backend (.NET)                       | `refactor-slices`                  |

Se uma skill não estiver instalada, siga os equivalentes em `.docs` e registre a ausência no relatório. Não invente comandos ou APIs.

Regras de execução:

- Respeite os limites de `.docs/ARCHITECTURE.md` (regra de dependência do backend: `Api → Application → Domain`, `Infrastructure` implementa as portas e o `Domain` não importa framework; no mobile, tela não fala HTTP direto).
- Siga `.docs/DESIGN.md` e `.docs/DESIGN_SYSTEM.md` em qualquer UI (tokens de tema, nada de hex solto; copy de interface em pt-BR; identificadores, comentários e logs em inglês).
- Faça a menor mudança que atende ao requisito; não refatore fora do escopo.
- Não adicione dependência sem justificar problema, alternativa e custo.
- Não edite artefatos gerados (`bin/`, `obj/`, `node_modules/`, `.expo/`, `dist/`, `coverage/`, `TestResults/`).
- Não commite segredo, PII ou dado real.
- Atualize `.docs` na mesma mudança se a realidade divergir.
- Após cada fatia, rode a verificação mais barata que prove o comportamento (`dotnet test --filter` focado, `npx tsc --noEmit`, lint do arquivo).

## 6. Validar

Execute os gates reais de `.docs/QA.md` das áreas que a mudança toca, na ordem.

Backend (na raiz do repositório):

```bash
dotnet restore Reelix.sln
dotnet build Reelix.sln -c Release
dotnet test Reelix.sln -c Release
```

Mobile (dentro de `mobile/`):

```bash
npm run lint
npx tsc --noEmit
npm audit --audit-level=high
```

Gates futuros, **só quando existirem** (serão criados por specs; confira `.docs/QA.md` e os scripts de `mobile/package.json`): `npm test` (Jest/RNTL), `npm run test:e2e` (Playwright sobre Expo web), cobertura e testes de arquitetura. Se a spec cria um desses gates, ele passa a ser obrigatório a partir dela.

Observações:

- **Nunca** afirme que um comando passou sem ter executado.
- Se um gate falhar por causa de baseline conhecido registrado em `.docs/QA.md`, registre como dívida pré-existente com evidência; não atribua à sua mudança e não mascare.
- Testes só entram no relatório se o runner existir e você os tiver executado. Os testes de integração com Testcontainers exigem Docker no ar; sem Docker, declare que não rodaram.
- Para UI, faça a verificação manual de `.docs/QA.md` (dispositivo/emulador ou Expo web, telas pequenas e grandes, foco e leitor de tela, contraste, console e estados vazio/carregando/erro).
- Se a mudança for docs-only, build e testes podem ser dispensados com justificativa; o que fizer sentido (lint de Markdown, links) continua desejável.

Se um gate falhar:

1. Leia a causa.
2. Corrija.
3. Rode novamente.
4. Se for pré-existente e fora do escopo, registre com evidência e siga.

## 7. Registrar ADR

Crie `./.adrs/NNNN-titulo.md` quando a implementação:

- muda estrutura de pastas ou contrato entre partes;
- adiciona dependência relevante;
- escolhe tecnologia ou padrão;
- altera modelo de dados, autenticação ou autorização por recurso;
- define estratégia de testes, observabilidade ou deploy.

Use o template de `.docs/RULES.md`. Numere sequencialmente e atualize o índice, se existir.

## 8. Commit e push

**Último passo de toda execução de spec.** Depois de implementar, validar e registrar o ADR, chame a skill `commit-and-push` (pela ferramenta `Skill`, passando o slug da spec como contexto) para commitar todas as mudanças com uma mensagem Conventional Commits e fazer o push.

Antes de chamar:

1. Atualize o status da spec e do plano (ex.: `concluída`, ou `em implementação` com a pendência declarada) e o histórico de refinamento.
2. Confirme que os gates da seção 6 foram executados e passaram. **Não commite com gate obrigatório falhando**: pare, explique e peça decisão. Falhas pré-existentes e fora do escopo, já registradas com evidência, não bloqueiam.
3. Garanta que nenhum segredo, `.env` (raiz ou `mobile/.env`) ou artefato gerado está entre as mudanças.

Se `commit-and-push` parar (sem `.git`, sem remoto, conflito, push rejeitado, arquivo suspeito), **não contorne**: registre o motivo no relatório final como pendência e informe o próximo passo ao usuário.

## 9. Relatório final

Siga `.docs/RULES.md` seção 6, nesta ordem:

1. **Decisões arquiteturais** — resumo e link do ADR.
2. **QA manual** — passos reproduzíveis.
3. **Arquivos modificados** — caminhos.
4. **Bibliotecas instaladas** — nome, versão, workspace e motivo; “nenhuma” se for o caso.
5. **Racionais** — por que a solução é correta e o que foi descartado.
6. **Comandos executados** — comando e resultado real; declare o que não rodou (inclua o commit e o push).

Inclua também:

- Commit e push: branch, hash curto, header da mensagem e resultado do push (ou o motivo de não ter ocorrido).
- Requisitos da spec atendidos (R1, R2...).
- O que ficou fora ou pendente.
- Riscos residuais e próximo passo.

## 10. Quando parar

Pare e pergunte se:

- a spec não existe ou está ambígua em ponto bloqueante;
- a mudança é destrutiva ou irreversível;
- falta credencial, serviço externo ou dado;
- a solução exige dependência pesada ou troca de tecnologia;
- o comportamento esperado conflita com `.docs/BUSINESS.md` ou `.docs/ARCHITECTURE.md`;
- o baseline está quebrado de forma que impede validar a mudança.

Ao parar, explique o bloqueio, apresente opções e recomende uma.

## Guardrails

- Não implemente além da spec; melhorias viram sugestão.
- Não faça “teste de fumaça” mental e chame de QA.
- Não use `any`, `@ts-ignore`, `eslint-disable` (mobile) nem `#pragma warning disable`/`[SuppressMessage]` (backend) para fazer o gate passar.
- Não deixe `console.log`, `Console.WriteLine`, placeholder ou código morto.
- Não edite migration já aplicada (crie outra) e não apague `Down` nem o snapshot do EF.
- Não encerre a execução da spec sem rodar `commit-and-push`, nem commite com gate obrigatório falhando.
- Se a spec pedir algo que a arquitetura proíbe, a arquitetura vence até decisão explícita em contrário.

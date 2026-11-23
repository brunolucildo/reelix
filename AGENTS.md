# AGENTS.md

O guia operacional deste repositório, com comandos, regras não negociáveis, skills e fluxo de trabalho, está em [`.docs/AGENTS.md`](.docs/AGENTS.md). Leia-o antes de qualquer tarefa; a documentação canônica fica toda em [`.docs/`](.docs/) (`RULES.md`, `ARCHITECTURE.md`, `DEVELOPMENT.md`, `QA.md`, `TESTING.md`, `DESIGN.md`, `DESIGN_SYSTEM.md`, `BUSINESS.md`).

Regras mínimas, caso você não consiga abrir os documentos:

- Não invente comandos, scripts, dependências ou skills; confira `Reelix.sln`, os `.csproj` e `mobile/package.json`.
- Não afirme que executou build, lint, tipos ou testes sem ter executado e visto o resultado.
- Segredos nunca entram no repositório; `EXPO_PUBLIC_*` é público; jamais use `service_role` no app.
- Se o código divergir da documentação, o código vence — atualize `.docs/` na mesma mudança.

O app mobile tem regras adicionais em [`mobile/AGENTS.md`](mobile/AGENTS.md).

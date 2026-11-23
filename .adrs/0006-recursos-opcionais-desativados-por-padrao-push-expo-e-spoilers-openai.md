# 0006 — Recursos opcionais desativados por padrão: push Expo e spoilers OpenAI

- **Status:** aceito
- **Data:** 2026-11-01 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

Duas capacidades dependem de serviço externo com credencial, custo ou configuração nativa: o push remoto (Expo Push, que exige projeto EAS, APNs/FCM, segredo de servidor e hosting contínuo) e a análise automática de spoilers por OpenAI (custo, política de dados e consentimento). As specs `10-notifications.md` e `07-ratings-comments.md` exigem que a falha de um serviço externo não derrube o resto e que as chaves fiquem só no servidor. Nenhum ambiente com essas credenciais foi verificado (`.docs/QA.md`, "Entrega externa").

## Decisão

- **Push Expo:** seção `ExpoPush` com `Enabled` (padrão `false`; não consta em `appsettings.json`), `ProjectId`, `AccessToken` e `MaxAttempts` (padrão 5, faixa 1 a 10). `ValidateOnStart` só exige `ProjectId`, `AccessToken` e a faixa quando `Enabled` é `true`. Desligado, `NotificationDeliveryWorker` não processa entregas (reavalia a cada 1 min) e o registro de dispositivo responde 503; a inbox social continua funcionando.
- **Spoilers OpenAI:** seção `OpenAI:SpoilerAnalysis`; `appsettings.json` traz `Enabled=false` e `TemporarilyDisabled=true`. A análise só roda se `Enabled && !TemporarilyDisabled` (`OpenAiCommentModerationConfiguration`). Quando ativa exige `ApiKey` e `Model` (padrão `gpt-4o-mini`), com `TimeoutSeconds` de 1 a 30 (padrão 10). A análise é por comentário e só ocorre quando solicitada explicitamente (`AutomatedAnalysisRequested`, README). Sinalização manual de spoiler continua sempre disponível.
- **Execução assíncrona:** a classificação roda no `CommentModerationWorker` (lotes de 5, 3 tentativas), fora do caminho de gravação do comentário; falha da OpenAI não bloqueia avaliações.
- **Segredos só no servidor:** `ExpoPush__AccessToken` e `OpenAI:SpoilerAnalysis:ApiKey` vêm de User Secrets ou do gerenciador do ambiente, nunca de `appsettings*.json` nem do app. O `projectId` EAS em `mobile/app.json` é identificador público, não segredo. O CI roda Gitleaks sobre o histórico.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Ligar os dois recursos por padrão (inferido) | Funcionalidade completa desde o início | Quebra a partida sem credencial; custo e dados enviados à OpenAI sem decisão | `.docs/QA.md`: reativar exige decisão, segredo, controle de custo e consentimento |
| Remover o código até haver decisão (inferido) | Sem superfície a manter | Perde o trabalho de integração e dos testes | O código fica pronto e inerte, atrás de flag |
| Classificar spoiler de forma síncrona na gravação | Resultado imediato | Latência e falha da OpenAI afetam o comentário | Spec 07: falha da OpenAI não pode comprometer a gravação de avaliações |
| Enviar push direto do `SocialService` (inferido) | Menos peças | Acopla a ação do usuário ao serviço externo | Spec 10: evitar dependência síncrona de serviços externos (ADR 0007) |

## Consequências

- Positivas: a API sobe e funciona sem credencial de Expo ou OpenAI; reativar é configuração, não deploy de código novo; nenhum segredo no app; custo e envio de texto à OpenAI ficam sob decisão explícita.
- Negativas: parte do código fica inerte e não é exercitada fora dos testes (`ExpoPushClientTests`, `CommentModerationTests`); dois estados (`Enabled` e `TemporarilyDisabled`) exigem cuidado ao reativar; habilitar o push exige hosting contínuo para o worker; custo, política de dados e consentimento da OpenAI, e se e quando habilitar a análise, seguem pendentes de decisão (`.docs/ARCHITECTURE.md §6.3`).
- Impacto em código, operação e time: configuração documentada no `README.md` ("Banco de dados" e "Configure e inicie a API") e em `.specs/0007-ratings-comments.md`; reativar spoilers: `Enabled=true`, `TemporarilyDisabled=false` e `ApiKey`; ativar push: `ExpoPush__Enabled=true`, `ProjectId`, `AccessToken` e build nativo com credenciais APNs/FCM. Alertas de episódios e estreias não existem, independentemente da flag.

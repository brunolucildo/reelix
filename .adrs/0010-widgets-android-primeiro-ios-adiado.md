# 0010 — Widgets: Android primeiro, iOS adiado

- **Status:** aceito
- **Data:** 2026-11-03 (decisão de produto datada em `.specs/0012-mobile-widgets.md`)
- **Origem:** registro retroativo de decisão já implementada

> **Estado real:** o widget **ainda não está implementado**. Esta decisão cobre o escopo e a estratégia; `expo-widgets` não está instalado e nenhum widget funcional existe (`.docs/QA.md`, `.docs/ARCHITECTURE.md §6.3` REELIX-013).

## Contexto

A spec `12-mobile-widgets.md` pede widgets de episódios pendentes e próximas estreias, abrindo o título ou episódio por deep link, sem mostrar dados de outra conta após logout e sem tokens acessíveis ao widget. Widgets dependem de capacidades nativas e de build próprio: não funcionam no Expo Go. Neste ambiente não havia bundle identifier iOS, Xcode nem dispositivo iOS; havia um dispositivo Android (Samsung) para validação.

## Decisão

- **Escopo atual: só Android**, desenvolvido e validado no Samsung. **iOS está adiado** e fora dos critérios de aceite; só volta por nova decisão e com macOS/Xcode e dispositivo iOS disponíveis. `app.json` não define `ios.bundleIdentifier`.
- **Biblioteca candidata:** `expo-widgets` (SDK 58), segundo a documentação oficial consultada no spike (04/11/2026): exige development build, `android.package` e `enableAndroid`, roda em runtime isolado e recebe dados por snapshots/timelines, sem hooks nem chamadas de rede próprias. É candidata, não adotada: a escolha final depende de validar a compatibilidade.
- **Build e validação:** `android.package` está definido em `app.json`; `mobile/eas.json` tem os perfis `development` (`developmentClient`, distribuição interna, APK) e `preview` (APK interno), além de `production`. O build EAS de development concluiu e o usuário confirmou conexão ao Metro, login e navegação no Samsung. Esse teste **não** valida widgets, deep links nem push.
- **Privacidade do desenho (planejado, não implementado):** snapshots com dados mínimos do usuário atual, sem tokens, limpos no logout e na troca de conta; atualização conforme os limites do sistema operacional, sem serviço permanente em segundo plano; estado vazio e dados offline tratados.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Android e iOS ao mesmo tempo | Paridade entre plataformas | Sem macOS, Xcode, bundle identifier nem dispositivo iOS | `12-mobile-widgets.md`: decisão de 03/11/2026 de validar só Android |
| Módulo nativo próprio para widgets (inferido) | Controle total do widget | Código Kotlin/Swift a manter; sai do fluxo Expo | Inferido: a spec só pede avaliar se módulo nativo será necessário; `expo-widgets` é o candidato |
| Validar widgets no Expo Go | Sem build | Não suporta widgets | Spec 12: não presumir que widgets funcionam no Expo Go |
| Adiar widgets inteiros (inferido) | Foco em outros módulos | Funcionalidade pedida fica sem entrega | A etapa Android existe e é prioridade baixa no backlog (REELIX-013) |
| Serviço permanente em segundo plano para atualizar | Dados mais frescos | Bateria e rede; fora dos limites do SO | Spec 12: não implementar serviço permanente sem justificativa |

## Consequências

- Positivas: o escopo cabe no hardware disponível; a estratégia fica documentada antes de investir em código nativo; o requisito de privacidade (limpar no logout e na troca de conta) está definido antes da implementação.
- Negativas: usuários de iOS ficam sem widgets por tempo indefinido; a decisão pela biblioteca não está validada na prática; `adb` não está instalado na máquina de desenvolvimento; push e deep links no dispositivo também seguem sem validação; implementação precipitada pode expor dados da conta anterior em snapshots (`.docs/ARCHITECTURE.md §6.3`).
- Impacto em código, operação e time: nada a mudar no código hoje; a implementação (REELIX-013) toca `mobile/app.json`, `mobile/src/`, o wireframe `.docs/design/wireframes/reelix-visual-wireframes.html` e a spec 12. Reabrir iOS exige novo ADR ou atualização deste, com bundle identifier e hardware. O wireframe deve continuar marcando a lacuna sem simular um widget.

# Widgets mobile

## Escopo

Front-End mobile, integração com a API do Reelix e implementação nativa para as plataformas compatíveis.

## Contexto

O Reelix deverá oferecer widgets para que o usuário consulte informações úteis sem precisar abrir o aplicativo.

Os primeiros widgets deverão apresentar episódios pendentes e próximas estreias relevantes para o usuário.

A implementação depende das capacidades e restrições específicas de cada sistema operacional. A existência de uma API no backend não garante que o sistema operacional atualizará o widget imediatamente.

## Requisitos Funcionais

1. Avaliar e documentar as capacidades de widgets no iOS e Android para a versão do Expo e as bibliotecas escolhidas.
2. Definir quais widgets serão suportados em cada plataforma.
3. Criar um widget para exibir episódios pendentes, quando suportado.
4. Criar um widget para exibir próximas estreias relevantes, quando suportado.
5. Permitir que o usuário abra o Reelix ao tocar em um item do widget.
6. Direcionar a navegação para o título ou episódio correspondente.
7. Exibir um estado vazio quando não houver conteúdo relevante.
8. Exibir dados previamente sincronizados quando não houver conectividade.
9. Atualizar os dados conforme os mecanismos de atualização permitidos pela plataforma.
10. Evitar mostrar dados de outro usuário após logout ou troca de conta.
11. Respeitar as regras de privacidade dos dados exibidos.
12. Permitir que o usuário remova o widget pelo sistema operacional.
13. Tratar indisponibilidade dos dados do catálogo.
14. Não exibir informações de lançamento que não estejam disponíveis ou confirmadas.
15. Documentar as limitações de frequência de atualização.

## Requisitos Não Funcionais

- Minimizar o consumo de bateria.
- Minimizar uso de rede.
- Não armazenar tokens privados em locais acessíveis ao widget sem proteção adequada.
- Evitar dados pessoais desnecessários no armazenamento local.
- Respeitar os limites de atualização definidos pelo sistema operacional.
- Não depender de execução contínua em segundo plano.
- Tratar diferenças entre versões do iOS e Android.
- Garantir que a implementação escolhida seja compatível com o fluxo de build e distribuição do aplicativo.

## Você Deve

- Verificar a documentação atual do Expo e das plataformas antes de escolher a estratégia.
- Registrar as limitações técnicas encontradas.
- Definir se será necessário um módulo nativo, configuração especial de build ou biblioteca adicional.
- Compartilhar somente os dados mínimos necessários ao widget.
- Criar uma estratégia segura de sincronização.
- Limpar ou invalidar dados locais ao trocar de conta.
- Implementar deep links para abrir a tela correta do Reelix.
- Criar testes para navegação e tratamento de dados indisponíveis.
- Validar o comportamento em dispositivos ou simuladores compatíveis.
- Informar claramente quais funcionalidades não são suportadas em determinada plataforma.

## Você Não Deve

- Presumir que widgets funcionam integralmente no Expo Go.
- Garantir atualização em tempo real.
- Implementar um serviço permanente de segundo plano sem justificativa.
- Expor chaves de API ou tokens de autenticação no widget.
- Compartilhar informações privadas em armazenamento local sem proteção adequada.
- Criar uma implementação que dependa de capacidades inexistentes no sistema operacional.
- Implementar widgets antes de verificar os requisitos de build e distribuição.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- Expo Router
- APIs nativas do iOS e Android, conforme necessário
- Biblioteca de widgets compatível com a arquitetura aprovada
- ASP.NET Core
- C#
- PostgreSQL

A biblioteca e a estratégia nativa deverão ser selecionadas após a análise de compatibilidade. Não presumir que uma biblioteca específica atende às duas plataformas.

## Critérios de aceitação

- A estratégia técnica está documentada para cada plataforma suportada.
- O widget exibe os dados previstos quando disponíveis.
- O toque em um item abre o conteúdo correspondente no aplicativo.
- Dados indisponíveis são tratados sem falhas.
- A troca de conta não expõe informações do usuário anterior.
- O widget respeita as restrições de atualização do sistema operacional.
- A implementação é validada em ambiente compatível com os recursos nativos.
- As limitações de plataforma são documentadas.
- Os testes relevantes passam.

## Resultado do spike (atualizado em 02/11/2026)

A documentação oficial consultada identifica `expo-widgets` como candidato para iOS e Android no SDK 58. O módulo não funciona no Expo Go e exige development builds. A documentação também exige App Group/bundle identifier no iOS e `android.package` com `enableAndroid` no Android; o widget roda em runtime isolado, não usa hooks nem chamadas de rede próprias, e recebe dados via snapshots/timelines.

| Plataforma | Evidência disponível | Resultado |
|---|---|---|
| iOS | Documentação Expo/WidgetKit consultada; projeto sem `ios.bundleIdentifier`; ambiente Windows sem Xcode | Não validado; build, assinatura, App Group, renderização e deep link pendentes |
| Android | Documentação Expo/Android consultada; projeto sem `android.package`; ambiente sem Java, Android SDK/ADB ou Gradle | Não validado; build, instalação, launcher, renderização e deep link pendentes |

Nenhuma biblioteca foi instalada nem plugin/configuração nativa foi adicionada, pois os identificadores de distribuição não estão definidos e os builds compatíveis não podem ser executados neste ambiente. Não há widgets funcionais nem plataforma declarada como suportada. Próximo passo: definir bundle/package IDs e repetir o spike em ambiente com Xcode e Android SDK; só então aprovar snapshots, consentimento/limpeza por conta, dois layouts e navegação nativa.

**Estado atual:** pesquisa/documentação concluídas; widgets iOS e Android não implementados nem validados. O wireframe marca explicitamente esta lacuna e não simula a existência de um widget funcional.

Referências oficiais verificadas:

- [Expo Widgets SDK 58](https://docs.expo.dev/versions/latest/sdk/widgets/)
- [WidgetKit: keeping a widget up to date](https://developer.apple.com/documentation/widgetkit/keeping-a-widget-up-to-date)
- [Android App Widgets overview](https://developer.android.com/develop/ui/views/appwidgets/overview)

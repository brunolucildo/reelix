# Widgets mobile

## Escopo

Escopo atual: widgets Android, com desenvolvimento e validação em um dispositivo Samsung. iOS está explicitamente adiado e não é requisito de aceite desta etapa; só volta ao escopo mediante decisão futura e disponibilidade de macOS/Xcode e dispositivo iOS.

## Contexto

O Reelix deverá oferecer widgets para que o usuário consulte informações úteis sem precisar abrir o aplicativo.

Os primeiros widgets deverão apresentar episódios pendentes e próximas estreias relevantes para o usuário.

A implementação depende das capacidades e restrições específicas de cada sistema operacional. A existência de uma API no backend não garante que o sistema operacional atualizará o widget imediatamente.

## Requisitos Funcionais

1. Avaliar e documentar as capacidades de widgets Android para a versão do Expo e as bibliotecas escolhidas.
2. Definir os widgets suportados no Android; registrar iOS como adiado.
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
- Tratar diferenças entre versões e fabricantes Android; iOS permanece fora do escopo atual.
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
- Validar o comportamento em dispositivos Android compatíveis; validação iOS não é exigida nesta etapa.
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
- APIs nativas Android, conforme necessário
- Biblioteca de widgets compatível com a arquitetura aprovada
- ASP.NET Core
- C#
- PostgreSQL

A biblioteca e a estratégia nativa deverão ser selecionadas após a análise de compatibilidade. Não presumir que uma biblioteca específica atende às duas plataformas.

## Critérios de aceitação

- A estratégia técnica Android está documentada; iOS está explicitamente adiado.
- O widget exibe os dados previstos quando disponíveis.
- O toque em um item abre o conteúdo correspondente no aplicativo.
- Dados indisponíveis são tratados sem falhas.
- A troca de conta não expõe informações do usuário anterior.
- O widget respeita as restrições de atualização do sistema operacional.
- A implementação é validada em ambiente compatível com os recursos nativos.
- A implementação Android é validada em ambiente compatível com os recursos nativos e em dispositivo real.
- As limitações de plataforma são documentadas.
- As limitações Android são documentadas; iOS não bloqueia o aceite desta etapa.
- Os testes relevantes passam.

## Resultado do spike (atualizado em 04/11/2026)

A documentação oficial consultada identifica `expo-widgets` como candidato Android no SDK 58; não funciona no Expo Go e exige development build. No Android, requer `android.package` e `enableAndroid`; o widget roda em runtime isolado e recebe dados via snapshots/timelines, sem hooks nem chamadas de rede próprias. A decisão de produto em 03/11/2026 é desenvolver e validar somente Android nesta etapa; iOS foi adiado.

| Plataforma | Evidência disponível | Resultado |
|---|---|---|
| iOS | Sem bundle identifier, Xcode ou dispositivo iOS neste ambiente | Adiado; fora do escopo e dos critérios de aceite atuais |
| Android | `android.package` definido como `com.brunolucildo.reelix`; projeto EAS vinculado; perfis development/preview configurados para APK interno | Build EAS `b0c790c9-bc1c-4c76-82fa-33bf317c92b6` concluído; usuário confirmou conexão ao Metro, login e navegação. Widget ainda não implementado |

`expo-widgets` ainda não foi instalado e não há widget funcional. O application ID e os perfis de build Android estão configurados, e o development APK do commit atual concluiu. O usuário confirmou conexão ao Metro, login e navegação no Samsung. Esse smoke test não valida widgets, deep links nem push; testar essas capacidades ao implementar a integração Android. Não é necessário configurar iOS para concluir esta etapa.

**Estado atual em 04/11/2026:** iOS está adiado. O APK de development do commit atual concluiu; o usuário confirmou conexão ao Metro, login e navegação no Samsung. Widgets ainda não foram implementados nem validados. `adb` não está disponível nesta máquina. O wireframe continua marcando a lacuna e não simula a existência de widget funcional.

Referências oficiais verificadas:

- [Expo Widgets SDK 58](https://docs.expo.dev/versions/latest/sdk/widgets/)
- [WidgetKit: keeping a widget up to date](https://developer.apple.com/documentation/widgetkit/keeping-a-widget-up-to-date)
- [Android App Widgets overview](https://developer.android.com/develop/ui/views/appwidgets/overview)

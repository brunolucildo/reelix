# Reelix mobile

Aplicativo mobile React Native + Expo, com navegação por Expo Router e autenticação pelo Supabase Auth. A documentação canônica do projeto está em [`../.docs/`](../.docs/); o guia para agentes é [`AGENTS.md`](AGENTS.md).

## Identidade visual

Os logotipos, ícones vetoriais, badges e ícones de plataforma ficam em [`assets/brand/`](assets/brand/). Veja o [catálogo, paleta e orientações de uso](assets/brand/README.md). O kit segue o mood board [`visual_identity.png`](../.docs/design/visual_identity.png); `npm run brand:generate` exporta os PNGs de plataforma a partir dos SVGs.

Os tokens de cor, espaçamento e tipografia ficam em [`src/theme/tokens.ts`](src/theme/tokens.ts). Regras de design: [`DESIGN.md`](../.docs/DESIGN.md) e [`DESIGN_SYSTEM.md`](../.docs/DESIGN_SYSTEM.md).

## Feedback ao usuário

Use `useToast().showError` ou `useErrorToast(error, mensagem)` de [`src/providers/toast-provider.tsx`](src/providers/toast-provider.tsx) para falhas de API, carregamento e salvamento. Mantenha inline a validação por campo, os avisos de limite e as mensagens de sucesso.

## Configuração local

Siga a seção de ambiente de [`../.docs/QA.md`](../.docs/QA.md) (§2 e §4) para configurar Supabase, deep links, variáveis locais e a API.

```powershell
Copy-Item .env.example .env
npm install
npx expo start
```

Configure `mobile/.env` antes de iniciar o app (`EXPO_PUBLIC_SUPABASE_URL`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` e `EXPO_PUBLIC_API_URL`). As chaves privadas do Supabase e a `service_role` não pertencem ao cliente.

## Verificações

```powershell
npm run lint
npx tsc --noEmit
```

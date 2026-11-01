# Reelix mobile

Aplicativo mobile React Native + Expo, com navegação por Expo Router e autenticação pelo Supabase Auth.

## Identidade visual

Os logotipos, ícones vetoriais, badges e ícones de plataforma ficam em [`assets/brand/`](assets/brand/). Veja o [catálogo, paleta e orientações de uso](assets/brand/README.md). O kit segue a referência [`docs/visual_identity.png`](../docs/visual_identity.png); `npm run brand:generate` exporta os PNGs de plataforma a partir dos SVGs.

Os tokens de cor, espaçamento e tipografia ficam em [`src/theme/tokens.ts`](src/theme/tokens.ts).

## Feedback ao usuário

Use `useToast().showError` ou `useErrorToast(error, mensagem)` de [`src/providers/toast-provider.tsx`](src/providers/toast-provider.tsx) para falhas de API, carregamento e salvamento. Mantenha inline a validação por campo, os avisos de limite e as mensagens de sucesso.

## Configuração local

Siga [`docs/authentication-setup.md`](../docs/authentication-setup.md) para configurar Supabase, deep links, variáveis locais e a API.

```powershell
Copy-Item .env.example .env
npm install
npx expo start
```

Configure `mobile/.env` antes de iniciar o app. As chaves privadas do Supabase e a `service_role` não pertencem ao cliente.

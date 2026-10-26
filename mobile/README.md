# Reelix mobile

Aplicativo mobile React Native + Expo, com navegação por Expo Router e autenticação pelo Supabase Auth.

## Configuração local

Siga [`docs/authentication-setup.md`](../docs/authentication-setup.md) para configurar Supabase, deep links, variáveis locais e a API.

```powershell
Copy-Item .env.example .env
npm install
npx expo start
```

Configure `mobile/.env` antes de iniciar o app. As chaves privadas do Supabase e a `service_role` não pertencem ao cliente.

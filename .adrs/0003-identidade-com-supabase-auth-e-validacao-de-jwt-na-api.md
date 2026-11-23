# 0003 — Identidade com Supabase Auth e validação de JWT na API

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

O produto precisa de cadastro, login, confirmação de e-mail, recuperação de senha e logout, e a API precisa saber quem é o usuário em cada request. A spec `.specs/0001-authentication.md` proíbe armazenar senhas no banco do Reelix e proíbe autenticação paralela com ASP.NET Identity. `.docs/ARCHITECTURE.md` §8 manda usar uma solução de identidade reconhecida e evitar autenticação de senha própria. O app é nativo (ADR 0002), então os fluxos usam deep link (`reelix://auth/callback`).

## Decisão

- **Supabase Auth** é o provedor de identidade. O app usa `@supabase/supabase-js` com PKCE e `autoRefreshToken`; a sessão fica no `expo-secure-store` em iOS/Android (e em `localStorage` na web, que não é limite de segurança). Não há ASP.NET Core Identity.
- A **API só valida tokens**: `AddJwtBearer` em `Program.cs` com `Authority` = `{Supabase:Url}/auth/v1` (chaves públicas por metadados/JWKS), emissor e audiência (`authenticated` por padrão) validados, `ClockSkew` de 30 s e algoritmos ES256, RS256 e HS256. `Supabase:JwtSecret` é um segredo legado opcional, só no backend, para projetos que ainda emitem HS256. `Supabase:Url` é obrigatória na partida e precisa ser HTTPS (HTTP só em Development).
- **`sub` ↔ conta interna:** o `sub` validado é o único identificador de identidade; `GET /api/v1/auth/me` chama `GetAuthenticatedAccount`, que localiza ou cria o `UserAccount` por `supabase_user_id` (índice único; a corrida de criação trata a violação de unicidade). O cliente nunca envia o ID da conta.
- **`service_role` nunca vai ao app:** só a chave publishable (pública) compõe o bundle (`mobile/.env.example`); nenhuma referência a `service_role` existe em `mobile/src` ou `Reelix.Api`.
- **Só em Development:** `dev-auth.html` (em `wwwroot`, servido via `UseStaticFiles`), `/api/v1/dev/auth/config`, Swagger UI e OpenAPI são mapeados dentro de `if (app.Environment.IsDevelopment())`.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| ASP.NET Core Identity | Integrado ao framework | Armazenaria senhas no banco do Reelix; segundo sistema de identidade | `01-authentication.md` proíbe; `architecture.md` §8 e `technology-stack-evaluation.md` desaconselham |
| Autenticação própria com hash de senha | Controle total | Segurança de credenciais passa a ser do time | `01-authentication.md`: "Criar armazenamento próprio de senhas" é proibido |
| API só decodifica o JWT, sem validar assinatura | Menos configuração | Qualquer token forjado passaria | `01-authentication.md`: "Não confiar apenas na decodificação de um JWT" |
| Aceitar o ID do usuário enviado pelo cliente | Simples | Permite se passar por outro usuário | `01-authentication.md`: critério de aceite proíbe |

## Consequências

- Positivas: nenhuma senha no banco ou na API; fluxos de e-mail e recuperação prontos; a API é stateless e valida cada token localmente; o isolamento por conta depende só do `sub` verificado. `AuthenticationEndpointTests` cobre emissor errado, audiência errada, token expirado e assinatura inválida, além de rejeição anônima em 32 rotas.
- Negativas: dependência de um provedor externo (disponibilidade, limites de e-mail e configuração de **Site URL**, allowlist de callback e template de reset por ambiente); a API não consulta sessão, então um token válido continua aceito até expirar; exclusão e exportação de conta não têm fluxo definido (`.docs/QA.md`); o callback de cadastro ainda não foi validado em dispositivo.
- Impacto em código, operação e time: configuração do projeto Supabase documentada em `.specs/0001-authentication.md`; variáveis `Supabase__Url`, `Supabase__Audience` e `Supabase__JwtSecret` só no ambiente da API; no mobile, `EXPO_PUBLIC_SUPABASE_URL`, `EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY` e `EXPO_PUBLIC_API_URL`. O mobile envia `Authorization: Bearer` a cada chamada (`mobile/src/services/api-client.ts`).

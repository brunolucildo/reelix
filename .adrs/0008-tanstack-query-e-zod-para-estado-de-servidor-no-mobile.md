# 0008 — TanStack Query e Zod para estado de servidor no mobile

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

O app consome uma API REST própria (ADR 0001) com dezenas de endpoints e precisa de cache, invalidação após mutações, paginação e estados de carregamento e erro consistentes. `.docs/ARCHITECTURE.md` §3 e §15 fixa TanStack Query para estado de servidor e Zod para validar dados externos e formulários, e proíbe duplicar dados do servidor num store global sem motivo. §2 e §4 tratam a geração de cliente TypeScript a partir do OpenAPI como "opção futura", não como etapa de build existente. Os padrões de UX (§3) exigem erros de API em toast e validação de campo inline.

## Decisão

- **TanStack Query** (`@tanstack/react-query` ^5) para todo estado de servidor: `QueryClient` único em `mobile/src/app/_layout.tsx` com `retry: 1` e `staleTime: 30_000`; chaves por domínio (`['gamification', 'dashboard']`, `['notification-inbox']`...); mutações invalidam as chaves afetadas. Estado de UI temporário fica em React state.
- **Toda resposta da API é validada com Zod:** cada função de `mobile/src/services/api-client.ts` (e de `tracking-api.ts` e `lists-api.ts`) recebe um schema Zod escrito à mão, faz `safeParse` do corpo e, se não casar, lança "A API retornou dados em formato inesperado." Os tipos TypeScript são derivados dos schemas com `z.infer`; **não há geração de código a partir do OpenAPI**.
- **Cliente HTTP mínimo:** `fetch` com `Authorization: Bearer` obtido da sessão Supabase (ADR 0003); erro HTTP usa `detail` ou `title` do `ProblemDetails` (em português, `architecture.md` §4) e anexa o `status`.
- **Formulários:** React Hook Form com `zodResolver` e schemas em `mobile/src/schemas/auth-forms.ts`.
- **Feedback de erro:** falhas de API, rede e carregamento vão para toast (`useToast().showError` ou `useErrorToast`, `mobile/src/providers/toast-provider.tsx`); validação de campo, limites de seleção e confirmações de sucesso ficam inline, junto ao controle. Tela bloqueada mostra toast e um estado de erro com "Tentar novamente".

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Cliente gerado do OpenAPI | Contrato único, sem schema à mão | Etapa de build e configuração a mais; hoje o OpenAPI só existe em Development | `architecture.md` §2: geração é opção futura, não etapa existente |
| Confiar nos tipos sem validar a resposta (inferido) | Menos código | Contrato quebrado vira erro obscuro na tela | Inferido: `architecture.md` §3 manda validar dados externos com Zod |
| Store global (Redux, Zustand) para dados do servidor | Um lugar para tudo | Duplica cache e invalidação | `architecture.md` §3: não duplicar dados do servidor em store global |
| Erros de API inline em cada tela | Contexto local | Mensagens e posição diferentes por tela | `architecture.md` §3: erros de API só em toast; validação de campo inline |
| Biblioteca de requisição (axios etc.) (inferido) | Interceptors prontos | Dependência para o que `fetch` já faz | Inferido: nada nas fontes; o helper tem poucas linhas |

## Consequências

- Positivas: resposta fora do contrato falha de forma previsível e traduzida em vez de quebrar a renderização; cache e invalidação centralizados; tipos sempre iguais ao schema validado; UX de erro uniforme.
- Negativas: os schemas são mantidos à mão e podem divergir da API (risco de contrato, sem teste de contrato automatizado); `api-client.ts` tem 848 linhas e há três cópias do helper de requisição (`apiRequest`, `trackingRequest` e o de `lists-api.ts`); a validação roda a cada resposta; não há testes automatizados no mobile (`.docs/QA.md`); campos novos na API exigem atualizar o app junto (`09-social.md`, perfil contextual).
- Impacto em código, operação e time: toda função de serviço nova precisa de schema Zod, chave de query e invalidação; erros usam `useErrorToast`. Reavaliar geração por OpenAPI quando o contrato estabilizar e houver CI para isso (novo ADR).

## Atualização (2026-11-23, spec `0017`)

Atualizado pelo ADR `XXXX-0017` (`.adrs/XXXX-0017-cliente-http-unico-401-global-e-fabricas-de-chave.md`; número provisório até a integração). O que muda em relação ao texto acima, sem revogar a decisão original:

- O cliente HTTP deixa de ter três cópias: passa a ser `mobile/src/lib/http-client.ts` (`httpRequest`), com `ApiError` tipado (`status`, `title`, `detail`) em vez de `Object.assign(new Error(...), { status })`.
- O `QueryClient` é criado por `createQueryClient` (`lib/query/create-query-client.ts`) e trata 401 globalmente (sessão local encerrada, cache limpo, um toast, sem retry de 401); `retry: 1` e `staleTime` de 30 s seguem para os demais erros.
- As funções de serviço vivem em `mobile/src/features/<modulo>/api/` (`services/api-client.ts` é um barrel temporário) e as chaves de query são fábricas em `features/<modulo>/query-keys.ts`.
- As consequências negativas citadas ("`api-client.ts` tem 848 linhas" e "três cópias do helper") deixam de valer. Continuam valendo: schemas à mão sem teste de contrato e ausência de testes automatizados no mobile.


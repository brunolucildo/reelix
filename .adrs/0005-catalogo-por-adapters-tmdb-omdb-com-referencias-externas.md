# 0005 — Catálogo por adapters TMDB/OMDb com referências externas

- **Status:** aceito
- **Data:** 2026-10-26 (aproximada, pelo histórico git)
- **Origem:** registro retroativo de decisão já implementada

## Contexto

Tracking, listas, avaliações, social e gamificação precisam referenciar filmes, séries e episódios de forma estável, mas os metadados e as imagens pertencem a provedores externos com termos próprios (`.specs/0003-catalog.md`, `.docs/ARCHITECTURE.md` §7). A spec pede abstração sobre os provedores, ID interno estável, origem preservada, sem inventar dados ausentes e sem cache fora das condições permitidas. A API TMDB gratuita é para uso não comercial e exige atribuição; a OMDb não expõe imagens (`.specs/0003-catalog.md`).

## Decisão

- **Adapters atrás de uma porta:** `ICatalogProvider` (em `Application`) é implementada por `TmdbCatalogProvider` e `OmdbCatalogProvider` (em `Infrastructure`, `HttpClient` com timeout de 10 s). `CatalogService` escolhe o provedor pedido; **não há fallback automático** entre eles. O TMDB é o provedor principal (gêneros e descoberta só nele); o OMDb é opcional.
- **IDs internos estáveis:** a tabela `catalog_external_references` guarda `provider`, `external_id` e `content_type` com índice único (`UX_catalog_external_references_external_key`) e gera o GUID interno usado por todo o resto (`ExternalCatalogReferenceRepository.GetOrCreateIdsAsync`, que trata a corrida de criação pela violação de unicidade). O ID de um provedor nunca é a única identidade interna.
- **Sem cópia integral de metadados:** busca e detalhes são consultados ao vivo e não são gravados. Além do identificador, o banco guarda só dados derivados mínimos: `runtime_minutes` e vínculos de gênero (`CatalogReferenceGenre`) e de episódio com série (`CatalogEpisodeSeries`). Caches são temporários e em memória: descoberta por 10 min (`IMemoryCache`, limite de 200 entradas) e gêneros TMDB por 6 h.
- **Credenciais só no servidor:** `Catalog:Tmdb:AccessToken` e `Catalog:Omdb:ApiKey` por User Secrets ou ambiente; credencial ausente gera 503 explícito só para aquele provedor.
- **Atribuição TMDB:** a tela de perfil exibe o logo do TMDB e o aviso "This product uses the TMDB API but is not endorsed or certified by TMDB" (`mobile/src/app/profile.tsx`); a resposta do backend identifica a origem.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que não |
| --- | --- | --- | --- |
| Espelhar o catálogo no banco | Consultas rápidas e independência do provedor | Retenção e redistribuição sem direito comprovado; sincronização | `catalog-setup.md`: reavaliar termos e direitos de imagem antes de qualquer cache ou cópia |
| Usar o ID do provedor como identidade interna | Sem tabela de referência | Trocar de provedor quebraria histórico e listas | `architecture.md` §6 e §7: dados do usuário independentes do provedor |
| Fallback automático TMDB → OMDb | Mais disponibilidade | Campos e IDs diferentes entre provedores; risco de dado inventado | `catalog-setup.md`: "Não há fallback automático entre provedores" |
| Chamar os provedores direto do app | Menos código no backend | Expõe credenciais | Spec 03: chaves nunca no app |
| Raspar serviços de streaming | Disponibilidade por plataforma | Fora dos termos de uso | `architecture.md` §7 proíbe |

## Consequências

- Positivas: tracking, listas e feed referenciam títulos por GUID interno, sem depender do formato de nenhum provedor; trocar ou acrescentar provedor é implementar a porta; nenhum snapshot de terceiros para licenciar.
- Negativas: cada tela de catálogo depende da disponibilidade e do limite do provedor (erros 429/502/503/504 como `ProblemDetails`); progresso de série depende do inventário de temporadas do provedor (`05-tracking.md`); gamificação sofre com cobertura incompleta de duração e gênero; o TMDB gratuito é não comercial e a licença comercial precisa ser confirmada (decisão pendente no backlog); o OMDb não oferece imagens.
- Impacto em código, operação e time: módulo `Catalog` nos quatro projetos; credenciais por ambiente (`.specs/0003-catalog.md`); testes com respostas simuladas (`CatalogProviderTests`, `CatalogServiceTests`). Qualquer cache persistente ou cópia de metadados exige novo ADR.

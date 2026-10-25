# Catálogo

## Escopo

Back-End, integração com provedores externos, persistência de referências de catálogo e contratos consumidos pelo aplicativo mobile.

## Contexto

O Reelix precisa consultar filmes, séries, temporadas, episódios e informações de lançamento.

O TMDB será o provedor principal proposto para essas informações, complementado pela OMDb quando houver necessidade concreta.

A aplicação deverá manter uma camada de abstração para que as regras de negócio não dependam diretamente dos formatos de resposta dos provedores externos.

## Requisitos Funcionais

1. Criar uma abstração para consultar dados de catálogo.
2. Implementar um adaptador para o TMDB.
3. Implementar um adaptador para a OMDb, utilizando somente operações documentadas e permitidas pelo serviço.
4. Permitir pesquisar filmes e séries.
5. Permitir consultar detalhes de um filme.
6. Permitir consultar detalhes de uma série.
7. Permitir consultar temporadas e episódios de uma série.
8. Permitir consultar datas de lançamento disponíveis.
9. Permitir consultar gêneros e metadados necessários para a interface.
10. Normalizar os dados recebidos para um modelo interno consistente.
11. Identificar cada título pelo provedor e pelo identificador externo correspondente.
12. Evitar duplicação de registros para o mesmo título e provedor.
13. Tratar títulos não encontrados, respostas incompletas, limites de requisição e indisponibilidade dos provedores.
14. Permitir paginação quando suportada pelo provedor.
15. Expor endpoints próprios do Reelix para pesquisa e consulta de catálogo.
16. Manter a origem dos dados externos identificável.
17. Implementar cache somente conforme as condições permitidas pelos provedores.
18. Permitir que os módulos de Tracking, Avaliações, Listas e Home referenciem títulos de maneira estável.
19. Normalizar fusos horários e datas sem inventar informações ausentes.
20. Tratar metadados não disponíveis sem interromper a experiência inteira.

### Contratos internos esperados

Os modelos normalizados devem contemplar, quando disponíveis:

- Identificador interno.
- Provedor e identificador externo.
- Tipo de conteúdo: filme ou série.
- Título e título original.
- Sinopse.
- Gêneros.
- Data de lançamento ou estreia.
- Imagem de pôster e imagem de fundo.
- Idioma original.
- Duração, quando aplicável.
- Temporadas e episódios, para séries.
- Datas de lançamento de episódios.
- Metadados de origem.

Os campos opcionais deverão permanecer opcionais quando o provedor não fornecer a informação.

## Requisitos Não Funcionais

- Proteger as credenciais dos provedores.
- Respeitar limites de requisição e condições contratuais.
- Aplicar timeout e tratamento de falhas de rede.
- Evitar consultas redundantes.
- Utilizar cache apenas quando autorizado.
- Registrar erros de integração sem incluir credenciais.
- Manter os adaptadores isolados das regras de negócio.
- Evitar dependência obrigatória de um único provedor quando houver alternativa viável.
- Não garantir atualização em tempo real sem suporte técnico do provedor.

## Você Deve

- Consultar a documentação oficial atual do TMDB e da OMDb antes de implementar cada integração.
- Utilizar variáveis de ambiente para as credenciais.
- Criar DTOs específicos para respostas externas e modelos internos normalizados.
- Implementar mapeamentos explícitos entre os provedores e o modelo do Reelix.
- Criar testes com respostas simuladas.
- Tratar rate limits e indisponibilidade de forma previsível.
- Documentar as regras de cache, atribuição e armazenamento aplicáveis.
- Garantir que um erro de um provedor não seja convertido em um título fictício.
- Definir índices para os identificadores externos utilizados nas consultas.
- Preservar a origem dos dados.
- Verificar os direitos de uso das imagens antes de armazená-las ou redistribuí-las.

## Você Não Deve

- Inventar endpoints ou parâmetros das APIs externas.
- Expor as chaves do TMDB ou da OMDb no aplicativo.
- Presumir que todas as informações estarão disponíveis nos dois provedores.
- Tratar a resposta de um provedor como se todos os campos fossem obrigatórios.
- Criar microserviços separados para os provedores.
- Armazenar indefinidamente todos os dados externos sem verificar as condições de uso.
- Substituir informações ausentes por dados inventados.
- Fazer chamadas externas desnecessárias a cada renderização de tela.

## Tecnologias (Stack)

- ASP.NET Core
- C#
- HttpClient e IHttpClientFactory
- PostgreSQL
- Entity Framework Core
- Npgsql
- TMDB API
- OMDb API
- xUnit
- Testes de integração

## Critérios de aceitação

- O backend consegue pesquisar filmes e séries por meio do provedor configurado.
- Os detalhes disponíveis podem ser consultados por endpoints próprios do Reelix.
- Temporadas, episódios e datas são retornados quando suportados pelo provedor.
- Os dados externos são normalizados para contratos internos.
- A origem e o identificador externo de cada título são preservados.
- Títulos inexistentes não são inventados.
- Falhas de integração e limites de requisição são tratados.
- As credenciais não aparecem nas respostas ou nos logs.
- As políticas de cache e atribuição aplicáveis são documentadas.
- Os testes com respostas simuladas passam.

## Resultado

Camada de catálogo implementada, com integrações isoladas, dados normalizados e contratos reutilizáveis pelos módulos do Reelix.

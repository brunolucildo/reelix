# Perfil

## Escopo

Front-End, Back-End, persistência de dados e integração com armazenamento de imagens, caso aprovado para o projeto.

## Contexto

O perfil representa a identidade do usuário dentro do Reelix.

Deverá permitir editar informações básicas, gerenciar o avatar, consultar estatísticas disponíveis e controlar preferências de privacidade.

O perfil não deverá assumir que todas as informações do usuário são públicas.

## Requisitos Funcionais

1. Criar uma página de perfil para o próprio usuário.
2. Permitir consultar nome de exibição, biografia e avatar.
3. Permitir editar os campos de perfil autorizados.
4. Permitir selecionar ou substituir o avatar.
5. Permitir remover o avatar personalizado.
6. Disponibilizar um avatar padrão quando o usuário não possuir imagem.
7. Permitir consultar preferências pessoais.
8. Disponibilizar configurações de privacidade.
9. Permitir consultar estatísticas fornecidas pelo módulo responsável.
10. Exibir as conquistas que o módulo de Gamificação disponibilizar.
11. Permitir que o usuário visualize quais informações são compartilhadas.
12. Permitir editar as informações do perfil sem afetar a identidade de autenticação.
13. Exibir estados de carregamento, erro e sucesso.
14. Respeitar as regras de privacidade ao consultar perfis de outras pessoas.
15. Permitir acesso ao fluxo de exclusão da conta, caso seja disponibilizado conforme a política do produto.

## Requisitos Não Funcionais

- Validar entradas no servidor.
- Limitar o tamanho da biografia e dos demais campos textuais.
- Validar tamanho, formato e tipo de arquivo do avatar.
- Não permitir que arquivos enviados sejam tratados como confiáveis sem validação.
- Proteger informações pessoais.
- Aplicar regras de acesso no backend.
- Evitar armazenar arquivos de imagem diretamente no banco sem justificativa.
- Não expor URLs privadas de imagens ou credenciais de armazenamento.

## Você Deve

- Separar o perfil interno da identidade gerenciada pelo Supabase Auth.
- Definir os campos editáveis e seus limites.
- Criar endpoints para consultar e atualizar o perfil.
- Reutilizar o módulo de preferências para os dados de gosto pessoal.
- Integrar com o armazenamento de imagens apenas após definir o serviço e as regras de acesso.
- Permitir avatares padrão sem exigir upload.
- Aplicar as configurações de privacidade nas respostas da API.
- Criar testes para atualização e autorização.
- Documentar o comportamento de exclusão e retenção de dados pessoais.

## Você Não Deve

- Armazenar senhas ou tokens de autenticação no perfil.
- Permitir que usuários editem perfis alheios sem autorização.
- Expor e-mail ou outras informações pessoais em consultas públicas por padrão.
- Tornar o histórico público automaticamente.
- Implementar amizade ou seguidores neste módulo.
- Criar estatísticas independentes que contradigam o Tracking.
- Inventar um serviço de armazenamento ou presumir credenciais disponíveis.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- TanStack Query
- ASP.NET Core
- C#
- PostgreSQL
- Entity Framework Core
- Supabase Auth
- Serviço de armazenamento de imagens a definir

## Critérios de aceitação

- O usuário consegue consultar e editar seu perfil.
- O avatar pode ser atualizado, removido ou substituído por um padrão.
- Campos inválidos são rejeitados.
- O perfil não permite alterar a identidade de autenticação.
- As regras de privacidade são respeitadas pelo backend.
- Informações pessoais não são expostas sem autorização.
- Os dados são persistidos corretamente.
- Os testes de validação e autorização passam.

## Resultado

Módulo de perfil funcional, com edição de dados pessoais, avatar e configurações de privacidade.

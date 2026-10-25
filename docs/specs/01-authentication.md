# Autenticação

## Escopo

Front-End, Back-End, integração com Supabase Auth e banco de dados PostgreSQL.

## Contexto

Precisamos implementar os fluxos de autenticação do Reelix para permitir que os usuários criem suas contas, acessem a plataforma e recuperem o acesso quando necessário.

O Supabase Auth será responsável pela autenticação. O backend ASP.NET Core será responsável pela validação dos tokens e pela autorização das operações da aplicação.

A aplicação não deverá implementar um sistema próprio de armazenamento de senhas.

## Requisitos Funcionais

1. Implementar uma página de cadastro com e-mail e senha.
2. Implementar uma página de login com e-mail e senha.
3. Validar os formulários no cliente antes de enviar as solicitações.
4. Utilizar o SDK oficial do Supabase para os fluxos de autenticação suportados.
5. Tratar os estados de carregamento, sucesso e erro dos formulários.
6. Exibir mensagens genéricas de erro de autenticação, sem revelar desnecessariamente se determinado e-mail existe.
7. Implementar logout e limpar os dados de sessão mantidos no cliente conforme as capacidades do SDK.
8. Implementar recuperação de senha utilizando o fluxo do Supabase Auth.
9. Implementar confirmação de e-mail quando essa opção estiver habilitada no projeto.
10. Restaurar a sessão válida quando o usuário reabrir o aplicativo.
11. Encaminhar usuários não autenticados para as telas públicas apropriadas.
12. Proteger as telas privadas do aplicativo.
13. Validar os tokens recebidos nos endpoints protegidos do backend.
14. Criar ou localizar o registro interno do usuário na primeira autenticação válida.
15. Tratar contas desabilitadas, tokens expirados e sessões inválidas.
16. Disponibilizar um endpoint protegido para consultar o estado básico da conta autenticada.
17. Não exigir conclusão do onboarding para autenticar. O onboarding será tratado pelo módulo próprio.

## Requisitos Não Funcionais

- Não armazenar senhas na base de dados do Reelix.
- Não expor chaves privadas do Supabase no aplicativo.
- Não utilizar a chave `service_role` no mobile.
- Validar criptograficamente os tokens no backend, verificando assinatura, emissor, audiência e validade conforme a configuração do provedor.
- Não confiar apenas na decodificação de um JWT.
- Armazenar informações de sessão de acordo com as recomendações de segurança da plataforma.
- Utilizar HTTPS em ambientes não locais.
- Aplicar limites de requisições em operações sensíveis.
- Não registrar senhas, tokens ou dados sensíveis nos logs.
- Manter mensagens de erro consistentes e compreensíveis.

## Você Deve

- Utilizar Supabase Auth como provedor de identidade.
- Implementar telas acessíveis de cadastro, login e recuperação de senha.
- Criar middleware ou configuração de autenticação no ASP.NET Core.
- Associar o identificador estável do usuário no Supabase ao registro interno do Reelix.
- Aplicar autorização no backend, não apenas na navegação do aplicativo.
- Criar testes de autenticação e autorização.
- Documentar as variáveis de ambiente necessárias.
- Tratar adequadamente erros de rede e indisponibilidade do provedor.
- Garantir que o logout não deixe a interface aparentando estar autenticada.
- Manter os fluxos de autenticação independentes das regras específicas dos demais módulos.

## Você Não Deve

- Implementar autenticação paralela usando ASP.NET Identity.
- Criar armazenamento próprio de senhas.
- Inventar endpoints do Supabase.
- Confiar em identificadores de usuário enviados pelo cliente para definir a identidade autenticada.
- Armazenar tokens em locais inseguros.
- Expor informações que permitam enumerar contas.
- Implementar funcionalidades de perfil, amigos ou preferências neste módulo.
- Permitir acesso a dados privados apenas porque a tela correspondente está protegida no frontend.

## Tecnologias (Stack)

- React Native
- Expo
- TypeScript
- Expo Router
- Supabase Auth e SDK oficial
- ASP.NET Core
- C#
- JWT Bearer Authentication
- PostgreSQL
- Entity Framework Core
- xUnit

## Critérios de aceitação

- Um usuário consegue criar uma conta.
- Um usuário consegue autenticar e encerrar a sessão.
- A recuperação de senha segue o fluxo configurado no Supabase.
- Sessões válidas podem ser restauradas de forma segura.
- Tokens inválidos, expirados ou adulterados são rejeitados pela API.
- Endpoints protegidos rejeitam solicitações não autenticadas.
- Um usuário não consegue se passar por outro alterando um identificador enviado pelo cliente.
- O aplicativo trata erros de autenticação e indisponibilidade de rede.
- Nenhuma chave privada é incluída no aplicativo.
- Os testes automatizados relevantes passam.

## Resultado

Fluxos de autenticação implementados e integrados ao backend, com sessões tratadas de forma segura e identidade consistente para os demais módulos do Reelix.

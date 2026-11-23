# Açaí Galáctico

Sistema completo para uma açaiteria: **site para clientes**, **painel administrativo**, **API REST** e **aplicativo desktop** para a operação da loja, todos compartilhando o mesmo domínio e banco de dados.

🌐 **Site publicado:** [acaigalactico.runasp.net](https://acaigalactico.runasp.net)

## Funcionalidades

**Site (clientes)**
- Página inicial e cardápio com conteúdo editável pelo painel
- Montagem do pedido (tipo, tamanho e adicionais), carrinho e finalização
- Pagamento **simulado** por cartão ou Pix (nenhum dado de cartão é armazenado ou enviado a terceiros)
- Cadastro, login e recuperação de senha com ASP.NET Identity
- Perfil com foto, endereço e histórico de pedidos
- Formulário de contato com envio de e-mail via SendGrid

**Painel administrativo** (restrito ao perfil `Admin`)
- Dashboard com métricas de vendas, clientes e faturamento, e gráfico configurável
- Cadastro e edição de pedidos, produtos e clientes
- Gerenciamento de usuários
- Edição do conteúdo do site e do cardápio, com upload de imagens

**API REST**
- Endpoints de produtos, categorias, clientes e pedidos
- Documentação interativa com OpenAPI/Swagger (NSwag)
- Protegida por chave (`X-Api-Key`)

**Aplicativo desktop (Windows Forms)**
- Login, listagem e edição de pedidos e controle de vendas
- Consome a API REST por `HttpClient`

## Arquitetura

O projeto segue uma arquitetura em camadas, inspirada em Clean Architecture:

```
acaigalatico.Domain           Entidades e interfaces dos repositórios
acaigalatico.Application      Regras de negócio (services), DTOs e interfaces
acaigalatico.Infrastructure   Entity Framework Core, repositórios, migrations e seed
acaigalatico.Web              Site e painel (ASP.NET Core MVC + Razor Pages + Identity)
AcaiGalatico.API              API REST (ASP.NET Core Web API + NSwag)
acaiGalatico.UI               Aplicativo desktop (Windows Forms + Guna UI)
```

```
 Site (MVC) ─┐
             ├──> Application ──> Domain
 API REST ───┘         │
     ▲                 └──> Infrastructure ──> SQLite
     │
 App desktop (HttpClient)
```

## Tecnologias

- **C# / .NET 10**
- **ASP.NET Core MVC** e **Razor Pages**
- **ASP.NET Core Identity** (autenticação e autorização por perfil)
- **ASP.NET Core Web API** com **NSwag** (OpenAPI/Swagger)
- **Entity Framework Core** com **SQLite** e migrations
- **Windows Forms** com **Guna UI**
- **SendGrid** (envio de e-mails)
- **Bootstrap**, HTML, CSS e JavaScript

## Segurança

- Painel administrativo protegido por `[Authorize(Roles = "Admin")]`
- Nenhuma senha, chave ou string de conexão sensível no código: tudo vem de **user-secrets** (desenvolvimento) ou **variáveis de ambiente** (produção)
- API protegida por chave enviada no cabeçalho `X-Api-Key`
- Upload de imagens com validação de extensão e tamanho, e nome do arquivo gerado pelo servidor
- Campos do formulário de contato codificados antes de entrar no e-mail
- Proteção contra CSRF (`ValidateAntiForgeryToken`) nos formulários
- Swagger disponível só em ambiente de desenvolvimento

## Como executar localmente

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) e, para o app desktop, Windows.

```bash
git clone https://github.com/moreiralivia80-oss/acai-galactico-sistema.git
cd acai-galactico-sistema
dotnet restore
```

### 1. Configurar os segredos locais

Os valores abaixo ficam só na sua máquina (user-secrets) e **não vão para o repositório**. Use valores seus.

```bash
# Site (opcional): senha própria para o administrador (mínimo 8 caracteres, com número).
# Se não configurar, em desenvolvimento é usada a senha de demonstração (veja abaixo).
dotnet user-secrets set "AdminSeed:Password" "SuaSenhaLocal1" --project acaigalatico.Web

# Site (opcional): envio de e-mail do formulário de contato
dotnet user-secrets set "SendGrid:ApiKey" "sua-chave-sendgrid" --project acaigalatico.Web
dotnet user-secrets set "SendGrid:FromEmail" "remetente@exemplo.com" --project acaigalatico.Web
dotnet user-secrets set "Contato:EmailDestino" "destino@exemplo.com" --project acaigalatico.Web

# API: chave exigida no cabeçalho X-Api-Key
dotnet user-secrets set "Api:Key" "uma-chave-local-qualquer" --project AcaiGalatico.API
```

### 2. Rodar o site

```bash
dotnet run --project acaigalatico.Web
```

O banco SQLite é criado automaticamente na primeira execução, com produtos, clientes e pedidos **fictícios** de exemplo.

#### Acesso de demonstração ao painel administrador

Para conhecer o dashboard, clique em **Entrar** no menu, use os dados abaixo e depois clique em **Painel Admin**. Para ir direto ao painel após o login, use `/Identity/Account/Login?isAdmin=true`.

| Usuário | Senha |
|---|---|
| `Administrador` | `admin123` |

- Essa conta existe **só em ambiente de desenvolvimento** (rodando na sua máquina). Em produção a senha de demonstração nunca é usada: o administrador só é criado se `AdminSeed:Password` estiver configurada no servidor.
- Se você configurou `AdminSeed:Password` (passo 1), a senha do administrador passa a ser a sua.
- A senha só é definida quando o administrador é criado. Se o banco já existia com outra senha, apague o arquivo `.db` local para recriá-lo.
- Todos os dados do painel (clientes, pedidos, faturamento) são fictícios e gerados pelo seed.

### 3. Rodar a API

```bash
dotnet run --project AcaiGalatico.API
```

A documentação fica em `http://127.0.0.1:5207/swagger`. Todas as rotas `/api` exigem o cabeçalho `X-Api-Key`.

### 4. Rodar o aplicativo desktop (Windows)

Defina as variáveis de ambiente e execute o projeto `acaiGalatico.UI`:

```powershell
$env:ACAI_API_KEY = "uma-chave-local-qualquer"     # a mesma configurada em Api:Key
$env:ACAI_DESKTOP_USUARIO = "seu-usuario"
$env:ACAI_DESKTOP_SENHA = "sua-senha"
dotnet run --project acaiGalatico.UI
```

## Estrutura de pastas

```
acai-galactico/
├── acaigalatico.Domain/          Entities/, Interfaces/
├── acaigalatico.Application/     DTOs/, Interfaces/, Services/
├── acaigalatico.Infrastructure/  Context/, Migrations/, Repositories/, Seed/
├── acaigalatico.Web/             Controllers/, Views/, Areas/Identity/, Services/, wwwroot/
├── AcaiGalatico.API/             Controllers/, Program.cs
├── acaiGalatico.UI/              Forms/, Models/, Services/
└── AcaiGalatico.sln
```

## Autora

**Lívia Moreira Teixeira**, estudante de Análise e Desenvolvimento de Sistemas.
[LinkedIn](https://www.linkedin.com/in/liviamoreira-software/) · [GitHub](https://github.com/moreiralivia80-oss)

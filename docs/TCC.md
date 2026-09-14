# Documentação TCC — EcoByte

## Sumário
1. Introdução
2. Requisitos Funcionais
3. Requisitos Não-Funcionais
4. Arquitetura
5. Modelo de Dados (Cloud Firestore)
6. Diagramas
7. Segurança
8. Limitações e Trabalhos Futuros

---

## 1. Introdução

O EcoByte é uma plataforma web para redução de desperdício de alimentos. Conecta estabelecimentos que possuem excedentes a consumidores, ONGs e responsáveis pela coleta, promovendo sustentabilidade e economia.

**Stack:**
- C# / .NET 10
- ASP.NET Core MVC + Razor Views
- Cloud Firestore (Google.Cloud.Firestore 4.4.0)
- Firebase Authentication (ID tokens validados via Firebase Admin SDK - FirebaseAdmin 3.6.0)
- Firebase Emulator Suite (Auth, Firestore, Storage) para dev/tests locais
- Cookie Authentication (ASP.NET Core)

---

## 2. Requisitos Funcionais

### RF01 — Catálogo Público de Produtos
- Consumidor pode listar produtos disponíveis com filtros (categoria, busca, ordenação).
- Consumidor pode visualizar detalhes de um produto.

### RF02 — Autenticação (Login/Registro)
- Qualquer pessoa pode criar conta com nome, email e senha.
- A conta é criada no Firebase Authentication (emulador em dev); o email/senha é trocado por um **ID token**.
- O ID token é validado no backend pelo Firebase Admin SDK; o **UID é extraído somente do token validado**.
- O usuário faz login/logout; roles (perfis) vêm de claims do cookie e são aplicadas via políticas de autorização.
- Sem perfil definido, o usuário é dirigido ao Onboarding (Consumidor, Parceiro ou Ong).

### RF03 — Perfil do Consumidor
- Consumidor pode visualizar e editar seus dados.

### RF04 — Solicitação de Produto
- Consumidor pode solicitar um produto (quantidade ≤ estoque disponível).
- Consumidor pode cancelar sua solicitação (se Pendente).
- Consumidor pode confirmar retirada (se Aprovada).

### RF05 — Painel do Consumidor
- Consumidor visualiza suas solicitações e status.

### RF06 — Retiradas
- Consumidor visualiza retiradas pendentes e confirma entrega.

### RF07 — Impacto Ambiental
- Consumidor visualiza resumo de CO₂ evitado, alimentos salvos e solicitações concluídas.

### RF08 — Conquistas
- Consumidor visualiza conquistas desbloqueadas com base em ações.

### RF09 — CMS Admin (Produtos)
- Admin cria, edita e remove produtos.
- Admin gerencia status (Disponível, Esgotado, Expirado, Desativado).

### RF10 — CMS Admin (Usuários)
- Admin lista e gerencia usuários.

### RF11 — CMS Admin (Estabelecimentos)
- Admin cadastra e gerencia estabelecimentos.

### RF12 — CMS Admin (Solicitações)
- Admin aprova, cancela e confirma solicitações.

### RF13 — Dashboard Admin
- Admin visualiza contadores e acessos rápidos.

### RF14 — Auditoria
- Todas as ações críticas são registradas com timestamp e IP.

---

## 3. Requisitos Não-Funcionais

- **RNF01:** Firebase desabilitado por padrão (`Firebase.Enabled = false`); projeto inicia sem credenciais.
- **RNF01b:** Em dev, `Firebase.UseEmulator = true` aponta para `localhost` (Auth 9099, Firestore 8080, Storage 9199); `ApiKey` e credenciais nunca são versionadas (User Secrets/variáveis de ambiente).
- **RNF02:** Preços armazenados em centavos (`long`) no Firestore, exibidos em reais (`decimal`) nas Views.
- **RNF03:** Enums persistidos por nome (conversão explícita, não automática).
- **RNF04:** Mensagens de erro em português.
- **RNF05:** Validação de dados no servidor + `[ValidateAntiForgeryToken]` nos POSTs.
- **RNF06:** Sem Entity Framework Core.
- **RNF07:** Sem Repository genérico — cada entidade tem seu próprio repositório.
- **RNF08:** async/await em todas as operações Firestore.
- **RNF09:** Arquitetura em camadas: Controller → Service → Repository → Firestore.
- **RNF10:** Usuário documentado no Firestore com **id = UID** do Firebase Authentication.

---

## 4. Arquitetura

```
┌─────────────────────────────────────────────────────┐
│                     Views (Razor)                    │
├─────────────────────────────────────────────────────┤
│                    Controllers                       │
├─────────────────────────────────────────────────────┤
│                     Services                         │
├─────────────────────────────────────────────────────┤
│                   Repositories                       │
├─────────────────────────────────────────────────────┤
│                   FirestoreDb                        │
├─────────────────────────────────────────────────────┤
│                  Cloud Firestore                     │
└─────────────────────────────────────────────────────┘
```

**Fluxo de requisição:**
1. Razor View envia formulário/ação para Controller.
2. Controller valida dados e chama Service.
3. Service aplica regras de negócio e chama Repository.
4. Repository executa operação no Firestore.
5. Retorno segue caminho inverso até a View.

**Fluxo de retorno:**
1. Firestore devolve dados ao Repository.
2. Repository mapeia para Model Firestore.
3. Service converte Model Firestore → ViewModel (centavos → reais, enums → strings legíveis).
4. Controller passa ViewModel para a View.

### Fluxo de autenticação (Firebase)

```
View (Login/Registro)
      │ email + senha
      ▼
Controller (Conta) ──► IAutenticacaoService
      │                    │ 1. IAuthGateway (REST Identity Toolkit: signInWithPassword / signUp)
      │                    ▼
      │              ID token ──► IFirebaseTokenValidator (FirebaseAdmin SDK: VerifyIdTokenAsync)
      │                    │ 2. UID sai SOMENTE do token validado
      │                    ▼
      │              IUsuarioService (obter/criar usuário; perfil; estabelecimento do Parceiro)
      │                    ▼
      │         ResultadoAutenticacao (Uid, Nome, Email, Perfil, EstabelecimentoId)
      │                    ▼
      ▼         ClaimsIdentityFactory → ClaimsIdentity (AuthClaimTypes)
  SignInAsync → cookie HttpOnly + SameSite=Lax + SecurePolicy
      │
      ▼
  Autorização: políticas em AuthPolicies (Autenticado, Consumidor, Parceiro, Ong,
               EmpresaOuOng, Administrador) aplicadas via [Authorize(Policy = ...)]
```

---

## 5. Modelo de Dados (Cloud Firestore)

### Coleção: `produtos`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| Nome | string | |
| Descricao | string | |
| Categoria | string | Nome do enum (ex: "Padaria") |
| Status | string | Nome do enum (ex: "Disponivel") |
| PrecoOriginalCentavos | long | Centavos |
| PrecoPromocionalCentavos | long | Centavos |
| QuantidadeDisponivel | int | |
| DataLimite | timestamp | Validade |
| ImagemUrl | string | |
| EhVegano / EhSemGluten / EhSemLactose | bool | Tags |
| EstabelecimentoId | string | FK → estabelecimentos |
| CriadoEm / AtualizadoEm | timestamp | |

### Coleção: `usuarios` (documento id = UID do Firebase)
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId = UID do Firebase Authentication |
| Uid | string | UID (espelho, por compatibilidade) |
| Nome | string | |
| Email | string | |
| Perfil | string | Nome do enum (Consumidor, Parceiro, Ong, Administrador) |
| Ativo | bool | Conta desativada bloqueia login |
| CriadoEm / AtualizadoEm | timestamp | |
| `usuarios/{uid}/conquistas` | subcoleção | Conquistas concedidas ao usuário |

### Coleção: `estabelecimentos`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| UsuarioResponsavelId | string | FK → usuarios (Parceiro proprietário) |
| NomeFantasia | string | |
| Telefone | string | |
| Email | string | |
| Descricao | string | |
| Endereco | string | |
| Aprovado / Ativo | bool | Aprovação apenas por Admin |
| CriadoEm | timestamp | |

### Coleção: `solicitacoes`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| ProdutoId | string | FK → produtos |
| ConsumidorId | string | FK → usuarios (uid) |
| EstabelecimentoId | string | FK → estabelecimentos |
| Quantidade | int | |
| Status | string | Nome do enum (ex: "Pendente") |
| CriadoEm | timestamp | |

### Coleção: `conquistas` (catálogo) e `logs`
- `conquistas`: catálogo de conquistas (read: autenticado; write: Admin).
- `logs`: trilha de auditoria — escrita apenas via Admin SDK (server-side), leitura: Admin.

### Fonte de verdade
Os nomes de campo seguem exatamente os `[FirestoreProperty]` dos Models em `src/EcoByte.Web/Models/`.

---

## 6. Diagramas

### Diagrama de Classes (simplificado)

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Produto    │     │   Usuario    │     │Estabelecimento│
├──────────────┤     ├──────────────┤     ├──────────────┤
│ Id           │     │ Id           │     │ Id           │
│ Nome         │     │ Nome         │     │ Nome         │
│ Categoria    │     │ Email        │     │ Endereco     │
│ Status       │     │ SenhaHash    │     │ Telefone     │
│ PrecoOriginal│     │ Perfil       │     │ Horario      │
│ PrecoPromoc. │     └──────┬───────┘     └──────┬───────┘
│ QtdeDispon.  │            │                    │
│ EstabId ─────┼────────────┼────────────────────┘
└──────────────┘            │
                            │ 1:N
                   ┌────────┴────────┐
                   │   Solicitacao   │
                   ├─────────────────┤
                   │ ProdutoId       │
                   │ ConsumidorId    │
                   │ EstabelecimentoId│
                   │ Quantidade      │
                   │ Status          │
                   └─────────────────┘
```

### Diagrama de Casos de Uso (resumo)

- **Consumidor:** Listar produtos, ver detalhes, solicitar, cancelar, confirmar retirada, ver impacto, ver conquistas.
- **Parceiro (Empresa):** gerenciar estabelecimento, cadastrar/editar produtos, confirmar retiradas de seus produtos (área `Empresa`).
- **Ong:** visualizar e retirar produtos disponíveis (política `EmpresaOuOng`).
- **Admin:** CRUD produtos, gerenciar usuários, gerenciar estabelecimentos, gerenciar solicitações, dashboard.
- **Visitante:** Listar produtos, ver detalhes, registrar conta, fazer login.

---

## 7. Segurança

- **Autenticação:** Firebase Authentication real. Email/senha são trocados por um ID token no backend (`IAuthGateway`); o token é verificado com o **Firebase Admin SDK** (`VerifyIdTokenAsync`) e o **UID é obtido apenas do token validado** — nunca de campos enviados pelo cliente.
- **Sessão:** Cookie de autenticação ASP.NET Core com `HttpOnly`, `SameSite=Lax`, `SecurePolicy = SameAsRequest`, expiração de 24h e `AccessDeniedPath` para `/Conta/AcessoNegado`.
- **Claims:** `AuthClaimTypes` (Uid, Nome, Email, Perfil, EstabelecimentoId) gerados por `ClaimsIdentityFactory`; autorização por **políticas** (`AuthPolicies`) com `[Authorize(Policy = "...")]`.
- **Perfis:** Consumidor, Parceiro, Ong e Administrador. `Administrador` não pode ser escolhido via Onboarding (`DefinirPerfilAsync` rejeita).
- **Propriedade de recursos:** verificada nos Services — dono da solicitação só cancela a sua; dono do estabelecimento só gerencia seus produtos/solicitações (ex.: `SolicitacaoService.CancelarAsync`, `ConfirmarRetiradaAsync`).
- **CSRF:** `[ValidateAntiForgeryToken]` em todos os POSTs + `@Html.AntiForgeryToken()` nas Views.
- **Firestore/Storage:** regras versionadas em `firestore.rules` e `storage.rules` (não ficam "fora do escopo"). Como o Admin SDK ignora regras, a autorização real é **server-side** (Controllers/Services); as regras protegem clientes diretos.
- **Validação:** todos os dados (incluindo senha fraca, email duplicado) validados no servidor, com mensagens em português.
- **Credenciais:** nunca versionadas — `Firebase.ApiKey` e tokens via User Secrets / variáveis de ambiente; em dev/homologação usa-se o Emulator (localhost, sem credenciais).

---

## 8. Limitações e Trabalhos Futuros

### Limitações atuais
1. **Firebase CLI não instalado no ambiente:** emuladores (Auth/Firestore/Storage) não puderam ser executados; testes de integração em `tests/EcoByte.IntegrationTests` foram estruturados e **pulam** quando `ECOBYTE_EXECUTA_TESTES_EMULADOR=1` não está definido. Validar regras Firestore/Storage contra o emulador fica pendente.
2. **Sem imagens reais:** URLs de imagem são placeholders.
3. **Sem notificações push.**
4. **Paginacao com limit/offset no Firestore:** sem cursores (limitação de custo/escala).
5. **CMS Admin cria produtos sem estabelecimento vinculado** (`EstabelecimentoId` vazio): produtos exclusivos do painel administrativo; Parceiros gerenciam produtos no `Areas/Empresa` com estabelecimento próprio.

### Trabalhos futuros
1. Instalar o Firebase CLI (`npm install -g firebase-tools`) e executar os emuladores (`scripts/emuladores.ps1`) para validar o fluxo ponta a ponta e as regras de segurança.
2. Upload de imagens via Firebase Storage.
3. Paginação server-side com cursor Firestore.
4. Notificações push para novos produtos.
5. Migração/dedupe de documentos `usuarios` legados (id auto-gerado vs id = UID).
6. Deploy no Firebase Hosting.
7. Analytics de consumo e desperdício.

---

## Estrutura do Projeto

```
EcoByte/
├── EcoByte.slnx
├── AGENTS.md
├── firebase.json
├── firestore.rules
├── storage.rules
├── scripts/
│   └── emuladores.ps1
├── src/
│   └── EcoByte.Web/
│       ├── Controllers/
│       ├── Models/
│       │   └── Auth/
│       ├── Enums/
│       ├── Interfaces/
│       ├── Repositories/
│       ├── Services/
│       ├── Security/
│       ├── ViewModels/
│       ├── Firebase/
│       ├── Extensions/
│       ├── Exceptions/
│       ├── Views/
│       ├── Areas/
│       │   ├── Admin/Controllers/
│       │   └── Empresa/Controllers/
│       └── wwwroot/
└── tests/
    ├── EcoByte.Tests/
    │   └── Services/
    └── EcoByte.IntegrationTests/
```

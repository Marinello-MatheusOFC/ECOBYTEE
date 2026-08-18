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
- Cookie Authentication

---

## 2. Requisitos Funcionais

### RF01 — Catálogo Público de Produtos
- Consumidor pode listar produtos disponíveis com filtros (categoria, busca, ordenação).
- Consumidor pode visualizar detalhes de um produto.

### RF02 — Autenticação (Login/Registro)
- Consumidor pode criar conta com nome, email e senha.
- Consumidor pode fazer login e logout.

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
- **RNF02:** Preços armazenados em centavos (`long`) no Firestore, exibidos em reais (`decimal`) nas Views.
- **RNF03:** Enums persistidos por nome (conversão explícita, não automática).
- **RNF04:** Mensagens de erro em português.
- **RNF05:** Validação de dados no servidor.
- **RNF06:** Sem Entity Framework Core.
- **RNF07:** Sem Repository genérico — cada entidade tem seu próprio repositório.
- **RNF08:** async/await em todas as operações Firestore.
- **RNF09:** Arquitetura em camadas: Controller → Service → Repository → Firestore.

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
| PrecoOriginal | long | Centavos |
| PrecoPromocional | long | Centavos |
| QuantidadeDisponivel | int | |
| UnidadeMedida | string | |
| DataValidade | timestamp | |
| ImagemUrl | string | |
| EstabelecimentoId | string | FK → estabelecimentos |

### Coleção: `usuarios`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId (UID) |
| Nome | string | |
| Email | string | |
| SenhaHash | string | |
| Perfil | string | Nome do enum (ex: "Consumidor") |
| CriadoEm | timestamp | |

### Coleção: `estabelecimentos`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| Nome | string | |
| Endereco | string | |
| Telefone | string | |
| HorarioFuncionamento | string | |
| CriadoEm | timestamp | |

### Coleção: `solicitacoes`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| ProdutoId | string | FK → produtos |
| ConsumidorId | string | FK → usuarios |
| EstabelecimentoId | string | FK → estabelecimentos |
| Quantidade | int | |
| Status | string | Nome do enum (ex: "Pendente") |
| CriadoEm | timestamp | |

### Coleção: `conquistas`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| ConsumidorId | string | FK → usuarios |
| Nome | string | |
| Descricao | string | |
| ConquistadaEm | timestamp | |

### Coleção: `logs_auditoria`
| Campo | Tipo Firestore | Observação |
|---|---|---|
| Id | string | DocumentId |
| Acao | string | |
| UsuarioId | string | |
| IpAddress | string | |
| CriadoEm | timestamp | |

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
- **Admin:** CRUD produtos, gerenciar usuários, gerenciar estabelecimentos, gerenciar solicitações, dashboard.
- **Visitante:** Listar produtos, ver detalhes, registrar conta, fazer login.

---

## 7. Segurança

- **Autenticação:** Cookie-based (não Firebase Auth SDK).
- **Autorização:** `[Authorize]` em Controllers protegidos; `[Authorize(Roles = "Administrador")]` no CMS.
- **Proteção de dados:** Senhas armazenadas com hash (simulado; em produção usar BCrypt).
- **Validação:** Todos os dados validados no servidor antes de persistir.
- **Firestore:** Regras de segurança devem ser configuradas no console Firebase (fora do escopo deste projeto).
- **Credenciais:** Não versionadas — `Firebase.Credenciais` em `appsettings.Development.json` (gitignored).

---

## 8. Limitações e Trabalhos Futuros

### Limitações atuais
1. **Auth simplificada:** Login por UID via campo email — não é Firebase Authentication real.
2. **Sem imagens reais:** URLs de imagem são placeholders.
3. **Sem paginação server-side no Firestore:** Itens carregados todos, filtrados client-side.
4. **Sem notificações push.**
5. **Sem testes de integração com Firestore Emulator.**
6. **Sem paginação无限:** Sem limite de queries Firestore (limitações de custo).

### Trabalhos futuros
1. Integrar Firebase Authentication SDK real.
2. Upload de imagens via Firebase Storage.
3. Paginação server-side com cursor Firestore.
4. Notificações push para novos produtos.
5. Testes de integração com Firestore Emulator.
6. Deploy no Firebase Hosting.
7. Analytics de consumo e desperdício.

---

## Estrutura do Projeto

```
EcoByte/
├── EcoByte.slnx
├── AGENTS.md
├── src/
│   └── EcoByte.Web/
│       ├── Controllers/
│       ├── Models/
│       ├── Enums/
│       ├── Interfaces/
│       ├── Repositories/
│       ├── Services/
│       ├── ViewModels/
│       ├── Firebase/
│       ├── Extensions/
│       ├── Exceptions/
│       ├── Views/
│       ├── Areas/Admin/Controllers/
│       └── wwwroot/
└── tests/
    └── EcoByte.Tests/
        └── Services/
```

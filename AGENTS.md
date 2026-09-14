# AGENTS.md - EcoByte

## Visão Geral
Plataforma web para redução de desperdício de alimentos.
Conecta estabelecimentos com excedentes a consumidores, ONGs e responsáveis.

## Arquitetura
```
Razor View → Controller → Service → Repository → FirestoreDb → Cloud Firestore
Cloud Firestore → Repository → Service → Controller → ViewModel → Razor View
```

## Tecnologias
- C# / .NET 10
- ASP.NET Core MVC + Razor Views
- Cloud Firestore (Google.Cloud.Firestore 4.4.0)
- Firebase Authentication (ID tokens validados via Firebase Admin SDK - FirebaseAdmin 3.6.0)
- Firebase Emulator Suite (Auth, Firestore, Storage) para dev/tests locais
- HTML5 / CSS3 / JavaScript

## Estrutura
```
src/EcoByte.Web/
├── Controllers/
├── Models/Auth/
├── Enums/
├── Interfaces/
├── Repositories/
├── Services/
├── Security/                  (claim types + políticas de autorização)
├── ViewModels/
├── Firebase/
├── Extensions/
├── Views/
├── Areas/Admin/
├── Areas/Empresa/
└── wwwroot/
tests/
├── EcoByte.Tests/             (unitários - 89 testes)
└── EcoByte.IntegrationTests/  (integração com Emulator - apenas quando ECOBYTE_EXECUTA_TESTES_EMULADOR=1)
raiz: EcoByte.slnx, firebase.json, firestore.rules, storage.rules, scripts/emuladores.ps1
```

## Regras
- Firestore habilitado apenas quando Firebase.Enabled = true
- Sem credenciais versionadas (ApiKey/token sempre via User Secrets ou variáveis de ambiente)
- UID é obtido SOMENTE do ID token validado pelo Firebase Admin SDK - nunca confiar em uid/perfil enviados pelo cliente
- Claims de identidade via AuthClaimTypes (Uid/Nome/Email/Perfil/EstabelecimentoId)
- Autorização por políticas em AuthPolicies (Autenticado, Consumidor, Parceiro, Ong, EmpresaOuOng, Administrador)
- Propriedade de recursos verificada no Service (ex.: dono do estabelecimento, dono da solicitação)
- Documento de usuário no Firestore com id = UID (subcoleção conquistas em usuarios/{uid}/conquistas)
- POST em formulários: [ValidateAntiForgeryToken] + @Html.AntiForgeryToken()
- Regras Firestore/Storage em arquivos de regras do Emulator (Admin SDK ignora regras; autorização é server-side)
- Sem CRUD público (apenas Index + Detalhes)
- CRUD administrativo em Areas/Admin; operacional em Areas/Empresa
- Preços em centavos (long) no Firestore, decimal nos ViewModels
- Enums persistidos por nome (conversão explícita)
- Validação no servidor
- Sem Entity Framework Core
- Sem Repository genérico
- Mensagens em português
- async/await em todas as operações Firestore

## Códigos de Commit
- `feat: <descrição>` - nova funcionalidade
- `fix: <descrição>` - correção
- `test: <descrição>` - testes
- `docs: <descrição>` - documentação
- `refactor: <descrição>` - refatoração

## Divisão
- Backend: Models, Controllers, Services, Interfaces, Repositories, ViewModels, Enums, Firebase, Security
- Frontend: Views (cshtml), CSS, JavaScript
- Compartilhado: ViewModels, rotas, contratos de formulários

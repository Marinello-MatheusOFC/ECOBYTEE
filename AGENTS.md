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
- Firebase Authentication (futuro)
- HTML5 / CSS3 / JavaScript

## Estrutura
```
src/EcoByte.Web/
├── Controllers/
├── Models/
├── Enums/
├── Interfaces/
├── Repositories/
├── Services/
├── ViewModels/
├── Firebase/
├── Extensions/
├── Views/
├── Areas/Admin/
└── wwwroot/
```

## Regras
- Firestore habilitado apenas quando Firebase.Enabled = true
- Sem credenciais versionadas
- Sem CRUD público (apenas Index + Detalhes)
- CRUD administrativo em Areas/Admin
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
- Backend: Models, Controllers, Services, Interfaces, Repositories, ViewModels, Enums, Firebase
- Frontend: Views (cshtml), CSS, JavaScript
- Compartilhado: ViewModels, rotas, contratos de formulários

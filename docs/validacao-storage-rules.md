# Validação manual das regras de Storage

Alguns cenários de `storage.rules` dependem de `get()` cross-service (ler
`estabelecimentos/{id}` / `produtos/{id}` no Firestore). O runtime de regras do
emulador de Storage **não implementa `get()`** (`Function not found error: Name: [get]`),
por isso esses casos ficam como `Skip` nos testes de integração. Este roteiro
descreve como validá-los manualmente em um ambiente controlado.

## Pré-requisitos

- Projeto Firebase **de testes** (nunca produção) com Firestore + Storage ativos.
- Regras publicadas a partir de `firestore.rules` e `storage.rules` deste repositório.
- Dois usuários de teste: **Parceiro** (dono do estabelecimento) e **Parceiro intruso**.
- Um produto do estabelecimento do dono.
- Tokens de ID (`idToken`) obtidos via SDK de Auth (Firebase Admin/CDK ou REST Identity Toolkit).

## Cenários GET-dependentes (equivalentes aos testes `Skip`)

Substitua `{bucket}`, `{tokenDono}`, `{tokenIntruso}`, `{estab}`, `{produto}`.

1. Upload pelo responsável (esperado **200**)
   ```bash
   curl -X POST "https://firebasestorage.googleapis.com/v0/b/{bucket}/o?name=estabelecimentos/{estab}/imagens/fachada.png" \
     -H "Authorization: Bearer {tokenDono}" \
     -H "Content-Type: image/png" \
     --data-binary @fachada.png
   ```

2. Upload por outro parceiro / sem vínculo (esperado **403**)
   ```bash
   curl -X POST "https://firebasestorage.googleapis.com/v0/b/{bucket}/o?name=estabelecimentos/{estab}/imagens/fachada.png" \
     -H "Authorization: Bearer {tokenIntruso}" \
     -H "Content-Type: image/png" \
     --data-binary @fachada.png
   ```

3. Upload de imagem de produto vinculado ao estabelecimento (esperado **200**)
   ```bash
   curl -X POST "https://firebasestorage.googleapis.com/v0/b/{bucket}/o?name=estabelecimentos/{estab}/produtos/{produto}/imagens/foto.png" \
     -H "Authorization: Bearer {tokenDono}" \
     -H "Content-Type: image/png" \
     --data-binary @foto.png
   ```

4. Upload em produto de outro estabelecimento (esperado **403**)
   ```bash
   curl -X POST "https://firebasestorage.googleapis.com/v0/b/{bucket}/o?name=estabelecimentos/{estab}/produtos/{produto}/imagens/foto.png" \
     -H "Authorization: Bearer {tokenIntruso}" \
     -H "Content-Type: image/png" \
     --data-binary @foto.png
   ```

5. Exclusão por não autorizado (esperado **403**) / pelo dono (esperado **200**)
   ```bash
   curl -X DELETE "https://firebasestorage.googleapis.com/v0/b/{bucket}/o/estabelecimentos%2F{estab}%2Fimagens%2Ffachada.png" \
     -H "Authorization: Bearer {tokenIntruso}"
   ```

## Cenários já cobertos automaticamente (sem `get()`)

Avatar (`usuarios/{uid}/avatar.png`) e bloqueio de caminhos arbitrários são
validados no emulador por `StorageRulesIntegrationTests`:

- upload pelo dono (200); por outro usuário (403); anônimo (403);
- MIME não-imagem (403); arquivo maior que 5 MB (403);
- exclusão por outro usuário / anônimo (403); pelo dono (200);
- caminhos fora de `avatar.png` e path legado `produtos/**/imagens/**` (403).

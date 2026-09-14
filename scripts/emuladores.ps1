# EcoByte - Emuladores Firebase (Auth + Firestore + Storage)
# Uso: scripts/emuladores.ps1 [-Seed] [-Import <diretorio>] [-SomenteFirestore]
#
# Requisitos:
#   - Node.js 18+ (https://nodejs.org)
#   - Firebase CLI (https://firebase.google.com/docs/cli)
#
# Instalação do Firebase CLI (se ainda não tiver):
#   npm install -g firebase-tools
#
# Projeto: usa prefixo "demo-" (demo-ecobyte) configurado em .firebaserc.
# Projetos demo-* rodam SOMENTE no emulador e nunca contatam o backend real.

param(
    [switch]$Seed,
    [string]$Import,
    [switch]$SomenteFirestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$raiz = Split-Path -Parent $PSScriptRoot
$firebaseJson = Join-Path $raiz 'firebase.json'

if (-not (Test-Path $firebaseJson)) {
    Write-Error "firebase.json nao encontrado em '$raiz'. Execute este script a partir da raiz do repositorio."
}

Write-Host "== EcoByte: iniciando emuladores Firebase (projeto demo-ecobyte) ==" -ForegroundColor Cyan

# ---- Preflight: Node.js e Firebase CLI ----
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Node.js nao instalado." -ForegroundColor Yellow
    Write-Host "Instale em: https://nodejs.org ou via winget:" -ForegroundColor Yellow
    Write-Host "  winget install OpenJS.NodeJS.LTS" -ForegroundColor Yellow
    exit 1
}

if (-not (Get-Command firebase -ErrorAction SilentlyContinue)) {
    Write-Host "Firebase CLI nao instalado." -ForegroundColor Yellow
    Write-Host "Instale com:" -ForegroundColor Yellow
    Write-Host "  npm install -g firebase-tools" -ForegroundColor Yellow
    Write-Host "Valide com:  firebase --version" -ForegroundColor Yellow
    exit 1
}

Write-Host ("Firebase CLI: " + (firebase --version).Trim()) -ForegroundColor Green

# ---- Argumentos do emulador ----
$argsEmuladores = @()
if ($SomenteFirestore) {
    $argsEmuladores += '--only'
    $argsEmuladores += 'firestore'
}

Write-Host "Portas: Auth=9099 Firestore=8080 Storage=9199 UI=4000" -ForegroundColor Green

$comando = @('emulators:start', '--project', 'demo-ecobyte') + $argsEmuladores
if ($Import) {
    $comando = @('emulators:exec', '--project', 'demo-ecobyte', '--import', $Import) + $argsEmuladores
}

if ($Seed) {
    Write-Host "Seed: criando dados de demonstracao apos a subida..." -ForegroundColor Green
}

# ---- Subir emuladores (fica em foreground; Ctrl+C para encerrar) ----
& firebase @comando
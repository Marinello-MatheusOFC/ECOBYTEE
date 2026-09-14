# EcoByte - Emuladores Firebase (Auth + Firestore + Storage)
# Uso: scripts/emuladores.ps1 [-Seed] [-Import <diretorio>]
#
# Requisitos:
#   - Node.js 18+ (https://nodejs.org)
#   - Firebase CLI (https://firebase.google.com/docs/cli)
#
# Instalação do Firebase CLI (se ainda não tiver):
#   npm install -g firebase-tools
#
# Certifique-se de estar num projeto Firebase (firebase.json + firestore.rules + storage.rules na raiz).

param(
    [switch]$Seed,
    [string]$Import,

    # Portas padrão (alinhadas com Program.cs / AuthEmulatorHost)
    [int]$PortaAuth = 9099,
    [int]$PortaFirestore = 8080,
    [int]$PortaStorage = 9199
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$raiz = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $raiz 'firebase.json'))) {
    Write-Error "firebase.json nao encontrado em '$raiz'. Execute este script a partir da raiz do repositorio."
}

Write-Host "== EcoByte: iniciando emuladores Firebase ==" -ForegroundColor Cyan

# ---- Preflight: Node.js e Firebase CLI ----
$node = (Get-Command node -ErrorAction SilentlyContinue)
if (-not $node) {
    Write-Host "Node.js nao instalado." -ForegroundColor Yellow
    Write-Host "Instale em: https://nodejs.org ou via winget:" -ForegroundColor Yellow
    Write-Host "  winget install OpenJS.NodeJS.LTS" -ForegroundColor Yellow
    Write-Host "Depois:  npm install -g firebase-tools" -ForegroundColor Yellow
    exit 1
}

$firebase = (Get-Command firebase -ErrorAction SilentlyContinue)
if (-not $firebase) {
    Write-Host "Firebase CLI nao instalado." -ForegroundColor Yellow
    Write-Host "Instale com:" -ForegroundColor Yellow
    Write-Host "  npm install -g firebase-tools" -ForegroundColor Yellow
    Write-Host "Valide com:  firebase --version" -ForegroundColor Yellow
    exit 1
}

Write-Host ("Firebase CLI: " + (firebase --version).Trim()) -ForegroundColor Green

# ---- Exige projectId em binário.config (emulador local usa localhost, sem credenciais) ----
$argsBase = @('emulators:start')
if ($Import) {
    $argsBase = @('emulators:exec', "--import=$Import")
}

# Aviso sobre BLOQUEIO por credencial (usar emulador não exige login, mas o CLI cobra por padrão)
$ghost = $env:FIREBASE_TOKEN -or (Test-Path (Join-Path $env:USERPROFILE '.config\configstore\firebase-tools.json'))
if (-not $ghost) {
    Write-Host "Dica: para usar o emulador sem login, exporte FIREBASE_TOKEN ou use 'firebase login'." -ForegroundColor DarkYellow
}

Write-Host ("Portas: Auth=$PortaAuth Firestore=$PortaFirestore Storage=$PortaStorage UI=4000") -ForegroundColor Green

if ($Seed) {
    Write-Host "Seed: criando dados de demonstracao apos a subida..." -ForegroundColor Green
}

# ---- Subir emuladores (fica em foreground; Ctrl+C para encerrar) ----
firebase @argsBase
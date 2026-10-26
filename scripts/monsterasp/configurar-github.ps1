# Conecta el despliegue automático con MonsterASP.NET (plan gratuito, sin tarjeta):
#   1. Genera la clave del JWT y la copia al portapapeles para pegarla en el panel de MonsterASP.
#   2. Muestra las variables de entorno que hay que crear en el panel (ahí viven los secretos de la API).
#   3. Guarda en GitHub los datos de Web Deploy, activa GitHub Pages y lanza el primer despliegue.
#
# Requisitos: GitHub CLI con sesión iniciada (gh auth login) y el sitio y la base ya creados en MonsterASP.
# Uso:
#   powershell -ExecutionPolicy Bypass -File scripts/monsterasp/configurar-github.ps1

$ErrorActionPreference = 'Stop'

function Write-Paso([string] $texto) { Write-Host "`n==> $texto" -ForegroundColor Cyan }

function Read-Secreto([string] $pregunta) {
    $segura = Read-Host $pregunta -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

# El repositorio sale de la carpeta del script, no de la carpeta desde donde se ejecuta.
$raizRepo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repo = git -C $raizRepo remote get-url origin | ForEach-Object { $_ -replace '^.*github\.com[:/]', '' -replace '\.git$', '' }
if ($repo -notmatch '^[^/]+/[^/]+$') { throw "No se pudo leer el repositorio de GitHub de $raizRepo." }
$propietario = $repo.Split('/')[0].ToLowerInvariant()
$origenWeb = "https://$propietario.github.io"
Write-Host "Repositorio: $repo"

# ---------- 1 y 2: variables de entorno en el panel de MonsterASP ----------
Write-Paso 'Clave para firmar los JWT'
$bytes = New-Object byte[] 64
$generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$generador.GetBytes($bytes)
$generador.Dispose()
[Convert]::ToBase64String($bytes) | Set-Clipboard
Write-Host 'Se generó una clave aleatoria y quedó copiada en el portapapeles (no se muestra en pantalla).'

Write-Host ''
Write-Host 'En el panel de MonsterASP: tu sitio > Scripting > Environment variables, crea estas variables:' -ForegroundColor Yellow
Write-Host '  Jwt__Clave                 = (pega con Ctrl+V la clave copiada)'
Write-Host '  ConnectionStrings__Agenda  = (la cadena de conexión de tu base, en Databases)'
Write-Host '  AdminInicial__Correo       = (el correo con el que entrarás al panel)'
Write-Host '  AdminInicial__Contrasena   = (10+ caracteres, con letras y números)'
Write-Host "  Cors__Origenes__0          = $origenWeb"
Write-Host ''
Read-Host 'Presiona Enter cuando las hayas creado'

# ---------- 3: datos de Web Deploy en GitHub ----------
Write-Paso 'Datos de Web Deploy (Deploy > tarjeta "Visual Studio" en el panel)'
do { $sitio = (Read-Host 'Identificador del sitio (por ejemplo site12345)').Trim() } until ($sitio -match '^site\d+$')
$servidorSugerido = "https://$sitio.siteasp.net:8172"
$servidor = (Read-Host "Servidor de Web Deploy [Enter = $servidorSugerido]").Trim()
if (-not $servidor) { $servidor = $servidorSugerido }
do { $urlApi = (Read-Host 'Dirección pública del sitio (por ejemplo https://agendasalon.runasp.net)').Trim().TrimEnd('/') } until ($urlApi -match '^https://')
do {
    $contrasena = Read-Secreto 'Contraseña de Web Deploy'
    # Web Deploy separa sus opciones con comas; unas comillas romperían el paso a gh en Windows PowerShell.
    $valida = $contrasena.Length -gt 0 -and $contrasena -notmatch '[,"]'
    if (-not $valida) { Write-Host 'La contraseña está vacía o tiene comas o comillas: genera otra en el panel.' -ForegroundColor Yellow }
} until ($valida)

gh variable set MONSTERASP_SITIO --repo $repo --body $sitio
gh variable set MONSTERASP_SERVIDOR --repo $repo --body $servidor
gh variable set API_URL --repo $repo --body $urlApi
gh secret set MONSTERASP_CONTRASENA --repo $repo --body $contrasena
Remove-Variable contrasena
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron guardar los datos en GitHub.' }

Write-Paso 'Activando GitHub Pages con despliegue desde GitHub Actions'
& {
    $ErrorActionPreference = 'Continue'
    gh api --method POST "repos/$repo/pages" -f build_type=workflow 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { gh api --method PUT "repos/$repo/pages" -f build_type=workflow 2>$null | Out-Null }
}

Write-Paso 'Lanzando el despliegue'
gh workflow run ci.yml --repo $repo --ref main

Write-Host ''
Write-Host 'Listo.' -ForegroundColor Green
Write-Host "API:  $urlApi/salud"
Write-Host "Web:  $origenWeb/$($repo.Split('/')[1])/"
Write-Host "Sigue el despliegue en: https://github.com/$repo/actions"

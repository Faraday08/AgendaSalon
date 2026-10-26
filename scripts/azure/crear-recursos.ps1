# Crea en Azure todo lo que necesita la demo, usando solo planes gratuitos, y conecta el despliegue de GitHub:
#   - Azure SQL Database con la oferta gratuita (se pausa sola si se acaba la cuota del mes: nunca cobra).
#   - App Service en el plan F1 (gratis) para la API.
#   - Una identidad para que GitHub Actions despliegue con OIDC, sin guardar contraseñas en GitHub.
#   - Las variables y secretos del repositorio, y GitHub Pages para la web.
#
# Requisitos: Azure CLI con sesión iniciada (az login) y GitHub CLI con sesión iniciada (gh auth login).
# Uso, desde la raíz del repositorio:
#   powershell -ExecutionPolicy Bypass -File scripts/azure/crear-recursos.ps1
#
# Las contraseñas y claves se generan aquí y van directo a Azure: no se muestran ni se guardan en el repositorio.

param(
    [string] $Region = 'eastus2',
    [string] $Nombre = 'agendasalon'
)

$ErrorActionPreference = 'Stop'

function Invoke-Az {
    $salida = & az @args
    if ($LASTEXITCODE -ne 0) { throw "Falló: az $($args[0..2] -join ' ')" }
    return $salida
}

# Como Invoke-Az, pero devuelve $null en vez de fallar: sirve para preguntar si algo ya existe.
function Find-Az {
    $salida = & az @args 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $salida) { return $null }
    return $salida
}

function New-Aleatorio([int] $longitud, [string] $alfabeto) {
    $generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $bytes = New-Object byte[] $longitud
        $generador.GetBytes($bytes)
        return -join ($bytes | ForEach-Object { $alfabeto[$_ % $alfabeto.Length] })
    } finally {
        $generador.Dispose()
    }
}

function Write-Paso([string] $texto) { Write-Host "`n==> $texto" -ForegroundColor Cyan }

function Set-AjustesDesdeArchivo([string[]] $comando, [object[]] $ajustes) {
    # Los secretos viajan en un archivo temporal y no en la línea de comandos, para que los símbolos
    # (+, /, =, &, %) no se rompan al pasar por cmd.exe. El archivo se borra enseguida.
    $archivo = Join-Path $env:TEMP ("ajustes-" + [guid]::NewGuid() + '.json')
    try {
        [IO.File]::WriteAllText($archivo, (ConvertTo-Json -InputObject $ajustes -Depth 3), (New-Object Text.UTF8Encoding $false))
        Invoke-Az @comando --settings "@$archivo" --output none
    } finally {
        Remove-Item $archivo -ErrorAction SilentlyContinue
    }
}

# ---------- Comprobaciones previas: nada se crea hasta que todo cuadre ----------
Write-Paso 'Comprobando herramientas y sesiones'
foreach ($herramienta in 'az', 'gh') {
    if (-not (Get-Command $herramienta -ErrorAction SilentlyContinue)) { throw "Falta '$herramienta'. Instálalo y vuelve a ejecutar el script." }
}
$cuenta = az account show --output json 2>$null | ConvertFrom-Json
if (-not $cuenta) { throw 'No hay sesión de Azure. Ejecuta: az login' }
gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'No hay sesión de GitHub. Ejecuta: gh auth login' }
# El repositorio sale de la carpeta del script, no de la carpeta desde donde se ejecuta:
# así nunca se configuran secretos en otro repositorio por accidente.
$raizRepo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repo = git -C $raizRepo remote get-url origin | ForEach-Object { $_ -replace '^.*github\.com[:/]', '' -replace '\.git$', '' }
if ($repo -notmatch '^[^/]+/[^/]+$') { throw "No se pudo leer el repositorio de GitHub de $raizRepo." }
$propietario = $repo.Split('/')[0].ToLowerInvariant()

$ayudaSql = az sql db create --help | Out-String
if ($ayudaSql -notmatch 'use-free-limit') { throw 'Tu versión de Azure CLI no tiene la oferta gratuita de SQL. Actualízala con: az upgrade' }

# La salida tsv trae varias columnas (versión, fin de soporte, sistema...): solo interesa la primera.
$runtime = (Invoke-Az webapp list-runtimes --os linux --output tsv) |
    ForEach-Object { ($_ -split "`t")[0].Trim() } |
    Where-Object { $_ -match '^DOTNETCORE[:|]10(\.\d+)?$' } |
    Select-Object -First 1
if (-not $runtime) { throw 'Azure todavía no ofrece .NET 10 en Linux en esta suscripción. Revisa: az webapp list-runtimes --os linux' }

Write-Host "Suscripción: $($cuenta.name)"
Write-Host "Repositorio: $repo"
Write-Host "Región:      $Region"
Write-Host "Runtime:     $runtime"
Write-Host ''
Write-Host 'Se crearán: un grupo de recursos, un servidor y una base Azure SQL (oferta gratuita con pausa automática),'
Write-Host 'un plan App Service F1 (gratis), una app web y una identidad para GitHub Actions.'
if ((Read-Host '¿Continuar? (s/n)') -ne 's') { Write-Host 'Cancelado. No se creó nada.'; exit 0 }

# ---------- Datos del administrador de la aplicación ----------
Write-Paso 'Cuenta del administrador del panel'
$correoAdmin = Read-Host 'Correo del administrador'
do {
    $segura = Read-Host 'Contraseña (10+ caracteres, con letras y números)' -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
    try { $contrasenaAdmin = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
    $valida = $contrasenaAdmin.Length -ge 10 -and $contrasenaAdmin -match '\p{L}' -and $contrasenaAdmin -match '\d'
    if (-not $valida) { Write-Host 'No cumple los requisitos, prueba otra.' -ForegroundColor Yellow }
} until ($valida)

# ---------- Proveedores de recursos ----------
# Una suscripción nueva debe registrar cada servicio antes de usarlo la primera vez. Es gratis y solo se hace una vez.
foreach ($proveedor in 'Microsoft.Sql', 'Microsoft.Web') {
    Write-Paso "Registrando el proveedor $proveedor (puede tardar un par de minutos)"
    Invoke-Az provider register --namespace $proveedor --wait --output none
}

# ---------- Nombres y secretos generados ----------
# Si el script ya se ejecutó antes y falló a medias, se reutiliza lo que exista en el grupo en vez de duplicarlo.
$grupo = "rg-$Nombre"
$plan = "$Nombre-plan"
$sufijo = New-Aleatorio 5 'abcdefghijkmnpqrstuvwxyz23456789'
$servidorSql = Find-Az sql server list --resource-group $grupo --query '[0].name' --output tsv
if (-not $servidorSql) { $servidorSql = "$Nombre-sql-$sufijo" }
$app = Find-Az webapp list --resource-group $grupo --query '[0].name' --output tsv
if (-not $app) { $app = "$Nombre-api-$sufijo" }
$usuarioSql = 'agendaadmin'
# Solo letras y números: cumple la complejidad de Azure SQL y no se rompe en la línea de comandos.
$contrasenaSql = 'Aa1' + (New-Aleatorio 29 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789')
$bytesJwt = New-Object byte[] 64
$generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$generador.GetBytes($bytesJwt)
$generador.Dispose()
$claveJwt = [Convert]::ToBase64String($bytesJwt)

# ---------- Base de datos ----------
Write-Paso "Grupo de recursos $grupo"
Invoke-Az group create --name $grupo --location $Region --output none

Write-Paso "Servidor SQL $servidorSql"
if (Find-Az sql server show --resource-group $grupo --name $servidorSql --query name --output tsv) {
    # Ya existía: se le pone la contraseña nueva, porque la anterior no se guardó en ningún lado.
    Invoke-Az sql server update --resource-group $grupo --name $servidorSql --admin-password $contrasenaSql --output none
} else {
    Invoke-Az sql server create --name $servidorSql --resource-group $grupo --location $Region `
        --admin-user $usuarioSql --admin-password $contrasenaSql --minimal-tls-version 1.2 --output none
}
# 0.0.0.0 es la regla especial de Azure para "servicios de Azure", no abre la base a Internet.
Invoke-Az sql server firewall-rule create --resource-group $grupo --server $servidorSql --name PermitirServiciosAzure `
    --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none

Write-Paso 'Base AgendaSalon con la oferta gratuita (se pausa al agotar la cuota del mes)'
if (-not (Find-Az sql db show --resource-group $grupo --server $servidorSql --name AgendaSalon --query name --output tsv)) {
    Invoke-Az sql db create --resource-group $grupo --server $servidorSql --name AgendaSalon `
        --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless `
        --use-free-limit --free-limit-exhaustion-behavior AutoPause --backup-storage-redundancy Local --output none
}

# ---------- API ----------
Write-Paso "Plan App Service F1 (gratis) y app $app"
Invoke-Az appservice plan create --resource-group $grupo --name $plan --location $Region --sku F1 --is-linux --output none
if (-not (Find-Az webapp show --resource-group $grupo --name $app --query name --output tsv)) {
    Invoke-Az webapp create --resource-group $grupo --plan $plan --name $app --runtime $runtime --output none
}
Invoke-Az webapp config set --resource-group $grupo --name $app --web-sockets-enabled true `
    --startup-file 'dotnet AgendaSalon.Api.dll' --min-tls-version 1.2 --ftps-state Disabled --output none
Invoke-Az webapp update --resource-group $grupo --name $app --https-only true --output none
$hostApi = Invoke-Az webapp show --resource-group $grupo --name $app --query defaultHostName --output tsv
$urlApi = "https://$hostApi"
$origenWeb = "https://$propietario.github.io"

Write-Paso 'Configuración y secretos de la API'
$cadena = "Server=tcp:$servidorSql.database.windows.net,1433;Database=AgendaSalon;User ID=$usuarioSql;Password=$contrasenaSql;Encrypt=True;TrustServerCertificate=False;Connect Timeout=60;"
Set-AjustesDesdeArchivo @('webapp', 'config', 'connection-string', 'set', '--resource-group', $grupo, '--name', $app, '--connection-string-type', 'SQLAzure') @(
    @{ name = 'Agenda'; value = $cadena; type = 'SQLAzure'; slotSetting = $false })
Set-AjustesDesdeArchivo @('webapp', 'config', 'appsettings', 'set', '--resource-group', $grupo, '--name', $app) @(
    @{ name = 'Jwt__Clave'; value = $claveJwt; slotSetting = $false },
    @{ name = 'AdminInicial__Correo'; value = $correoAdmin; slotSetting = $false },
    @{ name = 'AdminInicial__Contrasena'; value = $contrasenaAdmin; slotSetting = $false },
    @{ name = 'Cors__Origenes__0'; value = $origenWeb; slotSetting = $false },
    @{ name = 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'; value = 'true'; slotSetting = $false })
Remove-Variable contrasenaAdmin, contrasenaSql, claveJwt, cadena

# ---------- Identidad de GitHub Actions (OIDC) ----------
Write-Paso 'Identidad para que GitHub Actions despliegue sin contraseñas'
$idCliente = Find-Az ad app list --display-name "github-$app" --query '[0].appId' --output tsv
if (-not $idCliente) { $idCliente = Invoke-Az ad app create --display-name "github-$app" --query appId --output tsv }
if (-not (Find-Az ad sp show --id $idCliente --query id --output tsv)) { Invoke-Az ad sp create --id $idCliente --output none }

$yaTieneCredencial = Find-Az ad app federated-credential list --id $idCliente --query "[?name=='github-produccion'].name" --output tsv
if (-not $yaTieneCredencial) {
    $credencial = @{
        name = 'github-produccion'
        issuer = 'https://token.actions.githubusercontent.com'
        subject = "repo:${repo}:environment:produccion"
        audiences = @('api://AzureADTokenExchange')
    }
    $archivo = Join-Path $env:TEMP ("credencial-" + [guid]::NewGuid() + '.json')
    [IO.File]::WriteAllText($archivo, (ConvertTo-Json $credencial), (New-Object Text.UTF8Encoding $false))
    try { Invoke-Az ad app federated-credential create --id $idCliente --parameters "@$archivo" --output none }
    finally { Remove-Item $archivo -ErrorAction SilentlyContinue }
}

# Solo puede desplegar en esta app, nada más de la suscripción. La identidad tarda unos segundos en estar lista.
$alcance = Invoke-Az webapp show --resource-group $grupo --name $app --query id --output tsv
if (-not (Find-Az role assignment list --assignee $idCliente --scope $alcance --role 'Website Contributor' --query '[0].id' --output tsv)) {
    for ($intento = 1; $intento -le 6; $intento++) {
        az role assignment create --assignee $idCliente --role 'Website Contributor' --scope $alcance --output none 2>$null
        if ($LASTEXITCODE -eq 0) { break }
        if ($intento -eq 6) { throw 'No se pudo dar permiso de despliegue a la identidad de GitHub.' }
        Start-Sleep -Seconds 15
    }
}

# ---------- GitHub ----------
Write-Paso "Variables y secretos de $repo"
gh secret set AZURE_CLIENT_ID --repo $repo --body $idCliente
gh secret set AZURE_TENANT_ID --repo $repo --body $cuenta.tenantId
gh secret set AZURE_SUBSCRIPTION_ID --repo $repo --body $cuenta.id
gh variable set AZURE_WEBAPP_NAME --repo $repo --body $app
gh variable set API_URL --repo $repo --body $urlApi

Write-Paso 'Activando GitHub Pages con despliegue desde GitHub Actions'
gh api --method POST "repos/$repo/pages" -f build_type=workflow 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { gh api --method PUT "repos/$repo/pages" -f build_type=workflow | Out-Null }

Write-Paso 'Lanzando el primer despliegue'
gh workflow run ci.yml --repo $repo --ref main

Write-Host ''
Write-Host 'Listo.' -ForegroundColor Green
Write-Host "API:  $urlApi/salud"
Write-Host "Web:  $origenWeb/$($repo.Split('/')[1])/"
Write-Host "Sigue el despliegue en: https://github.com/$repo/actions"
Write-Host 'La primera visita tarda unos segundos: el plan gratuito y la base se despiertan al recibir tráfico.'

# Configura los secretos locales de la API con dotnet user-secrets (se guardan en tu perfil de Windows,
# fuera del repositorio): una clave aleatoria para firmar los JWT y la cuenta del primer administrador.
# Funciona en Windows PowerShell 5.1 y en PowerShell 7.
#
# Uso, desde la raíz del repositorio:
#   powershell -ExecutionPolicy Bypass -File scripts/configurar-secretos.ps1

$ErrorActionPreference = 'Stop'
$proyecto = Join-Path $PSScriptRoot '..\src\AgendaSalon.Api'

function Set-Secreto([string] $clave, [string] $valor) {
    dotnet user-secrets set $clave $valor --project $proyecto | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudo guardar $clave." }
}

# Clave de firma: 64 bytes aleatorios en Base64.
$generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $bytes = New-Object byte[] 64
    $generador.GetBytes($bytes)
    Set-Secreto 'Jwt:Clave' ([Convert]::ToBase64String($bytes))
} finally {
    $generador.Dispose()
}
Write-Host 'Clave del JWT generada.' -ForegroundColor Green

$correo = Read-Host 'Correo del administrador'
do {
    $segura = Read-Host 'Contraseña (10+ caracteres, con letras y números)' -AsSecureString
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura)
    try {
        $contrasena = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero)
    } finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero)
    }
    $valida = $contrasena.Length -ge 10 -and $contrasena -match '\p{L}' -and $contrasena -match '\d'
    if (-not $valida) { Write-Host 'No cumple los requisitos, prueba otra.' -ForegroundColor Yellow }
} until ($valida)

Set-Secreto 'AdminInicial:Correo' $correo
Set-Secreto 'AdminInicial:Contrasena' $contrasena
Remove-Variable contrasena

Write-Host ''
Write-Host 'Listo. Al iniciar, la API crea la cuenta del administrador si todavía no existe ninguno.' -ForegroundColor Green
Write-Host 'Inicia la API con: dotnet run --project src/AgendaSalon.Api --launch-profile http'

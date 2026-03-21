param([Parameter(Mandatory=$true)][string]$Nombre)

Write-Host " Iniciando migración: $Nombre" -ForegroundColor Cyan

# 1. Agregar la migración
Add-Migration $Nombre -Context ZendaDbContext -Project Zenda.Infrastructure -StartupProject Zenda.Api

# 2. Actualizar la base de datos en Neon
if ($?) { 
    Write-Host "Updating database..." -ForegroundColor Yellow
    Update-Database -Context ZendaDbContext -Project Zenda.Infrastructure -StartupProject Zenda.Api
    Write-Host " ¡Base de datos actualizada con éxito!" -ForegroundColor Green
} else {
    Write-Host " Error al crear la migración. Revisá el código." -ForegroundColor Red
}
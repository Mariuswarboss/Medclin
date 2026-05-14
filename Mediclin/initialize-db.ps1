#!/usr/bin/env pwsh
# Script para inicializar a base de dados Mediclin
# Pré-requisitos: MySQL instalado e servidor em execução em localhost:3306

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Inicializador de Base de Dados Mediclin" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se MySQL está instalado
$mysqlPath = Get-Command mysql -ErrorAction SilentlyContinue
if (-not $mysqlPath) {
    Write-Host "ERRO: MySQL não está instalado ou não está no PATH." -ForegroundColor Red
    Write-Host "Instale MySQL Server em https://dev.mysql.com/downloads/mysql/" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ MySQL encontrado: $($mysqlPath.Source)" -ForegroundColor Green
Write-Host ""

# Credenciais do MySQL
$host = "localhost"
$user = "root"
$password = "Katepal01"
$port = 3306

Write-Host "Conectando ao MySQL..."
Write-Host "Host: $host"
Write-Host "Utilizador: $user"
Write-Host "Porta: $port"
Write-Host ""

# Executar script de schema
Write-Host "1. Criando tabelas da base de dados..." -ForegroundColor Yellow
$schemaPath = ".\Mediclin.Data\Migrations\schema.sql"
if (Test-Path $schemaPath) {
    try {
        mysql -h $host -u $user -p$password --port $port < $schemaPath
        Write-Host "✓ Schema criada com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Erro ao criar schema: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "✗ Ficheiro schema.sql não encontrado em: $schemaPath" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Executar script de seed de admin
Write-Host "2. Inserindo dados iniciais..." -ForegroundColor Yellow
$seedPath = ".\Mediclin.Data\Migrations\seed_admin.sql"
if (Test-Path $seedPath) {
    try {
        mysql -h $host -u $user -p$password --port $port < $seedPath
        Write-Host "✓ Dados iniciais inseridos com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Erro ao inserir dados: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "✗ Ficheiro seed_admin.sql não encontrado em: $seedPath" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "Base de dados inicializada com sucesso!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "Dados de login para teste:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  ADMIN:" -ForegroundColor Yellow
Write-Host "    Email: admin@mediclin.com" -ForegroundColor Gray
Write-Host "    Senha: Admin@123" -ForegroundColor Gray
Write-Host ""
Write-Host "  MEDICO:" -ForegroundColor Yellow
Write-Host "    Email: medic@mediclin.com" -ForegroundColor Gray
Write-Host "    Senha: Medic@123" -ForegroundColor Gray
Write-Host ""
Write-Host "  PACIENT:" -ForegroundColor Yellow
Write-Host "    Email: pacient@mediclin.com" -ForegroundColor Gray
Write-Host "    Senha: Pacient@123" -ForegroundColor Gray
Write-Host ""
Write-Host "Nota: Altere as senhas após o primeiro login!" -ForegroundColor Magenta

# Compila os scripts do jogo FORA do Unity, só pra achar erros de C# (não gera nada que o jogo usa).
# Usa o compilador (Roslyn) e as DLLs que vêm com o próprio Unity, lidas do Assembly-CSharp.csproj.
#
# Uso, na raiz do projeto:
#   powershell -ExecutionPolicy Bypass -File tools/checar-csharp.ps1
#
# Precisa ter aberto o projeto no Unity pelo menos uma vez (cria o .csproj e a pasta Library).
# Saída 0 = sem erros. Não substitui o teste no Play.

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz

if (-not (Test-Path 'Assembly-CSharp.csproj')) {
    Write-Host 'Não achei o Assembly-CSharp.csproj. Abra o projeto no Unity uma vez (e, se precisar, Edit > Preferences > External Tools > Regenerate project files).'
    exit 2
}

[xml]$csproj = Get-Content 'Assembly-CSharp.csproj'
$refs = @($csproj.SelectNodes('//*[local-name()="HintPath"]') | ForEach-Object { $_.InnerText })
$defines = ($csproj.SelectNodes('//*[local-name()="DefineConstants"]') | Select-Object -First 1).InnerText
$langver = ($csproj.SelectNodes('//*[local-name()="LangVersion"]') | Select-Object -First 1).InnerText

# Pasta do Unity a partir de uma DLL conhecida
$core = $refs | Where-Object { $_ -like '*UnityEngine.CoreModule.dll' } | Select-Object -First 1
if (-not $core) { Write-Host 'Não achei o caminho do Unity no .csproj.'; exit 2 }
$data = $core.Substring(0, $core.IndexOf('\Data\') + 5)
$dotnet = Join-Path $data 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $data 'DotNetSdkRoslyn\csc.dll'

# Todos os .cs de Assets que caem no Assembly-CSharp: fora de pastas Editor e de pastas com .asmdef
$asmdefDirs = @(Get-ChildItem Assets -Recurse -Filter *.asmdef | ForEach-Object { $_.DirectoryName + '\' })
$arquivos = Get-ChildItem Assets -Recurse -Filter *.cs | Where-Object {
    $p = $_.FullName
    if ($p -match '\\Editor\\') { return $false }
    foreach ($d in $asmdefDirs) { if ($p.StartsWith($d)) { return $false } }
    return $true
} | ForEach-Object { Resolve-Path -Relative $_.FullName }

$saida = Join-Path $env:TEMP 'five_stages_check.dll'
$rsp = @('-nologo', '-target:library', '-nostdlib+', '-noconfig', "-out:`"$saida`"", "-define:$defines",
         '-nowarn:CS0618,CS0414,CS0649,CS0169,CS0219,CS0162,CS0108,CS0114,CS0067')
if ($langver) { $rsp += "-langversion:$langver" }
$rsp += $refs | ForEach-Object { "-r:`"$_`"" }
$rsp += $arquivos | ForEach-Object { "`"$_`"" }
$rspPath = Join-Path $env:TEMP 'five_stages_check.rsp'
$rsp | Set-Content -Encoding utf8 $rspPath

Write-Host ("Compilando {0} scripts com o Roslyn do Unity..." -f $arquivos.Count)
& $dotnet $csc "@$rspPath" | Where-Object { $_ -match 'error' }
Write-Host "saida: $LASTEXITCODE"
exit $LASTEXITCODE

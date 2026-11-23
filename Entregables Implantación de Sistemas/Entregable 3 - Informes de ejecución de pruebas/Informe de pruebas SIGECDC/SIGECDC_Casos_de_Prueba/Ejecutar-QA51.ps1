[CmdletBinding()]
param([ValidateSet('LocalAislado','RemotoLectura')][string]$Modo='LocalAislado',
      [string]$Url='https://sigecdc.runasp.net/', [switch]$Inventario)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(Test-Path -LiteralPath (Join-Path $root 'MANIFIESTO_SHA256.json')){& (Join-Path $root 'Verificar-Integridad.ps1')}
$casos=Get-Content -LiteralPath (Join-Path $root 'Casos51.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($casos.Count -ne 51 -or @($casos.id | Sort-Object -Unique).Count -ne 51){throw 'El inventario no tiene 51 IDs distintos.'}
$env:SIGECDC_QA51_MODO=$Modo
$env:SIGECDC_QA51_URL=$Url
$dotnet=Join-Path $root 'dependencias\dotnet\dotnet.exe'
$runner=Join-Path $root 'dependencias\runner\vstest.console.dll'
$dll=Join-Path $root 'pruebas\SIGECDC.Tests.dll'
$env:DOTNET_ROOT=Join-Path $root 'dependencias\dotnet'
$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$env:DOTNET_NOLOGO='1'
$env:PATH=$env:DOTNET_ROOT+';'+$env:PATH
foreach($name in @('MYSQLD','MYSQL','MYSQLDUMP')){
    [Environment]::SetEnvironmentVariable('SIGECDC_QA_'+$name+'_PATH',(Join-Path $root ('dependencias\mysql\bin\'+$name.ToLowerInvariant()+'.exe')),'Process')
}
foreach($p in @($dotnet,$runner,$dll)){if(!(Test-Path -LiteralPath $p)){throw 'Dependencia incluida no encontrada: '+$p}}
if($Inventario){Write-Host '51 IDs de QA02; modo completo local y subconjunto remoto PUB-001/PUB-009.'; & $dotnet --list-runtimes; exit $LASTEXITCODE}
$seleccion=if($Modo -eq 'LocalAislado'){@($casos)}else{@($casos | Where-Object {$_.id -in @('PUB-001','PUB-009')})}
$filter=if($Modo -eq 'LocalAislado'){'Suite=EntregableImplantacion'}else{'FullyQualifiedName~Pub001_|FullyQualifiedName~Pub009_'}
$resultados=Join-Path $root ('resultados\'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+$Modo)
New-Item -ItemType Directory -Path $resultados -Force | Out-Null
$trx=Join-Path $resultados 'QA51.trx'
Write-Host ('Ejecutando '+$seleccion.Count+' casos. '+$Modo+'. Sin pagos ni conexion DB remota.')
Push-Location $root
try {
    & $dotnet $runner $dll ('/TestCaseFilter:'+$filter) ('/TestAdapterPath:'+(Join-Path $root 'pruebas')) ('/ResultsDirectory:'+$resultados) '/Logger:trx;LogFileName=QA51.trx' '/Logger:console;verbosity=normal' '/Platform:x64'
    $codigo=$LASTEXITCODE
} finally { Pop-Location }
$resultadosCaso=@()
$filas=@()
if(Test-Path -LiteralPath $trx){[xml]$xml=Get-Content -LiteralPath $trx -Raw -Encoding UTF8; $filas=@($xml.SelectNodes("//*[local-name()='UnitTestResult']"))}
$vistos=@{}
foreach($r in $filas){
    if($r.testName -match '^([A-Z]+-\d{3})\s'){
        $id=$Matches[1]
        if($vistos.ContainsKey($id)){throw 'Resultado duplicado para '+$id}
        $vistos[$id]=$r
    }else{throw 'Se ejecuto un caso fuera de QA02: '+$r.testName}
}
foreach($c in $seleccion){
    $r=$vistos[$c.id]
    $estado='BLOCKED';$mensaje='Sin resultado individual; revisar TRX/salida de infraestructura.'
    if($r){
        $err=$r.SelectSingleNode(".//*[local-name()='Message']")
        $mensaje=if($err){$err.InnerText}else{''}
        $estado=if($r.outcome -eq 'Passed'){'PASS'}elseif($r.outcome -eq 'Failed'){'FAIL'}else{'BLOCKED'}
        if($mensaje -match 'BLOCKED:|requerido para F1-FIN-02B|No se pudo inicializar MySQL|MySQL temporal no inici'){$estado='BLOCKED'}
    }
    $resultadosCaso+=[pscustomobject]@{ID=$c.id;Estado=$estado;Modo=$Modo;Escenario=$c.escenario;Metodo=$c.metodo;Mensaje=$mensaje}
}
$resultadosCaso | Export-Csv -LiteralPath (Join-Path $resultados 'ResultadosPorCaso.csv') -NoTypeInformation -Encoding UTF8
$resumen=[ordered]@{modo=$Modo;esperados=$seleccion.Count;ejecutados=$vistos.Count;PASS=@($resultadosCaso|Where-Object Estado -EQ 'PASS').Count;FAIL=@($resultadosCaso|Where-Object Estado -EQ 'FAIL').Count;BLOCKED=@($resultadosCaso|Where-Object Estado -EQ 'BLOCKED').Count;codigoVSTest=$codigo;fecha=(Get-Date).ToString('o')}
$resumen | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $resultados 'Resumen.json') -Encoding UTF8
$resumen | ConvertTo-Json | Write-Host
Write-Host ('Evidencia: '+$resultados)
if($resumen.FAIL -gt 0 -or $resumen.BLOCKED -gt 0 -or $vistos.Count -ne $seleccion.Count){exit 1}
exit 0

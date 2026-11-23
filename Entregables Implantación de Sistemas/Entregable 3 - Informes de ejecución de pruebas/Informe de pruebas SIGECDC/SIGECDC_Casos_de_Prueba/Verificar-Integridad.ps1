$ErrorActionPreference='Stop'
$raiz=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')+'\'
$path=Join-Path $raiz 'MANIFIESTO_SHA256.json'
if(!(Test-Path -LiteralPath $path)){throw 'No se encuentra el manifiesto de la entrega final.'}
$manifest=Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
$n=0
foreach($p in $manifest.PSObject.Properties){
    $archivo=[IO.Path]::GetFullPath((Join-Path $raiz $p.Name))
    if(!$archivo.StartsWith($raiz,[StringComparison]::OrdinalIgnoreCase)){throw 'Ruta fuera del paquete.'}
    if(!(Test-Path -LiteralPath $archivo)){throw ('Archivo ausente: '+$p.Name)}
    $stream=[IO.File]::OpenRead($archivo)
    $algoritmo=[Security.Cryptography.SHA256]::Create()
    try{$hash=[BitConverter]::ToString($algoritmo.ComputeHash($stream)).Replace('-','')}
    finally{$stream.Dispose();$algoritmo.Dispose()}
    if($hash -ne $p.Value){throw ('Archivo alterado: '+$p.Name)}
    $n++
}
Write-Host ('INTEGRIDAD_OK: '+$n+' archivos verificados.')

param(
    [Parameter(Mandatory = $true)]
    [string]$BuildBaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$WasmFile,

    [Parameter(Mandatory = $true)]
    [string]$DataFile
)

$ErrorActionPreference = 'Stop'
$baseUrl = $BuildBaseUrl.TrimEnd('/')

function Assert-Header {
    param(
        [Parameter(Mandatory = $true)] [string]$Url,
        [Parameter(Mandatory = $true)] [string]$Header,
        [Parameter(Mandatory = $true)] [string]$ExpectedPattern
    )

    $response = Invoke-WebRequest -Uri $Url -Method Head -UseBasicParsing
    $actual = [string]$response.Headers[$Header]
    if ($actual -notmatch $ExpectedPattern) {
        throw "$Url returned '${Header}: $actual'; expected /$ExpectedPattern/."
    }
}

$wasmUrl = "$baseUrl/$WasmFile"
$dataUrl = "$baseUrl/$DataFile"

Assert-Header -Url $wasmUrl -Header 'Content-Encoding' -ExpectedPattern '(^|,\s*)br($|,)'
Assert-Header -Url $wasmUrl -Header 'Content-Type' -ExpectedPattern '^application/wasm($|;)'
Assert-Header -Url $wasmUrl -Header 'Cache-Control' -ExpectedPattern 'immutable'
Assert-Header -Url $dataUrl -Header 'Content-Encoding' -ExpectedPattern '(^|,\s*)br($|,)'
Assert-Header -Url $dataUrl -Header 'Cache-Control' -ExpectedPattern 'immutable'

Write-Output 'WebGL deployment headers passed.'

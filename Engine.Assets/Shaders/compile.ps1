# ---- Paths ----
$BGFX        = 'V:\Taiga6\bgfx'
$SC          = "$BGFX\.build\win64_vs2022\bin\shadercRelease.exe"
$SHDR        = 'V:\Taiga6\CustomEngine\Engine.Assets\Materials'
$OUTPUT_BASE = 'V:\Taiga6\CustomEngine\Engine.Editor\bin\debug\net9.0\Compiled\Shaders'

# ---- Common params ----
$commonParams = @(
    "--platform","windows",
    "-p","s_5_0",
    "-i","$BGFX\src",
    "-i","V:\Taiga6\bgfx\examples\common",
    "--varyingdef", "$SHDR\varying.def.sc"
)

$Errors = @()          # will hold compiler stderr text

# ---------- Helper functions ----------
function Write-Section {
    param(
        [string]$Text,
        [string]$Color = 'Cyan'
    )
    Write-Host ""
    Write-Host $Text -ForegroundColor $Color
    Write-Host ('-' * $Text.Length) -ForegroundColor $Color
}

function Write-Result {
    param(
        [string]$Source,
        [string]$Dest,
        [bool]  $Ok,
        [int]   $SrcWidth
    )
    $srcPadded = $Source.PadRight($SrcWidth)
    $arrow     = '->'

    if ($Ok) {
        Write-Host "[OK]" -ForegroundColor Green -NoNewline
    } else {
        Write-Host "[!!]" -ForegroundColor Red -NoNewline
    }
    Write-Host " $srcPadded $arrow $Dest"
}

function Invoke-CompileShaders {
    param(
        [string]$Filter,
        [string]$Type
    )

    Write-Section "-- Compiling $Type shaders"

    $longestPathLength = (Get-ChildItem -Path $SHDR -Filter $Filter -Recurse |
            ForEach-Object { $_.FullName.Substring($SHDR.Length + 1).Length } |
            Measure-Object -Maximum).Maximum + 2

    $count = 0
    Get-ChildItem -Path $SHDR -Filter $Filter -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($SHDR.Length + 1)
        $absolute = $_.FullName
        $outPath  = Join-Path $OUTPUT_BASE ($relative -replace '\.glsl$', '.bin')
        $outDir   = Split-Path $outPath -Parent
        if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

        $destShort = $outPath.Substring($OUTPUT_BASE.Length + 1)

        $outText = & $SC -f $absolute -o $outPath --type $Type @commonParams 2>&1
        
        $ok      = ($LASTEXITCODE -eq 0)
        Write-Result $relative $destShort $ok $longestPathLength
        
        $outText | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
        $count++
    }
    return $count
}

# ---------- Main ----------
$timer = [Diagnostics.Stopwatch]::StartNew()

$vCnt = Invoke-CompileShaders "*.vert.glsl" "vertex"
$fCnt = Invoke-CompileShaders "*.frag.glsl" "fragment"

$timer.Stop()
$elapsed = [Math]::Round($timer.Elapsed.TotalSeconds,2)

Write-Host ""
Write-Host "Done: $vCnt vertex + $fCnt fragment shaders in $elapsed s" -ForegroundColor Cyan

if ($Errors.Count) {
    Write-Section "-- Errors" "Red"
    $Errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}
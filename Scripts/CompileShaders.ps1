param(
    [ValidateSet('debug','release')]
    [string]$Config = 'debug',

    [string]$Framework = 'net9.0'
)

# ---------- Roots ----------
$ScriptDir     = $PSScriptRoot                       # ...\CustomEngine\Scripts
$SolutionRoot  = (Split-Path $ScriptDir -Parent)     # ...\CustomEngine

# bgfx sits one level *above* the solution by default (to be moved at some point)
$BGFX = Resolve-Path (Join-Path $SolutionRoot '..\bgfx') -ErrorAction SilentlyContinue
if (-not $BGFX) {
    throw "bgfx folder not found next to $SolutionRoot; adjust BGFX path in script."
}
$BGFX = $BGFX.ProviderPath   # make it a plain string

# ---------- Fixed paths (now relative) ----------
$SC          = Join-Path $BGFX '.build\win64_vs2022\bin\shadercRelease.exe'
$SHDR        = Join-Path $SolutionRoot 'Engine.Assets\Materials'
$OUTPUT_BASE = Join-Path $SolutionRoot "Engine.Editor\bin\$Config\$Framework\Compiled\Shaders"

# ---------- Common params ----------
$commonParams = @(
    '--platform','windows',
    '-p','s_5_0',
    '-i', (Join-Path $BGFX 'src'),
    '-i', (Join-Path $BGFX 'examples\common'),
    '--varyingdef', (Join-Path $SHDR 'varying.def.sc')
)

$Errors = @()   # collect shaderc stderr

# ---------- Helper functions ----------
function Write-Section {
    param([string]$Text,[string]$Color='Cyan')
    Write-Host "`n$Text" -ForegroundColor $Color
    Write-Host ('-'*($Text.Length)) -ForegroundColor $Color
}
function Write-Result {
    param([string]$Src,[string]$Dst,[bool]$Ok,[int]$Pad)
    $srcPad = $Src.PadRight($Pad)
    $flag   = if ($Ok) { '[OK] '  } else { '[!!]' }
    $color  = if ($Ok) { 'Green' } else { 'Red'  }
    Write-Host $flag -ForegroundColor $color -NoNewline
    Write-Host " $srcPad -> $Dst"
}

function Invoke-CompileShaders {
    param([string]$Filter,[string]$Type)

    Write-Section "-- Compiling $Type shaders"
    $pad = (Get-ChildItem $SHDR -Filter $Filter -Recurse |
            ForEach-Object {($_.FullName.Substring($SHDR.Length+1)).Length} |
            Measure-Object -Maximum).Maximum + 2

    $count = 0
    Get-ChildItem $SHDR -Filter $Filter -Recurse | ForEach-Object {
        $rel     = $_.FullName.Substring($SHDR.Length+1)
        $outPath = Join-Path $OUTPUT_BASE ($rel -replace '\.glsl$', '.bin')
        $null    = New-Item (Split-Path $outPath) -ItemType Directory -Force

        $stderr  = & $SC -f $_.FullName -o $outPath --type $Type @commonParams 2>&1
        $ok      = ($LASTEXITCODE -eq 0)
        if (-not $ok) { $Errors += $stderr }
        Write-Result $rel ($outPath.Substring($OUTPUT_BASE.Length+1)) $ok $pad
        $count++
    }
    return $count
}

# ---------- Run ----------
$sw = [Diagnostics.Stopwatch]::StartNew()

$vCnt = Invoke-CompileShaders '*.vert.glsl' 'vertex'
$fCnt = Invoke-CompileShaders '*.frag.glsl' 'fragment'

$sw.Stop()
Write-Host "`nDone: $vCnt vertex + $fCnt fragment shaders in $([math]::Round($sw.Elapsed.TotalSeconds,2)) s" -ForegroundColor Cyan

if ($Errors) {
    Write-Section '-- Errors' 'Red'
    $Errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}

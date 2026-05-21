[CmdletBinding()]
param(
    [int]$StartPhase = 1,
    [int]$EndPhase = 7,
    [int]$MaxIterations = 3
)

$ErrorActionPreference = "Stop"
$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$agentsRoot = "$repoRoot\agents-v2"
$pipelineRoot = "$agentsRoot\pipeline"
$logFile = "$pipelineRoot\_logs\orchestrator.log"

if (-not (Test-Path "$pipelineRoot\_logs")) {
    New-Item -ItemType Directory -Force -Path "$pipelineRoot\_logs" | Out-Null
}

function Write-Log {
    param([string]$Message)
    $stamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$stamp] $Message"
    Write-Host $line -ForegroundColor DarkGray
    Add-Content -Path $logFile -Value $line
}

# Pipeline definition. ProducerDir matches the agents-v2 folder name; OutputDir is under pipeline/.
$phases = @(
    @{ Num = 1; Name = "BA";         ProducerDir = "01-ba";        CriticDir = "01-ba-critic";        OutputDir = "01-spec";         HasCritic = $true;  Interactive = $true  }
    @{ Num = 2; Name = "Architect";  ProducerDir = "02-architect"; CriticDir = "02-architect-critic"; OutputDir = "02-architecture"; HasCritic = $true;  Interactive = $false }
    @{ Num = 3; Name = "Data";       ProducerDir = "03-data";      CriticDir = "03-data-critic";      OutputDir = "03-data";         HasCritic = $true;  Interactive = $false }
    @{ Num = 4; Name = "Backend";    ProducerDir = "04-backend";   CriticDir = "04-backend-critic";   OutputDir = "04-backend";      HasCritic = $true;  Interactive = $false }
    @{ Num = 5; Name = "Frontend";   ProducerDir = "05-frontend";  CriticDir = "05-frontend-critic";  OutputDir = "05-frontend";     HasCritic = $true;  Interactive = $false }
    @{ Num = 6; Name = "QA";         ProducerDir = "06-qa";        CriticDir = "06-qa-critic";        OutputDir = "06-qa";           HasCritic = $true;  Interactive = $false }
    @{ Num = 7; Name = "Deployment"; ProducerDir = "07-deployment"; CriticDir = $null;                OutputDir = "07-deployment";   HasCritic = $false; Interactive = $false }
)

function Get-Verdict {
    param([string]$CriticFile)
    if (-not (Test-Path $CriticFile)) { return "MISSING" }
    $lines = Get-Content $CriticFile -ErrorAction SilentlyContinue
    if (-not $lines) { return "EMPTY" }
    # Find the last non-empty line
    for ($i = $lines.Length - 1; $i -ge 0; $i--) {
        $line = $lines[$i].Trim()
        if ($line) {
            if ($line -match '^VERDICT:\s*APPROVED\s*$') { return "APPROVED" }
            if ($line -match '^VERDICT:\s*BLOCKED\s*$')  { return "BLOCKED" }
            return "MALFORMED: $line"
        }
    }
    return "EMPTY"
}

function Invoke-Phase {
    param($Phase)

    $num = $Phase.Num
    $name = $Phase.Name
    Write-Log "=== Phase $num : $name ==="

    # Phase 1 interactive special-case: if spec.md doesn't exist yet, run BA without -Auto first.
    $specPath = "$pipelineRoot\01-spec\spec.md"
    $runInteractiveBA = ($num -eq 1) -and ($Phase.Interactive) -and (-not (Test-Path $specPath))

    for ($iter = 1; $iter -le $MaxIterations; $iter++) {
        Write-Log "Phase $num : producer iteration $iter"

        $producerScript = "$agentsRoot\$($Phase.ProducerDir)\run.ps1"

        if ($runInteractiveBA -and $iter -eq 1) {
            Write-Host "`n>>> Launching BA in INTERACTIVE mode. Converse with the BA to capture the idea, then ask it to write the spec." -ForegroundColor Yellow
            Write-Host ">>> When the spec is written and you exit Claude, the orchestrator will resume.`n" -ForegroundColor Yellow
            & $producerScript -Iteration $iter
        } else {
            & $producerScript -Auto -Iteration $iter
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Log "Phase $num producer exited with code $LASTEXITCODE - halting."
            return $false
        }

        if (-not $Phase.HasCritic) {
            Write-Log "Phase $num has no critic - advancing."
            return $true
        }

        Write-Log "Phase $num : critic iteration $iter"
        $criticScript = "$agentsRoot\$($Phase.CriticDir)\run.ps1"
        & $criticScript -Auto -Iteration $iter

        if ($LASTEXITCODE -ne 0) {
            Write-Log "Phase $num critic exited with code $LASTEXITCODE - halting."
            return $false
        }

        $criticFile = "$pipelineRoot\$($Phase.OutputDir)\critic-$iter.md"
        $verdict = Get-Verdict -CriticFile $criticFile
        Write-Log "Phase $num iteration $iter verdict: $verdict"

        if ($verdict -eq "APPROVED") {
            Write-Host "`n[Phase $num : $name] APPROVED on iteration $iter`n" -ForegroundColor Green
            return $true
        }
        elseif ($verdict -eq "BLOCKED") {
            Write-Host "[Phase $num : $name] BLOCKED on iteration $iter - re-running producer" -ForegroundColor Yellow
            $runInteractiveBA = $false
        }
        else {
            Write-Log "Phase $num : critic output malformed/missing ($verdict) - halting for human review."
            Write-Host "[Phase $num : $name] Critic verdict could not be parsed: $verdict" -ForegroundColor Red
            Write-Host "Inspect $criticFile and either fix the critic or set verdict manually." -ForegroundColor Red
            return $false
        }
    }

    Write-Log "Phase $num : exhausted $MaxIterations iterations without APPROVED."
    Write-Host "[Phase $num : $name] HALTED - $MaxIterations iterations without approval. Human intervention required." -ForegroundColor Red
    return $false
}

Write-Log "Orchestrator start. StartPhase=$StartPhase EndPhase=$EndPhase MaxIterations=$MaxIterations"

foreach ($phase in $phases) {
    if ($phase.Num -lt $StartPhase) { continue }
    if ($phase.Num -gt $EndPhase)   { break }

    $ok = Invoke-Phase -Phase $phase
    if (-not $ok) {
        Write-Log "Orchestrator halted at phase $($phase.Num)."
        exit 1
    }
}

Write-Log "Orchestrator complete."
Write-Host "`nAll phases complete. Artifacts under $pipelineRoot." -ForegroundColor Green

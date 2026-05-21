[CmdletBinding()]
param(
    [int]$StartPhase = 1,
    [int]$EndPhase = 9,
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
# ProducerVerdictFile: when HasCritic is $false but this is set, the orchestrator parses
# VERDICT from the named file in OutputDir instead of looking for a critic-N.md.
$phases = @(
    @{ Num = 1; Name = "BA";         ProducerDir = "01-ba";         CriticDir = "01-ba-critic";        OutputDir = "01-spec";         HasCritic = $true;  Interactive = $true;  ProducerVerdictFile = $null }
    @{ Num = 2; Name = "Architect";  ProducerDir = "02-architect";  CriticDir = "02-architect-critic"; OutputDir = "02-architecture"; HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 3; Name = "UI/UX";      ProducerDir = "03-uiux";       CriticDir = "03-uiux-critic";      OutputDir = "03-uiux";         HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 4; Name = "Data";       ProducerDir = "04-data";       CriticDir = "04-data-critic";      OutputDir = "04-data";         HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 5; Name = "Backend";    ProducerDir = "05-backend";    CriticDir = "05-backend-critic";   OutputDir = "05-backend";      HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 6; Name = "Frontend";   ProducerDir = "06-frontend";   CriticDir = "06-frontend-critic";  OutputDir = "06-frontend";     HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 7; Name = "QA";         ProducerDir = "07-qa";         CriticDir = "07-qa-critic";        OutputDir = "07-qa";           HasCritic = $true;  Interactive = $false; ProducerVerdictFile = $null }
    @{ Num = 8; Name = "Security";   ProducerDir = "08-security";   CriticDir = $null;                 OutputDir = "08-security";     HasCritic = $false; Interactive = $false; ProducerVerdictFile = "report.md" }
    @{ Num = 9; Name = "Deployment"; ProducerDir = "09-deployment"; CriticDir = $null;                 OutputDir = "09-deployment";   HasCritic = $false; Interactive = $false; ProducerVerdictFile = $null }
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
            # No critic - either advance silently (e.g. deployment) or parse producer's own verdict file (e.g. security).
            if ($Phase.ProducerVerdictFile) {
                $verdictFile = "$pipelineRoot\$($Phase.OutputDir)\$($Phase.ProducerVerdictFile)"
                $verdict = Get-Verdict -CriticFile $verdictFile
                Write-Log "Phase $num producer-verdict: $verdict (from $($Phase.ProducerVerdictFile))"
                if ($verdict -eq "APPROVED") {
                    Write-Host "`n[Phase $num : $name] APPROVED (producer verdict)`n" -ForegroundColor Green
                    return $true
                }
                elseif ($verdict -eq "BLOCKED") {
                    Write-Host "[Phase $num : $name] BLOCKED by producer verdict - halting for human review." -ForegroundColor Red
                    Write-Host "Inspect $verdictFile for the findings." -ForegroundColor Red
                    return $false
                }
                else {
                    Write-Host "[Phase $num : $name] Producer verdict could not be parsed: $verdict" -ForegroundColor Red
                    Write-Host "Inspect $verdictFile." -ForegroundColor Red
                    return $false
                }
            }
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

param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Deployment] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the Deployment agent. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
Inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\04-backend\summary.md
  - $repoRoot\agents-v2\pipeline\05-frontend\summary.md
  - $repoRoot\agents-v2\pipeline\06-qa\summary.md

Write:
  - Bicep + pipelines under infra\ and .github\workflows\ (or azure-pipelines.yml)
  - Summary: $repoRoot\agents-v2\pipeline\07-deployment\summary.md
  - Runbook: $repoRoot\agents-v2\pipeline\07-deployment\runbook.md

Only the dev environment is fully populated. qa / staging / prod are skeletons.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

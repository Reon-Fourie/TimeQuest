param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[QA] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the QA / Tester agent. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
Inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\04-backend\summary.md
  - $repoRoot\agents-v2\pipeline\05-frontend\summary.md
  - any critic-*.md under $repoRoot\agents-v2\pipeline\06-qa\

Write:
  - Test plan: $repoRoot\agents-v2\pipeline\06-qa\test-plan.md
  - Playwright project: tests\e2e\ (use whichever path the Architect specified)
  - Summary: $repoRoot\agents-v2\pipeline\06-qa\summary.md
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Backend Dev] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the Backend Developer. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
Inputs to read:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\03-uiux\design.md
  - $repoRoot\agents-v2\pipeline\04-data\design.md
  - any critic-*.md under $repoRoot\agents-v2\pipeline\05-backend\

Write summary to: $repoRoot\agents-v2\pipeline\05-backend\summary.md
Implementation goes under the project structure the Architect specified.

Run 'dotnet build' and 'dotnet test' before finalising summary.md. Real results only.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

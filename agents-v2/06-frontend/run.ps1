param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Frontend Dev] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the Frontend Developer. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
Read inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\03-uiux\design.md
  - $repoRoot\agents-v2\pipeline\04-data\design.md
  - $repoRoot\agents-v2\pipeline\05-backend\summary.md
  - The actual backend source for DTO signatures.
  - any critic-*.md under $repoRoot\agents-v2\pipeline\06-frontend\

Write summary to: $repoRoot\agents-v2\pipeline\06-frontend\summary.md
Run 'dotnet build' and (if bUnit tests exist) 'dotnet test' before writing summary.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

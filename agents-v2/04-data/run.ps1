param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir
Write-Host "[Data Designer] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the Data Designer. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\03-uiux\design.md
  - any critic-*.md under $repoRoot\agents-v2\pipeline\04-data\

Write your output to: $repoRoot\agents-v2\pipeline\04-data\design.md
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

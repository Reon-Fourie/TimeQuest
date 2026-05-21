param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir
Write-Host "[UI/UX Designer] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the UI/UX Designer. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - any critic-*.md in $repoRoot\agents-v2\pipeline\03-uiux\

Write your output to: $repoRoot\agents-v2\pipeline\03-uiux\design.md
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

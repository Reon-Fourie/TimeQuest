param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir
Write-Host "[Architect] iteration=$Iteration" -ForegroundColor Green

$prompt = @"
You are the System Architect. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Read inputs:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - any critic-*.md in $repoRoot\agents-v2\pipeline\02-architecture\

Write your output to: $repoRoot\agents-v2\pipeline\02-architecture\design.md

If the spec is missing or empty, write a single-line file noting the missing input and stop.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir
Write-Host "[Data Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the Data Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Review: $repoRoot\agents-v2\pipeline\03-data\design.md
Cross-check: $repoRoot\agents-v2\pipeline\01-spec\spec.md and $repoRoot\agents-v2\pipeline\02-architecture\design.md
Write critique to: $repoRoot\agents-v2\pipeline\03-data\critic-$Iteration.md

LAST LINE must be 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir
Write-Host "[UI/UX Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the UI/UX Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Design to review: $repoRoot\agents-v2\pipeline\03-uiux\design.md
Cross-check: $repoRoot\agents-v2\pipeline\01-spec\spec.md and $repoRoot\agents-v2\pipeline\02-architecture\design.md
Write critique to: $repoRoot\agents-v2\pipeline\03-uiux\critic-$Iteration.md

LAST LINE must be 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

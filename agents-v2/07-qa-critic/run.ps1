param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-haiku-4-5-20251001"

Set-Location $repoRoot
Write-Host "[QA Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the QA Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Review: $repoRoot\agents-v2\pipeline\07-qa\test-plan.md and $repoRoot\agents-v2\pipeline\07-qa\summary.md
Cross-check: $repoRoot\agents-v2\pipeline\01-spec\spec.md
Inspect (do not run): tests/e2e/ project structure
Write critique to: $repoRoot\agents-v2\pipeline\07-qa\critic-$Iteration.md

LAST LINE: 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

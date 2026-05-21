param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-haiku-4-5-20251001"

Set-Location $agentDir
$env:ITERATION = "$Iteration"

Write-Host "[BA Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the BA Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Spec to review: $repoRoot\agents-v2\pipeline\01-spec\spec.md
User intent: $repoRoot\agents-v2\pipeline\00-input\user-prompt.md
Write your critique to: $repoRoot\agents-v2\pipeline\01-spec\critic-$Iteration.md

The LAST LINE of your output file MUST be either 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

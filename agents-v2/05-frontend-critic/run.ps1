param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Frontend Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the Frontend Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
You MAY run 'dotnet build' / 'dotnet test'.

Review summary: $repoRoot\agents-v2\pipeline\05-frontend\summary.md
Cross-check spec + backend summary.
Write critique to: $repoRoot\agents-v2\pipeline\05-frontend\critic-$Iteration.md

LAST LINE: 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

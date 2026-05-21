param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Backend Critic] iteration=$Iteration" -ForegroundColor Cyan

$prompt = @"
You are the Backend Critic. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
You MAY run 'dotnet build' and 'dotnet test' to verify the dev's claims.

Review summary: $repoRoot\agents-v2\pipeline\04-backend\summary.md
Cross-check: spec.md, design.md (architecture + data) in their respective phase dirs.
Write critique to: $repoRoot\agents-v2\pipeline\04-backend\critic-$Iteration.md

LAST LINE must be 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model -p $prompt
}

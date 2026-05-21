param(
    [switch]$Auto,
    [int]$Iteration = 1
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $repoRoot
Write-Host "[Security Review] iteration=$Iteration" -ForegroundColor Magenta

$prompt = @"
You are the Security Reviewer. Read CLAUDE.md in $agentDir.

Iteration: $Iteration
Repo root: $repoRoot
You MAY read any file in the repo (backend source, frontend source, infra, configs, csproj, package.json).

Pipeline inputs to read first:
  - $repoRoot\agents-v2\pipeline\01-spec\spec.md
  - $repoRoot\agents-v2\pipeline\02-architecture\design.md
  - $repoRoot\agents-v2\pipeline\04-data\design.md
  - $repoRoot\agents-v2\pipeline\05-backend\summary.md
  - $repoRoot\agents-v2\pipeline\06-frontend\summary.md
  - $repoRoot\agents-v2\pipeline\07-qa\summary.md

Then sweep the codebase per the checklist in your CLAUDE.md.

Write the report to: $repoRoot\agents-v2\pipeline\08-security\report.md
LAST LINE must be 'VERDICT: APPROVED' or 'VERDICT: BLOCKED'.

Verdict rules:
  - BLOCKED if any Critical finding
  - BLOCKED if any High finding exploitable without authentication
  - APPROVED otherwise (auth-gated High findings recorded for post-deploy fix)
"@

if ($Auto) {
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    claude --model $model
}

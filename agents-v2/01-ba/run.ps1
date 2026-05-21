param(
    [switch]$Auto,
    [int]$Iteration = 1,
    [string]$CriticFeedbackFile = ""
)

$agentDir = $PSScriptRoot
$repoRoot = (Resolve-Path "$agentDir\..\..").Path
$model = "claude-sonnet-4-6"

Set-Location $agentDir

Write-Host "[BA Agent] iteration=$Iteration auto=$Auto" -ForegroundColor Green

$feedbackBlock = ""
if ($CriticFeedbackFile -and (Test-Path $CriticFeedbackFile)) {
    $feedbackBlock = "`n## Critic feedback to address`n" + (Get-Content $CriticFeedbackFile -Raw)
}

if ($Auto) {
    $prompt = @"
You are the Business Analyst agent. Read CLAUDE.md in $agentDir for your full role.

Repo: $repoRoot
Pipeline dir: $repoRoot\agents-v2\pipeline

This is iteration $Iteration. Read the existing user-prompt.md and any prior spec.md / critic feedback. Refine, then overwrite agents-v2/pipeline/01-spec/spec.md.
$feedbackBlock

Do not ask the user clarifying questions in this run (non-interactive). If the input is ambiguous, list the ambiguities in section 6 (Open Questions) of the spec.
"@
    claude --model $model --dangerously-skip-permissions -p $prompt
} else {
    Write-Host "Interactive mode. Tell BA your project idea. When satisfied with the captured idea, say 'write the spec'." -ForegroundColor Yellow
    Write-Host "BA will read CLAUDE.md and conduct the intake." -ForegroundColor Gray
    Write-Host ""
    claude --model $model
}

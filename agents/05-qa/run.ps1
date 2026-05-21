# QA Agent — Run Script
# Model: claude-opus-4-7
# Role: Spec compliance, build health, security review, startup verification

param(
    [switch]$Auto
)

$agentDir = $PSScriptRoot
Set-Location $agentDir

Write-Host "Starting QA Agent (claude-opus-4-7)..." -ForegroundColor Magenta
Write-Host "Role: Spec compliance, code review, security check, startup verification" -ForegroundColor Gray
Write-Host "Working directory: $agentDir" -ForegroundColor Gray

if ($Auto) {
    $task = Get-Content "$agentDir\requirements.md" -Raw
    claude --model claude-opus-4-7 --dangerously-skip-permissions -p $task
} else {
    Write-Host ""
    Write-Host "Interactive mode. Start by saying:" -ForegroundColor Yellow
    Write-Host "  'Read requirements.md and execute the full QA review.'" -ForegroundColor White
    Write-Host ""
    claude --model claude-opus-4-7
}

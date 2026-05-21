# Repository Agent — Run Script
# Model: claude-sonnet-4-6
# Tasks: 13-19 (LevelHelper, Generic + 5 specific repositories, xUnit tests)

param(
    [switch]$Auto
)

$agentDir = $PSScriptRoot
Set-Location $agentDir

Write-Host "Starting Repository Agent (claude-sonnet-4-6)..." -ForegroundColor Green
Write-Host "Tasks: 13-19 — LevelHelper, Repositories, xUnit test project" -ForegroundColor Gray
Write-Host "Working directory: $agentDir" -ForegroundColor Gray

if ($Auto) {
    $task = Get-Content "$agentDir\requirements.md" -Raw
    claude --model claude-sonnet-4-6 --dangerously-skip-permissions -p $task
} else {
    Write-Host ""
    Write-Host "Interactive mode. Start by saying:" -ForegroundColor Yellow
    Write-Host "  'Read requirements.md and execute all tasks.'" -ForegroundColor White
    Write-Host ""
    claude --model claude-sonnet-4-6
}

# Services Agent — Run Script
# Model: claude-sonnet-4-6
# Tasks: 20-25 (XpService, BadgeService, TimesheetService, TeamService, ReportingService, SeedData)

param(
    [switch]$Auto
)

$agentDir = $PSScriptRoot
Set-Location $agentDir

Write-Host "Starting Services Agent (claude-sonnet-4-6)..." -ForegroundColor Green
Write-Host "Tasks: 20-25 — XpService, BadgeService, TimesheetService, TeamService, ReportingService, SeedData" -ForegroundColor Gray
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

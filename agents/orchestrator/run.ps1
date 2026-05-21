# Orchestrator Agent — Run Script
# Model: claude-opus-4-7
# Role: Coordinates all build agents in sequence

param(
    [switch]$Auto  # Pass -Auto for non-interactive mode
)

$agentDir = $PSScriptRoot
Set-Location $agentDir

Write-Host "Starting TimeQuest Orchestrator Agent (claude-opus-4-7)..." -ForegroundColor Cyan
Write-Host "Working directory: $agentDir" -ForegroundColor Gray

if ($Auto) {
    $task = Get-Content "$agentDir\requirements.md" -Raw
    claude --model claude-opus-4-7 --dangerously-skip-permissions -p $task
} else {
    Write-Host ""
    Write-Host "Interactive mode. The agent will read CLAUDE.md and requirements.md automatically." -ForegroundColor Yellow
    Write-Host "Type 'Execute the requirements in requirements.md' to start." -ForegroundColor Yellow
    Write-Host ""
    claude --model claude-opus-4-7
}

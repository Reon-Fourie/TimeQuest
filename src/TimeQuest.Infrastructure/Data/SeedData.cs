using Microsoft.AspNetCore.Identity;

namespace TimeQuest.Infrastructure.Data;

/// <summary>
/// Seeds initial data at application startup. Idempotent — safe to run on every startup.
/// </summary>
public static class SeedData
{
    private static readonly string[] Roles =
    {
        "TeamMember",
        "TeamLead",
        "Administrator",
        "FinancialAdmin",
        "SystemAdmin"
    };

    /// <summary>
    /// Seeds the five application roles using RoleManager (idempotent).
    /// Called from Program.cs after the app is built.
    /// </summary>
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
    {
        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
        }
    }
}

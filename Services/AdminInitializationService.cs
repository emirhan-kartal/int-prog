using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class AdminInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminInitializationService> _logger;

    public AdminInitializationService(
        IServiceProvider serviceProvider,
        ILogger<AdminInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                _logger.LogInformation("Created Admin role");
            }

            var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
            if (!adminUsers.Any())
            {
                var firstUser = await userManager.Users
                    .OrderBy(u => u.CreatedAt)
                    .FirstOrDefaultAsync();

                if (firstUser != null)
                {
                    var result = await userManager.AddToRoleAsync(firstUser, "Admin");
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Made first user {firstUser.Email} an admin");
                    }
                    else
                    {
                        _logger.LogError($"Failed to make first user admin: {string.Join(", ", result.Errors)}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin role initialization");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
} 
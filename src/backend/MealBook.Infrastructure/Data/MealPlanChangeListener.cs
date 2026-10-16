using MealBook.Application.Planning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MealBook.Infrastructure.Data;

// Listens for Postgres notifications so that every app instance can push plan changes to its own
// connected browsers, no matter which instance handled the request.
public sealed class MealPlanChangeListener(
    string connectionString,
    IPlanChangeBroadcaster broadcaster,
    ILogger<MealPlanChangeListener> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken);
                connection.Notification += (_, _) => _ = BroadcastAsync(stoppingToken);
                await using (var listen = new NpgsqlCommand($"LISTEN {PgMealPlanNotifier.Channel}", connection))
                {
                    await listen.ExecuteNonQueryAsync(stoppingToken);
                }

                logger.LogInformation("Listening for meal plan changes.");
                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Meal plan listener lost its connection; retrying.");
                try
                {
                    await Task.Delay(RetryDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        try
        {
            await broadcaster.BroadcastAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not broadcast a meal plan change.");
        }
    }
}

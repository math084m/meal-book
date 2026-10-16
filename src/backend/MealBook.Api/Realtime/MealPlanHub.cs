using MealBook.Application.Planning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MealBook.Api.Realtime;

[Authorize]
public sealed class MealPlanHub : Hub
{
    public const string Route = "/api/hubs/mealplan";
    public const string PlanChangedMethod = "planChanged";
}

public sealed class MealPlanHubBroadcaster(IHubContext<MealPlanHub> hub) : IPlanChangeBroadcaster
{
    public Task BroadcastAsync(CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(MealPlanHub.PlanChangedMethod, cancellationToken);
}

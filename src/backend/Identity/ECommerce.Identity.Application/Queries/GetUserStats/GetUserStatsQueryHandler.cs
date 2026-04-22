namespace ECommerce.Identity.Application.Queries.GetUserStats;

public class GetUserStatsQueryHandler(IUserRepository users)
    : IRequestHandler<GetUserStatsQuery, Result<UserStatsDto>>
{
    public async Task<Result<UserStatsDto>> Handle(GetUserStatsQuery query, CancellationToken ct)
    {
        var totalCustomers = await users.GetCustomersCountAsync(ct);
        return Result<UserStatsDto>.Ok(new UserStatsDto { TotalCustomers = totalCustomers });
    }
}

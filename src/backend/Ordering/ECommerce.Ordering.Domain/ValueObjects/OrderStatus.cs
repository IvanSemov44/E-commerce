using ECommerce.SharedKernel.Domain;
using ECommerce.SharedKernel.Results;

namespace ECommerce.Ordering.Domain.ValueObjects;

public class OrderStatus : ValueObject
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Confirmed = new(2, "Confirmed");
    public static readonly OrderStatus Processing = new(3, "Processing");
    public static readonly OrderStatus Shipped = new(4, "Shipped");
    public static readonly OrderStatus Delivered = new(5, "Delivered");
    public static readonly OrderStatus Cancelled = new(6, "Cancelled");

    public int Id { get; }
    public string Name { get; } = null!;

    private static readonly IReadOnlyDictionary<int, OrderStatus> All = new Dictionary<int, OrderStatus>
    {
        { 1, Pending }, { 2, Confirmed }, { 3, Processing },
        { 4, Shipped }, { 5, Delivered }, { 6, Cancelled }
    };

    private OrderStatus() { }
    private OrderStatus(int id, string name) { Id = id; Name = name; }

    public static OrderStatus FromName(string name)
        => All.Values.FirstOrDefault(s => s.Name == name) ?? Pending;

    public bool CanTransitionTo(OrderStatus next)
    {
        return (Id, next.Id) switch
        {
            (1, 2) => true,
            (1, 6) => true,
            (2, 3) => true,
            (2, 6) => true,
            (3, 4) => true,
            (3, 6) => true,
            (4, 5) => true,
            _ => false
        };
    }

    public override string ToString() => Name;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
    }
}

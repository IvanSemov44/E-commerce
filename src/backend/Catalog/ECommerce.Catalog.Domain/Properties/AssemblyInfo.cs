using System.Runtime.CompilerServices;

// Grants EF Core entity configurations in Infrastructure access to internal child entities.
[assembly: InternalsVisibleTo("ECommerce.Catalog.Infrastructure")]

namespace ECommerce.SharedKernel.Interfaces;

/// <summary>
/// Commands that implement this interface are automatically wrapped in a database
/// transaction by TransactionBehavior in the MediatR pipeline.
/// Queries must NEVER implement this.
/// </summary>
public interface ITransactionalCommand { }

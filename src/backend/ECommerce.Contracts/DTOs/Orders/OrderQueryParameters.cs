using ECommerce.Contracts.DTOs.Common;

namespace ECommerce.Contracts.DTOs.Orders;

/// <summary>
/// Query parameters for order listing endpoints.
/// Inherits page, pageSize, search, sortBy, sortOrder from RequestParameters.
/// Currently pagination-only. Add status, dateRange, and other filters here when needed in future sprints.
/// </summary>
public class OrderQueryParameters : RequestParameters { }


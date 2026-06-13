namespace ECommerce.SharedKernel.Results;

public enum ErrorType
{
    Failure,      // 400
    Validation,   // 422
    NotFound,     // 404
    Conflict,     // 409
    Unauthorized, // 401
    Forbidden     // 403
}

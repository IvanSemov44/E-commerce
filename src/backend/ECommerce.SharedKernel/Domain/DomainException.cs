namespace ECommerce.SharedKernel.Domain;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException() : base()
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string message) : base(message)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
        Code = "DOMAIN_ERROR";
    }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }

    public DomainException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }
}

namespace ControlDeDinero.Application;

/// <summary>Thrown when the input of a use case is not valid. The API maps it to HTTP 400.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

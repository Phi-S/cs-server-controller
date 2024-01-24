using ErrorOr;

namespace PracPlugin.ErrorsExtension;

public static class Errors
{
    public static Error Fail(string message)
    {
        return Error.Failure(description: message);
    }
    
    public static string ErrorMessage<TValue>(this ErrorOr<TValue> error)
    {
        return error.FirstError.Description;
    }
}
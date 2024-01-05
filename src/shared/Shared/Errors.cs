using ErrorOr;

namespace Shared;

public static class Errors
{
    public static string ErrorMessage<TValue>(this ErrorOr<TValue> error)
    {
        return error.FirstError.Description;
    }

    public static Error Fail(string description = "A failure has occurred.")
    {
        return Error.Failure("General.Failure", description);
    }
}
using System.Diagnostics.Contracts;

namespace ResultLib;

public enum ResultState : byte
{
    FAILED,
    OK
}

public readonly struct Result
{
    #region Static

    public static Result Ok()
    {
        return new Result();
    }

    public static Result Ok(string okMessage)
    {
        return new Result(okMessage);
    }

    public static Result Fail(Exception e)
    {
        return new Result(e);
    }

    public static Result Fail(string errorMessage)
    {
        var e = new ErrorMessageException(errorMessage);
        return new Result(e);
    }

    public static Result Fail(Exception exception, string errorMessage)
    {
        var e = new ErrorMessageException(errorMessage, exception);
        return new Result(e);
    }

    #endregion

    private ResultState State { get; }

    private readonly Exception? _exception;


    /// <summary>
    /// Gets the exception set by the Result.Fail method
    /// </summary>
    /// <exception cref="ResultException">Throws an exception if the result sate is OK</exception>
    public Exception Exception
    {
        get
        {
            if (IsOk)
            {
                throw new ResultException($"Cant get exception if the result state is {ResultState.OK}");
            }

            return _exception!;
        }
        private init => _exception = value;
    }

    public string? OkMessage { get; init; }

    public bool IsFailed => State == ResultState.FAILED;
    public bool IsOk => State == ResultState.OK;
    public bool GotOkMessage => string.IsNullOrWhiteSpace(OkMessage) == false;

    public Result()
    {
        State = ResultState.OK;
        OkMessage = null;
        Exception = null;
    }

    private Result(string okMessage)
    {
        State = ResultState.OK;
        OkMessage = okMessage;
        Exception = null;
    }

    private Result(Exception exception)
    {
        State = ResultState.FAILED;
        Exception = exception;
    }

    public bool OnFailed(out Exception? exception)
    {
        exception = null;
        if (IsFailed == false)
        {
            return false;
        }

        exception = Exception!;
        return true;
    }
}

public readonly struct Result<TA>
{
    #region Static

    public static Result<TA> Ok(TA value)
    {
        return new Result<TA>(value);
    }

    public static Result<TA> Fail(Exception e)
    {
        return new Result<TA>(e);
    }

    public static Result<TA> Fail(string errorMessage)
    {
        return new Result<TA>(new ErrorMessageException(errorMessage));
    }

    #endregion


    public ResultState State { get; init; }

    private readonly Exception? _exception;
    private readonly TA? _value;

    /// <summary>
    /// Gets the exception set by the Result.Fail method
    /// </summary>
    /// <exception cref="ResultException">Throws an exception if the result sate is OK</exception>
    public Exception Exception
    {
        get
        {
            if (IsOk)
            {
                throw new ResultException($"Cant get exception if the result state is {ResultState.OK}");
            }

            return _exception!;
        }
        private init => _exception = value;
    }

    public TA Value
    {
        get
        {
            if (IsFailed)
            {
                throw new ResultException($"Cant get value if the result state is {ResultState.FAILED}");
            }

            return _value!;
        }
        private init => _value = value;
    }

    public bool IsFailed => State == ResultState.FAILED;
    public bool IsOk => State == ResultState.OK;


    private Result(TA? value)
    {
        State = ResultState.OK;
        Value = value;
        Exception = default;
    }

    private Result(Exception exception)
    {
        State = ResultState.FAILED;
        Value = default;
        Exception = exception;
    }

    public void OnOk(Action okAction)
    {
        if (IsOk)
        {
            okAction.Invoke();
        }
    }

    public async Task OnOk(Func<TA, Task> okAction)
    {
        if (IsOk)
        {
            await okAction.Invoke(Value!);
        }
    }

    public async Task<TAa?> OnOk<TAa>(Func<TA, Task<TAa>> okAction)
    {
        if (IsOk)
        {
            return await okAction.Invoke(Value!);
        }

        return default;
    }

    [Pure]
    public static implicit operator Result<TA>(TA value) =>
        new(value);

    [Pure]
    public static implicit operator Result<TA>(Exception exception) =>
        new(exception);
}
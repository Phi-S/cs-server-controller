namespace Web.Misc;

public class ThreadSaveData<T>
{
    public event Action<T>? OnChange;
    private readonly object _dataLock = new();
    private T? _data;

    public void Set(T data)
    {
        lock (_dataLock)
        {
            _data = data;
            OnChange?.Invoke(_data);
        }
    }
    
    public T? Get()
    {
        lock (_dataLock)
        {
            return _data;
        }
    }
}
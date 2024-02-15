namespace Web.Misc;

public class ThreadSaveList<T>
{
    public event Action? OnChange;
    public event Func<T, Task>? OnAdd;
    private readonly object _listLock = new();
    private readonly List<T> _list = [];

    public void Set(IEnumerable<T> data)
    {
        lock (_listLock)
        {
            _list.Clear();
            _list.AddRange(data);
        }
    }

    public void Add(T data)
    {
        lock (_listLock)
        {
            _list.Add(data);
            OnChange?.Invoke();
            OnAdd?.Invoke(data);
        }
    }

    public IReadOnlyCollection<T> Get()
    {
        lock (_listLock)
        {
            return _list.AsReadOnly();
        }
    }

    public T[] GetCopy()
    {
        lock (_listLock)
        {
            return _list.ToArray();
        }
    }
}
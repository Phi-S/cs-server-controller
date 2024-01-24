namespace Web.Misc;

public class ThreadSaveList<T>
{
    public event Action? OnChange;
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
        }
    }

    public void AddRange(IEnumerable<T> data)
    {
        lock (_listLock)
        {
            _list.AddRange(data);
            OnChange?.Invoke();
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
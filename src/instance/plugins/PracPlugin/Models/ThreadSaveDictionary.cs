using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PracPlugin.Models;

public class ThreadSaveDictionary<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _storage = new();
    private readonly object _storageLock = new();

    public bool Get(TKey playerController, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_storageLock)
        {
            if (_storage.TryGetValue(playerController, out var storageValue))
            {
                value = storageValue;
                return true;
            }

            value = default;
            return false;
        }
    }

    public bool AddOrUpdate(TKey player, TValue value)
    {
        lock (_storageLock)
        {
            if (_storage.TryGetValue(player, out var currentValue))
            {
                if (_storage.TryUpdate(player, value, currentValue))
                {
                    return true;
                }
            }
            else
            {
                if (_storage.TryAdd(player, value))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool Remove(TKey key)
    {
        lock (_storageLock)
        {
            return _storage.TryRemove(key, out _);
        }
    }

    public List<TValue> Values
    {
        get
        {
            lock (_storageLock)
            {
                return _storage.Values.ToList();
            }
        }
    }

    public List<TKey> Keys
    {
        get
        {
            lock (_storageLock)
            {
                return _storage.Keys.ToList();
            }
        }
    }


    public bool ContainsKey(TKey key)
    {
        lock (_storageLock)
        {
            return _storage.ContainsKey(key);
        }
    }
}
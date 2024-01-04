namespace Shared.ApiModels;

public record EventLogResponse(string EventName, DateTime TriggeredAtUtc)
{
    public virtual bool Equals(EventLogResponse? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(EventName, other.EventName, StringComparison.CurrentCulture) &&
               TriggeredAtUtc.Equals(other.TriggeredAtUtc);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (StringComparer.CurrentCulture.GetHashCode(EventName) * 397) ^ TriggeredAtUtc.GetHashCode();
        }
    }
}
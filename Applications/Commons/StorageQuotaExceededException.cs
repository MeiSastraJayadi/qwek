namespace Qwek.Applications.Commons;

public class StorageQuotaExceededException : Exception
{
    public StorageQuotaExceededException(long limitBytes)
        : this(limitBytes, "The storage limit has been reached. Try again later.")
    {
    }

    public StorageQuotaExceededException(long limitBytes, string message)
        : base(message)
    {
        LimitBytes = limitBytes;
    }

    public long LimitBytes { get; }
}

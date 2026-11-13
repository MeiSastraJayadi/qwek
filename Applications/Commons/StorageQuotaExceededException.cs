namespace Qwek.Applications.Commons;

public class StorageQuotaExceededException : Exception
{
    public StorageQuotaExceededException(long limitBytes)
        : base("The storage limit has been reached. Try again later.")
    {
        LimitBytes = limitBytes;
    }

    public long LimitBytes { get; }
}

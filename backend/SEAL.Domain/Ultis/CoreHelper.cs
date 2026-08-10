namespace SEAL_Domain.Ultis
{
    public class CoreHelper
    {
        // Always store time in UTC for database compatibility (e.g. PostgreSQL timestamptz requires offset 0)
        public static DateTimeOffset SystemTimeNow => DateTimeOffset.UtcNow;
    }
}

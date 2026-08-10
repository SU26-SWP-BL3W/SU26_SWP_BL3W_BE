namespace SEAL_Domain.Ultis
{
    public class TimeHelper
    {
        public static DateTimeOffset ConvertToUtcPlus7(DateTimeOffset dateTimeOffset)
        {
            TimeSpan utcPlus7Offset = new(7, 0, 0);
            return dateTimeOffset.ToOffset(utcPlus7Offset);
        }
    }
}

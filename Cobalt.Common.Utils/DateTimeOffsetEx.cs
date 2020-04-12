using System;

namespace Cobalt.Common.Utils
{
    public static class DateTimeOffsetEx
    {
        public static DateTimeOffset StartOfWeek(this DateTimeOffset d)
        {
            return d.AddDays(-(int)d.DayOfWeek);
        }

        public static DateTimeOffset EndOfWeek(this DateTimeOffset d)
        {
            return d.StartOfWeek().AddDays(7);
        }

        public static DateTimeOffset StartOfMonth(this DateTimeOffset d)
        {
            return d.AddDays(1 - d.Day);
        }

        public static DateTimeOffset EndOfMonth(this DateTimeOffset d)
        {
            return d.StartOfMonth().AddMonths(1);
        }

        public static DateTimeOffset Min(this DateTimeOffset d1, DateTimeOffset d2)
        {
            return d1 < d2 ? d1 : d2;
        }

        public static DateTimeOffset Max(this DateTimeOffset d1, DateTimeOffset d2)
        {
            return d1 < d2 ? d2 : d1;
        }
    }
}

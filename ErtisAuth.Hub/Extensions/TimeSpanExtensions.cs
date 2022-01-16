using System;
using System.Linq;

namespace ErtisAuth.Hub.Extensions
{
    public static class TimeSpanExtensions
    {
        #region Methods

        public static string ToHumanReadableString(this TimeSpan timeSpan, bool showMinutes = true, bool showSeconds = true, bool showMilliseconds = false, bool showTicks = false)
        {
            return ToHumanReadableString(timeSpan as TimeSpan?, showMinutes, showSeconds, showMilliseconds, showTicks);
        }
		
        public static string ToHumanReadableString(this TimeSpan? timeSpan, bool showMinutes = true, bool showSeconds = true, bool showMilliseconds = false, bool showTicks = false)
        {
            if (timeSpan != null)
            {
                var days = timeSpan.Value.Days;
                var daysString = days > 0 ? $"{days} days" : string.Empty;
                var hours = timeSpan.Value.Hours;
                var hoursString = hours > 0 ? $"{hours} hours" : string.Empty;

                string minutesString = null;
                if (showMinutes)
                {
                    var minutes = timeSpan.Value.Minutes;
                    minutesString = minutes > 0 ? $"{minutes} minutes" : string.Empty;	
                }

                string secondsString = null;
                if (showSeconds)
                {
                    var seconds = timeSpan.Value.Seconds;
                    secondsString = seconds > 0 ? $"{seconds} seconds" : string.Empty;	
                }

                string millisecondsString = null;
                if (showMilliseconds)
                {
                    var milliseconds = timeSpan.Value.Milliseconds;
                    millisecondsString = milliseconds > 0 ? $"{milliseconds} milliseconds" : string.Empty;	
                }

                string ticksString = null;
                if (showTicks)
                {
                    var ticks = timeSpan.Value.Ticks;
                    ticksString = ticks > 0 ? $"{ticks} ticks" : string.Empty;	
                }

                var arr = new[]
                {
                    daysString,
                    hoursString,
                    minutesString,
                    secondsString,
                    millisecondsString,
                    ticksString
                };

                return string.Join(", ", arr.Where(x => !string.IsNullOrEmpty(x)));
            }

            return null;
        }

        #endregion
    }
}
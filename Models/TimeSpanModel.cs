namespace WebAppsMoodle.Models
{
    public class TimeSpanModel
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }

        public TimeSpan ToTimeSpan() => new TimeSpan(Hours, Minutes, Seconds);
    }
}

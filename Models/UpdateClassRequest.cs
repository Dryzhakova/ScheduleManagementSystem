using System.ComponentModel.DataAnnotations;

namespace WebAppsMoodle.Models
{
    public class UpdateClassRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string RoomNumber { get; set; }
        public bool IsOneTimeClass { get; set; }
        [DataType(DataType.Date)] // Позволяет вводить только дату
        public DateTime? OneTimeClassFullDate { get; set; } = null;
        public TimeSpanModel OneTimeClassStartTime { get; set; }
        public TimeSpanModel OneTimeClassEndTime { get; set; }
        public DayOfWeek? RecurrenceDay { get; set; }
        public TimeSpanModel RecurrenceStartTime { get; set; }
        public TimeSpanModel RecurrenceEndTime { get; set; }
        public bool IsEven { get; set; }
        public bool IsEveryWeek { get; set; }
    }
}

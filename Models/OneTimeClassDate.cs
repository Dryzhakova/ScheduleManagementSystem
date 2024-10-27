using System.Data;

namespace WebAppsMoodle.Models
{
    public class OneTimeClassDate
    {
        public string OneTimeClassDateId { get; set; }
        public string ClassesId { get; set; }
        public DateTime OneTimeClassFullDate { get; set; }
        /* public DateTime OneTimeClassStartTime { get; set; }
         public DateTime OneTimeClassEndTime { get; set; }*/
        // Для хранения времени, установим значение времени в 00:00:00 для времени начала и окончания
        public TimeSpan OneTimeClassStartTime
        {
            get => OneTimeClassStartTime;
            set => OneTimeClassStartTime = value;
        }

        public TimeSpan OneTimeClassEndTime
        {
            get => OneTimeClassEndTime;
            set => OneTimeClassEndTime = value;
        }

      /*  // Полное значение времени начала и окончания, комбинирующее дату и время
        public DateTime OneTimeClassStartTimeAsDateTime => OneTimeClassFullDate.Add(OneTimeClassStartTime);
        public DateTime OneTimeClassEndTimeAsDateTime => OneTimeClassFullDate.Add(OneTimeClassEndTime);*/

        //DateTimeOffset(DateSetDateTime) — это тип для представления даты и времени с учетом часового пояса.
    }
}

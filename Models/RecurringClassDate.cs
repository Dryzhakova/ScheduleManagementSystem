namespace WebAppsMoodle.Models
{
    public class RecurringClassDate
    {
        public string RecurringClassDateId { get; set; } = Guid.NewGuid().ToString();
        public string ClassesId { get; set; }
        public Classes Classes { get; set; }
        public bool IsEveryWeek { get; set; }
        public bool IsEven { get; set; }// четное или нечетное 

/*        public DateTime RecurrenceStartDate { get; set; } // Дата начала повторения
        public DateTime RecurrenceEndDate { get; set; } // Дата окончания повторения*/
        public DayOfWeek RecurrenceDay { get; set; } // День недели для повторения (например, Понедельник, Вторник и т. д.)
        public TimeSpan RecurrenceStartTime { get; set; } // Время начала повторяющегося занятия
        public TimeSpan RecurrenceEndTime { get; set; } // Время окончания повторяющегося занятия

       

    }
}

namespace WebAppsMoodle.Models
{
    public class Classes
    {
        public string ClassesId { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } 
        public Teacher Teacher { get; set; } // Связь с Teacher
        public string RoomId { get; set; }
        public Room Room { get; set; } // Связь с Room
        public bool IsCanceled { get; set; } = false;
        public string ClassesDescriptionId { get; set; }
        public ClassesDescription ClassesDescription { get; set; } // Связь с ClassesDescription

        public ICollection<CanceledRecurringClass> CanceledRecurrClass { get; set; } // Связь с OneTimeClassDate
        public ICollection<OneTimeClassDate> OneTimeClassDates { get; set; } // Связь с OneTimeClassDate
        public ICollection<RecurringClassDate> RecurringClassDates { get; set; } // Связь с RecurringClassDate



    }
}

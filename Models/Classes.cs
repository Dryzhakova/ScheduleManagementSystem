namespace WebAppsMoodle.Models
{
    public class Classes
    {
        public string ClassesId { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } 
        public Teacher Teacher { get; set; } // Связь с таблицей Teacher
        public string RoomId { get; set; }
        public Room Room { get; set; } // Связь с комнатой
        public bool IsCanceled { get; set; }
        public string ClassesDescriptionId { get; set; }
        public ClassesDescription ClassesDescription { get; set; } // Связь с описанием занятия

    }
}

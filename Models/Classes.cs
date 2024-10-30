namespace WebAppsMoodle.Models
{
    public class Classes
    {
        public string ClassesId { get; set; } = Guid.NewGuid().ToString();
        public string TeacherId { get; set; } 
        public Teacher Teacher { get; set; } // Связь с таблицей Teacher
        public string RoomId { get; set; }
        public bool IsCanceled { get; set; } 
        
    }
}

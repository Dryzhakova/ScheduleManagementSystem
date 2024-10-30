namespace WebAppsMoodle.Models
{
    public class Teacher
    {
        public string TeacherId { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
       // public ICollection<Classes> Classes { get; set; } // Связь с таблицей Classes
    }
}

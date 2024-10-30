namespace WebAppsMoodle.Models
{
    public class ClassesDescription
    {
        public string ClassesDescriptionId { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }

      //  public ICollection<Classes> Classes { get; set; } // Связь с сессиями занятий
    }
}

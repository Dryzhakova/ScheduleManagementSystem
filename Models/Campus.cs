namespace WebAppsMoodle.Models
{
    public class Campus
    {
       public string Campusid { get; set; } = Guid.NewGuid().ToString();
       public string CampusName { get; set; }
       
       public ICollection<Classes> Classes { get; set; }

    }
}

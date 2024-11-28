using System.ComponentModel.DataAnnotations;

namespace WebAppsMoodle.Models
{
    public class CanceledRecurringClass
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ClassesId { get; set; } 
        public Classes Class { get; set; }
        [DataType(DataType.Date)]
        public DateTime CanceledDate { get; set; } 
    }
}

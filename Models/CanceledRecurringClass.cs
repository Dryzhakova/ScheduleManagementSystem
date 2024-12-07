using System.ComponentModel.DataAnnotations;

namespace WebAppsMoodle.Models
{
    public class CanceledRecurringClass
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ClassesId { get; set; } 
        public Classes Class { get; set; }
       
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)] // Настраивает формат
        public DateTime? CanceledDate { get; set; } = null;
       // public DateTime CanceledDate { get; set; } 
    }
}

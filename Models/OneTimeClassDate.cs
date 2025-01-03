﻿using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;


namespace WebAppsMoodle.Models
{
    public class OneTimeClassDate
    {
        public string OneTimeClassDateId { get; set; } = Guid.NewGuid().ToString();
        public string ClassesId { get; set; }
        
        [DataType(DataType.Date)] // Позволяет вводить только дату
        public DateTime? OneTimeClassFullDate { get ; set; } = null;

        /* public DateTime OneTimeClassStartTime { get; set; }
         public DateTime OneTimeClassEndTime { get; set; }*/

        // Для хранения времени, установим значение времени в 00:00:00 для времени начала и окончания
        public TimeSpan OneTimeClassStartTime { get; set; }
        public TimeSpan OneTimeClassEndTime { get; set; }

        /*  // Полное значение времени начала и окончания, комбинирующее дату и время
          public DateTime OneTimeClassStartTimeAsDateTime => OneTimeClassFullDate.Add(OneTimeClassStartTime);
          public DateTime OneTimeClassEndTimeAsDateTime => OneTimeClassFullDate.Add(OneTimeClassEndTime);*/

        //DateTimeOffset(DateSetDateTime) — это тип для представления даты и времени с учетом часового пояса.
    }
}

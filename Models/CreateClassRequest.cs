﻿using System.ComponentModel.DataAnnotations;

namespace WebAppsMoodle.Models
{
    public class CreateClassRequest
    {
        public string RoomNumber { get; set; } // Номер комнаты
      //  public bool IsCanceled { get; set; } // Флаг отмены занятия
        //Description
        public string Title { get; set; } // Заголовок занятия
        public string Description { get; set; } // Описание занятия

        //Campus
        public string CampusName { get; set; }

        //OneTimeClass
        public bool IsOneTimeClass { get; set; } // Указывает, является ли занятие одноразовым

        [DataType(DataType.Date)] // Позволяет вводить только дату
        public DateTime? OneTimeClassFullDate { get; set; } = null;// Дата, если занятие одноразовое 
        public TimeSpanModel OneTimeClassStartTime { get; set; } // Время начала, если занятие одноразовое
        public TimeSpanModel OneTimeClassEndTime { get; set; } // Время окончания, если занятие одноразовое

        //Recurrence

        /*       public DateTime RecurrenceStartDate { get; set; } // Дата начала повторения
               public DateTime RecurrenceEndDate { get; set; } // Дата окончания повторения*/
        public bool IsEveryWeek { get; set; }
        public bool IsEven { get; set; }
        public DayOfWeek RecurrenceDay { get; set; } // День недели для повторения
        public TimeSpanModel RecurrenceStartTime { get; set; } // Время начала повторяющегося занятия
        public TimeSpanModel RecurrenceEndTime { get; set; } // Время окончания повторяющегося занятия


    }
}

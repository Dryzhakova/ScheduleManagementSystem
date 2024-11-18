﻿namespace WebAppsMoodle.Models
{
    public class Room
    {
        public string RoomId { get; set; } = Guid.NewGuid().ToString();
        public string RoomNumber { get; set; }

        public ICollection<Classes> Classes { get; set; } // Связь с таблицей Classes

    }
}
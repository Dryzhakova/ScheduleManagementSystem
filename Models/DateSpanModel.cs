namespace WebAppsMoodle.Models
{
    public class DateSpanModel
    {
        public string DateString { get; set; } = ""; // Изначально пустая строка

        public DateTime? ToDate()
        {
            // Преобразуем строку в DateTime
            if (DateTime.TryParseExact(DateString, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime date))
            {
                return date;
            }
            return null; // Если формат не подходит, вернем null
        }
    }
}

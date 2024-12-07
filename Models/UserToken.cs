namespace WebAppsMoodle.Models
{
    public class UserToken
    {
        public string TokenId { get; set; } = Guid.NewGuid().ToString();
        public string Token { get; set; }
        public string TeacherID { get; set; }
        public Teacher teacher { get; set; }
        public DateTime Expiration { get; set; }
    }
}

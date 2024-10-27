using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppsMoodle.Models;

namespace WebAppsMoodle.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase

    { 

        private readonly ILogger<MainController> _logger;
        private readonly DataContext _context;

        public MainController(ILogger<MainController> logger, DataContext context)
        {
            _logger = logger;
            _context  = context;
        }


        // Mock database for storing users
        private static readonly List<Teacher> _teacher = new List<Teacher>();
        private static readonly List<Room> _rooms = new List<Room>();
        private static readonly List<Classes> _classes = new List<Classes>();
        private static readonly List <ClassesDescription> _classesDescription = new List<ClassesDescription>();
        public static readonly List <OneTimeClassDate> _oneTimeClasses = new List<OneTimeClassDate>();
        public static readonly List <RecurringClassDate> _recurringClasses = new List<RecurringClassDate>();
        // Register endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TeacherRegisterModel model)
        {
            // Check if username already exists
            if (await _context.Teachers.AnyAsync(t => t.Username == model.Username))
            {
                return BadRequest("Username already exists");
            }

            // We should hash the password before storing it
            var newTeacher = new Teacher
            {
                TeacherId = Guid.NewGuid().ToString(),
                Username = model.Username,
                Password = model.Password,
                Title = ""
            };

            _context.Teachers.Add(newTeacher);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        // Login endpoint
        [HttpPost("login")]
        public IActionResult Login([FromBody] TeacherLoginModel model)
        {
            // Check if user exists
            var teacher = _context.Teachers.SingleOrDefault(t => t.Username == model.Username);

            if (teacher == null)
            {
                return BadRequest("Invalid login");
            }
            
            // Сохраняем TeacherId в сессию
            HttpContext.Session.SetString("TeacherId",teacher.TeacherId);

            return Ok(new { Message = "Login successful", TeacherId = teacher.TeacherId });



            /* // Create JWT token
             var token = GenerateJwtToken(user);

             return Ok(new { Token = token });*/

        }

        [HttpPost("createClass")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest model)
        {
            var teacherId = HttpContext.Session.GetString("TeacherId");

            if (string.IsNullOrEmpty(teacherId))
            {
                return BadRequest("Teacher ID is missing.");
            }

            var existingRoom = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);
            Room newRoom;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! можно только одну такую комнату єксепшин если время в такой комнате уже занято с которой его хотят занять
            // Если комната не существует, создаем новую
            if (existingRoom == null)
            {
                newRoom = new Room
                {
                    RoomId = Guid.NewGuid().ToString(), // Генерируем новый ID для комнаты
                    RoomNumber = model.RoomNumber
                };

                await _context.Rooms.AddAsync(newRoom);
            }
            else
            {
                newRoom = existingRoom;
            }


            // Создаем описание занятия
            var classesDescription = new ClassesDescription
            {
                ClassesDescriptionId = Guid.NewGuid().ToString(), // Генерируем новый ID для описания
                Title = model.Title, // Заголовок занятия
                Description = model.Description // Описание занятия
            };

            await _context.ClassesDescription.AddAsync(classesDescription); // Добавляем описание занятия в контекст

            if (model.IsOneTimeClass)
            {

                // Создаем новое занятие
                var newClass = new Classes
                {
                    ClassesId = Guid.NewGuid().ToString(), // Генерируем новый ID для занятия
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    IsCanceled = model.IsCanceled
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

                // Создаем запись для одноразового занятия
                var oneTimeClassDate = new OneTimeClassDate
                {
                    OneTimeClassDateId = Guid.NewGuid().ToString(),
                    ClassesId = newClass.ClassesId,
                    OneTimeClassFullDate = model.OneTimeClassFullDate,
                    OneTimeClassStartTime = model.OneTimeClassStartTime.ToTimeSpan(),
                    OneTimeClassEndTime = model.OneTimeClassEndTime.ToTimeSpan()
                };

                await _context.OneTimeClasses.AddAsync(oneTimeClassDate);
                await _context.SaveChangesAsync();
            }
            else
            {
                var newClass = new Classes
                {
                    ClassesId = Guid.NewGuid().ToString(),
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    IsCanceled = model.IsCanceled
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync();

                // Запись для повторяющегося занятия
                var recurringClassDate = new RecurringClassDate
                {
                    RecurringClassDateId = Guid.NewGuid().ToString(),
                    ClassesId = newClass.ClassesId,
                    IsEven = model.IsEven,
                    IsEveryWeek = model.IsEveryWeek,
                    RecurrenceDay = model.RecurrenceDay,
                    RecurrenceStartTime = model.RecurrenceStartTime.ToTimeSpan(),
                    RecurrenceEndTime = model.RecurrenceEndTime.ToTimeSpan()
                };

                await _context.RecurringClasses.AddAsync(recurringClassDate);
                await _context.SaveChangesAsync();
            }
           /* else
            {
                // Создаем обычное занятие
                var newClass = new Classes
                {
                    ClassesId = Guid.NewGuid().ToString(), // Генерируем новый ID для занятия
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    IsCanceled = model.IsCanceled
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

            }*/
            return Ok(new { Message = "Class and Room created successfully." });
        }

        // Endpoint to get all classes for a specific teacher
        [HttpGet("GetAllClassesTeacher")]
        public async Task<IActionResult> GetClassesForTeacher()
        {
            // Получаем TeacherId из сессии
            var teacherId = HttpContext.Session.GetString("TeacherId");

            if (string.IsNullOrEmpty(teacherId))
            {
                return BadRequest("Teacher ID is missing.");
            }

            //!!!!!!!!!!!!!!! сделать вывод названия, описания и кабинет, IsCanceled or not
            // Получаем все занятия для данного преподавателя
            var classes = await _context.Classes
               /* .Include(c => c.Room) // Подключаем информацию о комнате*/
               /* .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия*/
                .Where(c => c.TeacherId == teacherId)
                .ToListAsync();

            if (classes.Count == 0)
            {
                return NotFound("No classes found for this teacher.");
            }

            return Ok(classes);
        }

        // Endpoint to get all classes for a specific room
        [HttpGet("classes/room/{roomId}")]
        public async Task<IActionResult> GetClassesForRoom(string roomNumber)
        {
            var roomId = await GetRoomIdByRoomNumberAsync(roomNumber);
            // Сначала проверяем, существует ли комната с данным номером
            var room = await _context.Rooms
                .AsNoTracking()  // Убираем отслеживание для оптимизации запроса
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);
            Console.WriteLine(room);

            // Если комната не найдена, возвращаем сообщение об ошибке
            if (room == null)
            {
                return BadRequest("Room does not exist.");
            }


            // Получаем все занятия, привязанные к RoomId этой комнаты
            var classes = await _context.Classes
                .Where(c => c.RoomId == roomId) // Используем RoomId, найденный в предыдущем запросе
                //.Include(c => c.ClassesDescription)  // Присоединяем информацию о описании занятия, если требуется
                .ToListAsync();

            // Проверяем, есть ли занятия для данной комнаты
            if (classes.Count == 0)
            {
                return NotFound("No classes found for this room.");
            }

            // Возвращаем список занятий
            return Ok(classes);

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! нужно выводить по айди а не по комнате сделать проверку и вывод
           /* var classes = await _context.Classes
               .Where(c => c.RoomId == room.RoomId)
               //.Include(c => c.ClassesDescription) // Подключаем описание занятия
               //.Include(c => c.Teacher) // Подключаем информацию о преподавателе, если нужно
               .ToListAsync();
     
    

            if (classes.Count == 0)
            {
                return NotFound("No classes found for this room.");
            }

            return Ok(classes);*/
        }
        private async Task<string> GetRoomIdByRoomNumberAsync(string roomNumber)
        {
            // Проверяем, существует ли комната с данным номером
            var room = await _context.Rooms
                .AsNoTracking() // Убираем отслеживание для оптимизации запроса
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

            // Если комната не найдена, возвращаем null
            return room?.RoomId; // Возвращаем RoomId или null, если комната не найдена
        }



        // Endpoint to get RoomId by room number
        [HttpGet("roomId/{roomNumber}")]
        public async Task<IActionResult> GetRoomIdByRoomNumber(string roomNumber)
        {
            // Проверяем, существует ли комната с данным номером
            var room = await _context.Rooms
                .AsNoTracking() // Убираем отслеживание для оптимизации запроса
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

            // Если комната не найдена, возвращаем сообщение об ошибке
            if (room == null)
            {
                return NotFound("Room does not exist.");
            }

            // Возвращаем RoomId
            return Ok(new { RoomId = room.RoomId });
        }


        // Helper method to generate JWT token
        private string GenerateJwtToken(Teacher user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("your-secret-key-with-at-least-128-bits"); // Ensure your key has at least 128 bits

            // We may also consider using stronger key generation methods
            // var key = new byte[32]; // 256 bits
            // using (var generator = RandomNumberGenerator.Create())
            // {
            //     generator.GetBytes(key);
            // }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.Username)

        }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // Models for registration and login
/*    public class TeacherRegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class TeacherLoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }*/

    // Mock user model (in a real-world scenario, use a proper user model with data annotations)
/*    public class Teacher
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
    }*/
}




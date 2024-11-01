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
                Username = model.Username,
                Password = model.Password,
                Title = model.Title
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

            // ��������� TeacherId � ������
            HttpContext.Session.SetString("TeacherId", teacher.TeacherId);

            return Ok(new { Message = "Login successful", TeacherId = teacher.TeacherId });



            /* // Create JWT token
             var token = GenerateJwtToken(user);

             return Ok(new { Token = token });*/

        }


        [HttpPost("createClass")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest model)
        {
            var teacherId = HttpContext.Session.GetString("TeacherId");

            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var existingRoom = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);
            //if (existingRoom == null) { return BadRequest("Room does not exist."); } �������� ���������� �� ��� � ����� ���� � ����� ������� ���� ��� ������� 

            Room newRoom;
            Classes newClass;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ����� ������ ���� ����� ������� �������� ���� ����� � ����� ������� ��� ������ � ������� ��� ����� ������
            // ���� ������� �� ����������, ������� �����
            if (existingRoom == null)
            {
                newRoom = new Room
                {  
                    RoomNumber = model.RoomNumber
                };

                await _context.Rooms.AddAsync(newRoom);
            }
            else
            {
                newRoom = existingRoom;
            }


            // ������� �������� �������
            var classesDescription = new ClassesDescription
            {
                Title = model.Title, // ��������� �������
                Description = model.Description // �������� �������
            };

            await _context.ClassesDescription.AddAsync(classesDescription); // ��������� �������� ������� � ��������
            await _context.SaveChangesAsync(); // ��������� ��������� � ���� ������

            if (model.IsOneTimeClass)
            {

                // ������� ����� �������
                newClass = new Classes
                {
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId,
                    IsCanceled = model.IsCanceled
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); // ��������� ��������� � ���� ������

                // ������� ������ ��� ������������ �������
                var oneTimeClassDate = new OneTimeClassDate
                {
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
                newClass = new Classes
                {  
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId,
                    IsCanceled = model.IsCanceled
                };

                await _context.Classes.AddAsync(newClass);
               

                // ������ ��� �������������� �������
                var recurringClassDate = new RecurringClassDate
                {  
                    ClassesId = newClass.ClassesId,
                    IsEven = model.IsEven,
                    IsEveryWeek = model.IsEveryWeek,
                    RecurrenceDay = model.RecurrenceDay,
                    RecurrenceStartTime = model.RecurrenceStartTime.ToTimeSpan(),
                    RecurrenceEndTime = model.RecurrenceEndTime.ToTimeSpan()
                };

                await _context.RecurringClasses.AddAsync(recurringClassDate);
              
            }
            await _context.SaveChangesAsync();
            /*else
        {
            // ������� ������� �������
            var newClass = new Classes
            {
                ClassesId = Guid.NewGuid().ToString(), // ���������� ����� ID ��� �������
                TeacherId = teacherId,
                RoomId = newRoom.RoomId,
                IsCanceled = model.IsCanceled
            };

            await _context.Classes.AddAsync(newClass);
            await _context.SaveChangesAsync(); // ��������� ��������� � ���� ������

        }*/
            return Ok(new { Message = "Class and Room created successfully.", ClassesID = newClass.ClassesId });
        }
        // Endpoint to get all classes for a specific teacher
        [HttpGet("teacher/all")]
        public async Task<IActionResult> GetAllTeacherData()
        {
            // �������� ��� �������� � ������ RoomId � RoomNumber
            var teacher = await _context.Teachers
               .Select(r => new
               {
                   TeacherId = r.TeacherId,
                   TeacherName = r.Username,
                   TeacherTitle =  r.Title
               })
               .ToListAsync();

            return Ok(teacher);

          /*  // �������� ���� ��������
            var teachers = await _context.Teachers
                .Select(t => new
                {
                    TeacherId = t.TeacherId,
                    TeacherName = t.Username,
                    ClassTitle = (string)null // ���������� ��� ������
                })
                .ToListAsync();

            // �������� ������ � ��������� � ��������������
            var teacherClasses = await _context.Classes
                .Include(c => c.ClassesDescription)
                .Include(c => c.Teacher)
                .Select(c => new
                {
                    TeacherId = c.Teacher.TeacherId,
                    TeacherName = c.Teacher.Username,
                    ClassTitle = c.ClassesDescription.Title
                })
                .ToListAsync();

            // ��������� ���������� � ������� � ��������
            var result = teachers.Select(t =>
            {
                var classes = teacherClasses
                    .Where(tc => tc.TeacherId == t.TeacherId)
                    .Select(tc => tc.ClassTitle)
                    .ToList();

                return new
                {
                    t.TeacherId,
                    t.TeacherName,
                    ClassTitles = classes.Count > 0 ? classes : new List<string> { null }
                };
            });

            return Ok(result);*/
        }
        // Endpoint to get all classes for a specific teacher
        [HttpGet("teacher/room/all")]
        public async Task<IActionResult> GetClassesForTeacher()
        {
            // �������� TeacherId �� ������
            var teacherId = HttpContext.Session.GetString("TeacherId");

            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var classes = await _context.Classes
              .Include(c => c.Room) // ���������� ���������� � �������
              .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������*/
              .Where(c => c.TeacherId == teacherId)
              .Select(c => new
              {
                  
                  RoomNumber = c.Room.RoomNumber,
                  TeacherName = c.Teacher.Username,
                  ClassTitle = c.ClassesDescription.Title,
                  IsCanceled = c.IsCanceled
              })
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this teacher.");

            //!!!!!!!!!!!!!!! ������� ����� ��������, �������� � �������, IsCanceled or not
            // �������� ��� ������� ��� ������� �������������

            /* var classes = await _context.Classes
                 *//*.Include(c => c.Room)               // ���������� ���������� � �������
                 .Include(c => c.Teacher)            // ���������� ���������� � �������������
                 .Include(c => c.ClassesDescriptions) // ���������� ���������� � �������*//*
                 .Where(c => c.TeacherId == teacherId)
                 .Select(c => new
                 {
                    *//* RoomNumber = c.Room.RoomNumber,
                     TeacherName = c.Teacher.Title,
                     ClassTitle = c.ClassesDescriptions.Select(d => d.Title).FirstOrDefault(),*//*
                     IsCanceled = c.IsCanceled
                 })
                 .ToListAsync();*/

            /*  var classes = await _context.Classes
                  *//*.Include(c => c.Room) // ���������� ���������� � �������
                  .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������*//*
                  .Where(c => c.TeacherId == teacherId)
                  .ToListAsync();*/



            /*            var recurringClasses = await _context.RecurringClasses
                            .Where(rc => classes.Select(c => c.ClassesId).Contains(rc.ClassesId))
                            .ToListAsync();

                        var oneTimeClasses = await _context.OneTimeClasses
                            .Where(oc => classes.Select(c => c.ClassesId).Contains(oc.ClassesId))
                            .ToListAsync();*/

            return Ok(new { Classes = classes/*, RecurringClasses = recurringClasses, OneTimeClasses = oneTimeClasses */});
        }
       
        [HttpGet("room/all")]
        public async Task<IActionResult> GetAllRoomsData()
        {
            // �������� ��� �������� � ������ RoomId � RoomNumber
             var rooms = await _context.Rooms
                .Select(r => new
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber
                })
                .ToListAsync();

             return Ok(rooms);

            /* // �������� ��� ��������
    var rooms = await _context.Rooms
        .Select(r => new
        {
            RoomId = r.RoomId,
            RoomNumber = r.RoomNumber,
            ClassTitle = (string)null // ���������� ������ ��������� ��� ��������� ��� �������
        })
        .ToListAsync();

    // �������� �������, ��������� � ����������
    var roomClasses = await _context.Classes
        .Include(c => c.ClassesDescription)
        .Include(c => c.Room)
        .Select(c => new
        {
            RoomId = c.Room.RoomId,
            RoomNumber = c.Room.RoomNumber,
            ClassTitle = c.ClassesDescription.Title
        })
        .ToListAsync();

    // ��������� ���������� � ������� � ���������
    var result = rooms.Select(r => 
    {
        var classes = roomClasses
            .Where(rc => rc.RoomId == r.RoomId)
            .Select(rc => rc.ClassTitle)
            .ToList();

        return new
        {
            r.RoomId,
            r.RoomNumber,
            ClassTitles = classes.Count > 0 ? classes : new List<string> { null }
        };
    });

    return Ok(result);*/

        }

        // Endpoint to get all classes for a specific room
        [HttpGet("classes/room/id")]
        public async Task<IActionResult> GetClassesForRoom(string roomNumber)
        {
            var roomId = await GetRoomIdByRoomNumberAsync(roomNumber);
            // ������� ���������, ���������� �� ������� � ������ �������
            var room = await _context.Rooms
                .AsNoTracking()  // ������� ������������ ��� ����������� �������
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);
            Console.WriteLine(room);

            // ���� ������� �� �������, ���������� ��������� �� ������
            if (room == null)
            {
                return BadRequest("Room does not exist.");
            }


            // �������� ��� �������, ����������� � RoomId ���� �������
            var classes = await _context.Classes
                .Where(c => c.RoomId == roomId) // ���������� RoomId, ��������� � ���������� �������
                 .Include(c => c.ClassesDescription)  // ������������ ���������� � �������� �������, ���� ���������
                 .Select(c => new
                 {
                     //ClassTitle = c.ClassesDescriptions.Select(d => d.Title).FirstOrDefault()
                     RoomNumber = c.Room.RoomNumber,
                     TeacherName = c.Teacher.Username,
                     ClassTitle = c.ClassesDescription.Title,
                     RoomDescription = c.ClassesDescription.Description  
                 })
                .ToListAsync();

            // ���������, ���� �� ������� ��� ������ �������
            if (classes.Count == 0)
            {
                return NotFound("No classes found for this room.");
            }

            // ���������� ������ �������
            return Ok(classes);

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ����� �������� �� ���� � �� �� ������� ������� �������� � �����
           /* var classes = await _context.Classes
               .Where(c => c.RoomId == room.RoomId)
               //.Include(c => c.ClassesDescription) // ���������� �������� �������
               //.Include(c => c.Teacher) // ���������� ���������� � �������������, ���� �����
               .ToListAsync();
     
    

            if (classes.Count == 0)
            {
                return NotFound("No classes found for this room.");
            }

            return Ok(classes);*/
        }
        private async Task<string> GetRoomIdByRoomNumberAsync(string roomNumber)
        {
            // ���������, ���������� �� ������� � ������ �������
            var room = await _context.Rooms
                .AsNoTracking() // ������� ������������ ��� ����������� �������
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

            // ���� ������� �� �������, ���������� null
            return room?.RoomId; // ���������� RoomId ��� null, ���� ������� �� �������
        }

        // Endpoint to get all classes for a specific day of the week
        [HttpGet("classes/day/dayOfWeek")]
        public async Task<IActionResult> GetClassesForDayOfWeek(int dayOfWeek)
        {
            // ���������, ��� ��������� �������� ��� ������ ��������� � �������� �� 0 �� 6
            if (dayOfWeek < 0 || dayOfWeek > 6) // ��� ��� DayOfWeek ���������� � 0 (�����������) �� 6 (�������)
            {
                return BadRequest("Invalid day of the week. Please provide a number between 0 (Sunday) and 6 (Saturday).");
            }

            // �������� `int` � `DayOfWeek`
            DayOfWeek selectedDay = (DayOfWeek)dayOfWeek;

            // �������� ��� ������� ��� ���������� ��� ������
            var classes = await _context.RecurringClasses
                .Where(c => c.RecurrenceDay == selectedDay) // ���������� � ����� `DayOfWeek`
                .ToListAsync();


            // ���������, ���� �� ������� ��� ������� ��� ������
            if (classes.Count == 0)
            {
                return NotFound($"No classes found for the specified day of the week: {dayOfWeek}.");
            }

            // ���������� ������ �������
            return Ok(classes);
        }


        /*  // Endpoint to get RoomId by room number
          [HttpGet("roomId/{roomNumber}")]
          public async Task<IActionResult> GetRoomIdByRoomNumber(string roomNumber)
          {
              // ���������, ���������� �� ������� � ������ �������
              var room = await _context.Rooms
                  .AsNoTracking() // ������� ������������ ��� ����������� �������
                  .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

              // ���� ������� �� �������, ���������� ��������� �� ������
              if (room == null)
              {
                  return NotFound("Room does not exist.");
              }

              // ���������� RoomId
              return Ok(new { RoomId = room.RoomId });
          }*/


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




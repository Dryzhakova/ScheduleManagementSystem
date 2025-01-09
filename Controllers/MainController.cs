using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppsMoodle.Models;
using System.Globalization;
using WebAppsMoodle.Migrations;
using System.Threading.Tasks;



namespace WebAppsMoodle.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase
    { 

        private readonly ILogger<MainController> _logger;
        private readonly DataContext _context;
      
        public MainController(ILogger<MainController> logger, DataContext context, TeacherRepository teacherRepository, RoomRepository roomRepository)
        {
            _logger = logger;
            _context  = context;
            _teacherRepository = teacherRepository;
            _roomRepository = roomRepository;
        }


        // Mock database for storing users
        private readonly TeacherRepository _teacherRepository;
        private readonly RoomRepository _roomRepository;

        private static readonly List<Room> _rooms = new List<Room>();
        public static readonly List<Campus> _campus = new List<Campus>();
        private static readonly List<Teacher> _teacher = new List<Teacher>();
        private static readonly List<Classes> _classes = new List<Classes>();
        public static readonly List<Models.CanceledRecurringClass> _canceledRecurringClasses = new List<Models.CanceledRecurringClass>();
        public static readonly List <OneTimeClassDate> _oneTimeClasses = new List<OneTimeClassDate>();
        public static readonly List <RecurringClassDate> _recurringClasses = new List<RecurringClassDate>();
        private static readonly List <ClassesDescription> _classesDescription = new List<ClassesDescription>();
       
        
        // Register endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] TeacherRegisterModel model)
        {
            if (await _context.Teachers.AnyAsync(t => t.Username == model.Username))
                return BadRequest(new { Message = "Username already exists" });

            if (!IsNameCorrect(model.Username))
                return BadRequest(new { Message = "Username must be at least 3 characters long, contain a first uppercase letter" });

            if (!IsPasswordStrong(model.Password))
                return BadRequest(new
                {
                    Message = "Password must be at least 8 characters long, contain one uppercase letter, one lowercase letter, one number, and one special character."
                });

            var passwordHasher = new PasswordHasher<Teacher>();
            // We should hash the password before storing it
            var newTeacher = new Teacher
            {
                Username = model.Username,
                Password = passwordHasher.HashPassword(null, model.Password),
                Title = model.Title
            };

            _context.Teachers.Add(newTeacher);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }

        private bool IsNameCorrect(string username)
        {
            // Пример проверки надежности
            var namePolicy = new System.Text.RegularExpressions.Regex(
                   @"^[A-Z][a-z]{2,}$");

            return namePolicy.IsMatch(username);
        }
        private bool IsPasswordStrong(string password)
        {
            // Пример проверки надежности
            var passwordPolicy = new System.Text.RegularExpressions.Regex(
                 @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W)[A-Za-z\d\W]{8,}$");

            // Условия: минимум 8 символов, одна строчная буква, одна заглавная, одна цифра, один спецсимвол
            return passwordPolicy.IsMatch(password);
        }

        // Login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] TeacherLoginModel model)
        {
            // Check if user exists
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Username == model.Username);
            if (teacher == null) return BadRequest("Invalid login");

            var passwordHasher = new PasswordHasher<Teacher>();
            if (passwordHasher.VerifyHashedPassword(teacher, teacher.Password, model.Password) != PasswordVerificationResult.Success)
                return BadRequest(new { Message = "Invalid Password" });

            var existedToken = await _context.UserTokens.SingleOrDefaultAsync(ut => ut.TeacherID == teacher.TeacherId);

            if (existedToken != null && existedToken.Expiration <= DateTime.UtcNow)
            {
                _context.UserTokens.Remove(existedToken);
                await _context.SaveChangesAsync();
                existedToken = null; 
            }

            if (existedToken == null)
            {
                var token = GenerateJwtToken(teacher);

                var userToken = new UserToken
                {
                    Token = token,
                    TeacherID = teacher.TeacherId,
                    Expiration = DateTime.UtcNow.AddDays(7)
                };

                _context.UserTokens.Add(userToken);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Login successful",
                    TeacherId = teacher.TeacherId,
                    Token = token,
                    Expiration = userToken.Expiration.ToShortDateString()
                });
            }

            return Ok(new
            {
                Message = "Login successful",
                TeacherId = teacher.TeacherId,
                Token = existedToken.Token,
                Expiration = existedToken.Expiration.ToShortDateString()
            });
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken(string token)
        {
            var userToken = await _context.UserTokens
                .Include(ut => ut.teacher)
                .SingleOrDefaultAsync(ut => ut.Token == token && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token" });

            return Ok(new
            {
                Message = "Token is valid",
                Username = userToken.teacher.Username,
                Expiration = userToken.Expiration.ToShortDateString()
            });
        }

        private int ConvertDayOfWeekToDbFormat(DayOfWeek dayOfWeek)
        {
            // Преобразуем System.DayOfWeek в формат базы данных
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => 0,
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Invalid day of week.")
            };
        }

        [HttpPost("createClass")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest model, string teacherId, string teacherToken)
        {


            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });

            Room newRoom;
            Classes newClass;

            var existingRoom = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);
            if (existingRoom == null)
            {
                //only digits
                if (!int.TryParse(model.RoomNumber, out _))throw new ArgumentException("Room number must contain only digits.");
                newRoom = new Room {   RoomNumber = model.RoomNumber };
                await _context.Rooms.AddAsync(newRoom);
            }
            else
            {
                newRoom = existingRoom;
            }

            var newCampus = new Campus
            {
                CampusName = model.CampusName
            };

            await _context.Campuses.AddAsync(newCampus);
            await _context.SaveChangesAsync();

           
            var classesDescription = new ClassesDescription
            {
                Title = model.Title, 
                Description = model.Description 
            };

            await _context.ClassesDescription.AddAsync(classesDescription); 
            await _context.SaveChangesAsync(); 

            if (model.IsOneTimeClass && model.OneTimeClassFullDate.HasValue)
            {
        
                newClass = new Classes
                {
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    CampusId = newCampus.Campusid,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); 

                var oneTimeClassDate = new OneTimeClassDate
                {
                    ClassesId = newClass.ClassesId,
                    OneTimeClassFullDate = model.OneTimeClassFullDate.Value,
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
                    CampusId = newCampus.Campusid,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId
                };

                await _context.Classes.AddAsync(newClass);
               

                var recurringClassDate = new RecurringClassDate
                {  
                    ClassesId = newClass.ClassesId,
                    IsEven = model.IsEven,
                    IsEveryWeek = model.IsEveryWeek,
                    RecurrenceDay = (DayOfWeek)ConvertDayOfWeekToDbFormat(model.RecurrenceDay),
                    RecurrenceStartTime = model.RecurrenceStartTime.ToTimeSpan(),
                    RecurrenceEndTime = model.RecurrenceEndTime.ToTimeSpan()
                };
               

                await _context.RecurringClasses.AddAsync(recurringClassDate);
               _logger.LogInformation($"RecurrenceDay saved: {model.RecurrenceDay}, RecurrenceDay retrieved: {recurringClassDate.RecurrenceDay}");
            }
            await _context.SaveChangesAsync();
           
            return Ok(new { Message = "Class and Room created successfully.", ClassesID = newClass.ClassesId });
        }

        [HttpPost("createRoom")]
        public async Task<IActionResult> CreateRoom(string RoomNumber, string CampusName)
        {


            if (string.IsNullOrEmpty(RoomNumber)) return BadRequest("room Number is missing.");

            var existingRoom = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomNumber == RoomNumber);
            if (existingRoom == null)
            {
                if (!int.TryParse(RoomNumber, out _)) throw new ArgumentException("Room number must contain only digits.");
                existingRoom = new Room { RoomNumber = RoomNumber };
                await _context.Rooms.AddAsync(existingRoom);
            }


            Room newRoom;

            if (existingRoom == null)
            {
                //only digits
                if (!int.TryParse(RoomNumber, out _)) throw new ArgumentException("Room number must contain only digits.");
                newRoom = new Room { RoomNumber = RoomNumber };
                await _context.Rooms.AddAsync(newRoom);
            }
            else
            {
                newRoom = existingRoom;
            }

            var newCampus = new Campus
            {
                CampusName = CampusName
            };

            await _context.Campuses.AddAsync(newCampus);
            await _context.SaveChangesAsync();


            return Ok(new { Message = "Campus name and Room created successfully.", CampusName = CampusName, RoomNumber = RoomNumber });
        }

        [HttpPost("cancelOrRestoreClassOneTime/{classId}")]
        public async Task<IActionResult> RestoreCancelOneTimeClass(string classId, string teacherId, string teacherToken)
        {

            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");


            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);
            if (userToken?.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });
            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            var classData = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .FirstOrDefaultAsync(c=>c.ClassesId == classId);

            if (classData == null) return NotFound(new { Message = "Class not found." });
            if (classData.TeacherId != teacherId) return BadRequest(new { Message = "You do not have permission to cancel this class." });


            if (classData.IsCanceled)
            {
                var classRestore = await _context.Classes
               .Include(c => c.OneTimeClassDates)
               .FirstOrDefaultAsync(c => c.ClassesId == classId);

                if (classRestore == null) return NotFound(new { Message = "Class not found." });

                classRestore.IsCanceled = false;
            }
            else
            {
                var classCancel = await _context.Classes
               .Include(c => c.OneTimeClassDates)
               .FirstOrDefaultAsync(c => c.ClassesId == classId);

                if (classCancel == null) return NotFound(new { Message = "Class not found." });

                classCancel.IsCanceled = true;
            }
            await _context.SaveChangesAsync();

            return Ok(new { Message = "successfully." });
        }
        private bool IsEvenWeek(DateTime date)
        {
            var firstDayOfYear = new DateTime(date.Year, 1, 1);
            var weekNumber = (date - firstDayOfYear).Days / 7 + 1;
            return weekNumber % 2 == 0;
        }

       [HttpPost("cancelRecurringClass/{classId}")]
       
        public async Task<IActionResult> CancelRecurringClass(string classId, string teacherId, string teacherToken, [FromBody] DateTime cancellationDate)
        {
            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken?.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });
            // cancellationDate.ToString(); 

            var reccuringClass = await _context.RecurringClasses
                .Include(r => r.Classes)
                .FirstOrDefaultAsync(r => r.ClassesId == classId);

            if (reccuringClass == null) return NotFound(new { Message = "Recurring class not found." });

            if (reccuringClass.Classes.TeacherId != teacherId)  return BadRequest(new { Message = "You do not have permission to cancel this class." }); // return BadRequest(new { Message = $"Expected: {teacherId}, Actual: {reccuringClass.Classes.TeacherId}" });

            if (reccuringClass.RecurrenceDay != cancellationDate.DayOfWeek) return BadRequest(new { Message = "The provided date does not match the recurring schedule." });

            bool isEveryWeek = IsEvenWeek(cancellationDate);
            if (!reccuringClass.IsEveryWeek && reccuringClass.IsEven != isEveryWeek) return BadRequest(new { Message = "The provided date does not match the recurring schedule's week pattern." });

            var exitingCancellation = await _context.CanceledRecurringClasses
                .FirstOrDefaultAsync(c => c.ClassesId == classId && c.CanceledDate.Value.Date == cancellationDate.Date);
            if (exitingCancellation != null) return Conflict(new { Message = "The class is already canceled for the specified date." });

            var canceledClass = new Models.CanceledRecurringClass
            {
                ClassesId = classId,
                CanceledDate = cancellationDate.Date,
            };

            await _context.CanceledRecurringClasses.AddAsync(canceledClass);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Recurring class canceled successfully." });
        }
      
        [HttpPost("restoreRecurringClass/{classId}")]
        public async Task<IActionResult> RestoreRecurringClass(string classId, string teacherId, string teacherToken, [FromBody] DateTime restorationDate)
        {
            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");


            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);
            if (userToken?.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });
            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });


            var canceledDate = await _context.CanceledRecurringClasses
                .Include(r => r.Class)
                .FirstOrDefaultAsync(c => c.ClassesId == classId && c.CanceledDate.Value.Date == restorationDate.Date);
           
            if (canceledDate.Class.TeacherId != teacherId) return BadRequest(new { Message = "You do not have permission to cancel this class." });


            if (canceledDate == null) return NotFound(new { message = "No canceled class found for the specified date." });

            _context.CanceledRecurringClasses.Remove(canceledDate); 
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Recurring class restored" });
        }

       [HttpPut("updateClass/{classId}")]
        public async Task<IActionResult> UpdateClass(string classId, string teacherId, string teacherToken, [FromBody] UpdateClassRequest model)
        {
            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });

         
            var classToUpdate = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToUpdate == null)
                return NotFound(new { Message = "Class not found." });

           
            if (!string.IsNullOrEmpty(model.Title))
                classToUpdate.ClassesDescription.Title = model.Title;
            if (!string.IsNullOrEmpty(model.Description))
                classToUpdate.ClassesDescription.Description = model.Description;

            if (!string.IsNullOrEmpty(model.RoomNumber))
            {
                var newRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);
                if (newRoom == null)
                {
                    newRoom = new Room { RoomNumber = model.RoomNumber };
                    await _context.Rooms.AddAsync(newRoom);
                }
                classToUpdate.RoomId = newRoom.RoomId;
            }

      
            if (model.IsOneTimeClass && model.OneTimeClassFullDate.HasValue)
            {
                var oneTimeClass = classToUpdate.OneTimeClassDates.FirstOrDefault();
                if (oneTimeClass != null)
                {
                    oneTimeClass.OneTimeClassFullDate = model.OneTimeClassFullDate.Value;
                    oneTimeClass.OneTimeClassStartTime = model.OneTimeClassStartTime.ToTimeSpan();
                    oneTimeClass.OneTimeClassEndTime = model.OneTimeClassEndTime.ToTimeSpan();
                }
            }

            if (!model.IsOneTimeClass)
            {
                var recurringClass = classToUpdate.RecurringClassDates.FirstOrDefault();
                if (recurringClass != null)
                {
                    recurringClass.RecurrenceDay = (DayOfWeek)model.RecurrenceDay;
                    recurringClass.RecurrenceStartTime = model.RecurrenceStartTime.ToTimeSpan();
                    recurringClass.RecurrenceEndTime = model.RecurrenceEndTime.ToTimeSpan();
                    recurringClass.IsEven = model.IsEven;
                    recurringClass.IsEveryWeek = model.IsEveryWeek;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Class updated successfully." });
        }

        [HttpDelete("deleteClass/{classId}")]
        public async Task<IActionResult> DeleteClass(string teacherId, string teacherToken, string classId)
        {
            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });

            var classToDelete = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToDelete == null)
                return NotFound(new { Message = "Class not found." });

           
            _context.OneTimeClasses.RemoveRange(classToDelete.OneTimeClassDates);
            _context.RecurringClasses.RemoveRange(classToDelete.RecurringClassDates);
            _context.ClassesDescription.Remove(classToDelete.ClassesDescription);
            _context.Classes.Remove(classToDelete);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Class and all related data deleted successfully." });
        }

        [HttpDelete("deleteAccount/{teacherToken}")]
        public async Task<IActionResult> DeleteAccount(string teacherToken, string teacherId)
        {

          var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });

            var teacherToDelete = await _context.Teachers
                .FirstOrDefaultAsync(t => t.TeacherId == userToken.TeacherID);

            if (teacherToDelete == null) return NotFound(new { Message = "Inccorect ID." });

            _context.Teachers.RemoveRange(teacherToDelete);
            var tokensToDelete = _context.UserTokens.Where(ut => ut.TeacherID == teacherToDelete.TeacherId);
            _context.UserTokens.RemoveRange(tokensToDelete);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Account deleted successfully." });
        }

        /*    [HttpDelete("deleteClass/{classId}")]
            public async Task<IActionResult> DeleteClassOnetimeRecurringAll(string classId, [FromQuery] string? deleteType = null)
            {
                // Проверяем, существует ли класс
                var classToDelete = await _context.Classes
                    .Include(c => c.OneTimeClassDates)
                    .Include(c => c.RecurringClassDates)
                    .Include(c => c.ClassesDescription)
                    .FirstOrDefaultAsync(c => c.ClassesId == classId);

                if (classToDelete == null)
                    return NotFound(new { Message = "Class not found." });

                switch (deleteType?.ToLower())
                {
                    case "onetime":
                        // Удаляем только одноразовые занятия
                        _context.OneTimeClasses.RemoveRange(classToDelete.OneTimeClassDates);
                        break;

                    case "recurring":
                        // Удаляем только повторяющиеся занятия
                        _context.RecurringClasses.RemoveRange(classToDelete.RecurringClassDates);
                        break;

                    case null: // Если deleteType не указан, удаляем всё
                    case "all":
                        // Удаляем связанные записи
                        _context.OneTimeClasses.RemoveRange(classToDelete.OneTimeClassDates);
                        _context.RecurringClasses.RemoveRange(classToDelete.RecurringClassDates);
                        _context.ClassesDescription.Remove(classToDelete.ClassesDescription);
                        _context.Classes.Remove(classToDelete);
                        break;

                    default:
                        return BadRequest(new { Message = "Invalid delete type. Use 'onetime', 'recurring', or 'all'." });
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Deletion completed successfully.", DeleteType = deleteType ?? "all" });
            }*/


        // Endpoint to get all classes for a specific teacher
        //[HttpGet("teacher/room/all")]
        /*   public async Task<IActionResult> GetClassesForTeacher()
           {
               // Получаем TeacherId из сессии
               var teacherId = HttpContext.Session.GetString("TeacherId");

               if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

               var classes = await _context.Classes
                 .Include(c => c.Room) // Подключаем информацию о комнате
                 .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия
                 .Where(c => c.TeacherId == teacherId)
                 .Select(c => new
                 {

                     RoomNumber = c.Room.RoomNumber,
                     TeacherName = c.Teacher.Username,
                     ClassTitle = c.ClassesDescription.Title,
                     IsCanceled = c.IsCanceled
                 })
                 .ToListAsync();

               if (classes.Count == 0) return NotFound("No classes found for this teacher.");*/


        /*        var classes = await _context.Classes
                         .Include(c => c.Room)               // Подключаем информацию о комнате
                         .Include(c => c.Teacher)            // Подключаем информацию о преподавателе
                         .Include(c => c.ClassesDescriptions) // Подключаем информацию о занятии
                         .Where(c => c.TeacherId == teacherId)
                         .Select(c => new
                         {
                             RoomNumber = c.Room.RoomNumber,
                             TeacherName = c.Teacher.Title,
                             ClassTitle = c.ClassesDescriptions.Select(d => d.Title).FirstOrDefault(),
                             IsCanceled = c.IsCanceled
                         })
                         .ToListAsync();

                var classes = await _context.Classes
                    .Include(c => c.Room) // Подключаем информацию о комнате
                    .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия
                    .Where(c => c.TeacherId == teacherId)
                    .ToListAsync();



                              var recurringClasses = await _context.RecurringClasses
                                    .Where(rc => classes.Select(c => c.ClassesId).Contains(rc.ClassesId))
                                    .ToListAsync();

                                var oneTimeClasses = await _context.OneTimeClasses
                                    .Where(oc => classes.Select(c => c.ClassesId).Contains(oc.ClassesId))
                                    .ToListAsync();*/

        /*return Ok(new { Classes = classes, RecurringClasses = recurringClasses, OneTimeClasses = oneTimeClasses});
    } */

        [HttpGet("{teacherId}/room/all")]

        public async Task<IActionResult> GetClassesForTeacher(string teacherId)
        {
            //var teacherId = HttpContext.Session.GetString("TeacherId");
            //"teacherId": " 643b2240-51b9-466c-8752-676e563441b5 "
            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

         
            var checkTeacherId = await _context.Teachers
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.TeacherId == teacherId);

            if (checkTeacherId == null)  return NotFound("Teacher not found.");

            var classes = await _context.Classes
              .Include(c => c.Room) 
              .Include(c => c.Campus)
              .Include(c => c.ClassesDescription) 
              .Include(c => c.OneTimeClassDates)
              .Include(c => c.RecurringClassDates)
              .Include(c => c.CanceledRecurrClass)
              .Where(c => c.TeacherId == teacherId)
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this teacher.");

            var result = classes.Select(c => new
            {
                ClassTitle = c.ClassesDescription.Title,
                ClassId = c.ClassesId,
                CampusId = c.CampusId,
                CampusName = c.Campus.CampusName,
                RoomNumber = c.Room.RoomNumber,
                RoomId = c.Room.RoomId,
                TeacherName = checkTeacherId.Username,
                TeacherTitle = checkTeacherId.Title,
                TeacherId = checkTeacherId.TeacherId,
                IsCanceled = c.IsCanceled,

                //true false
              RecurringClasses = c.RecurringClassDates.Select(r => new
                {
                    RecurrenceDay = r.RecurrenceDay,
                    RecurrenceStartTime = r.RecurrenceStartTime,
                    RecurrenceEndTime = r.RecurrenceEndTime,
                    CanceledDates = c.CanceledRecurrClass
                        .Where(cr => cr.ClassesId == c.ClassesId)
                        .Select(cr => cr.CanceledDate.Value.ToShortDateString()) 
                        .ToList(),
                    IsEven = r.IsEven,
                    IsEveryWeek = r.IsEveryWeek
                }).ToList(),

                 OneTimeClasses = c.OneTimeClassDates.Select(o => new
                {
                    OneTimeClassFullDate = o.OneTimeClassFullDate.Value.ToShortDateString(),
                    OneTimeClassStartTime = o.OneTimeClassStartTime,
                    OneTimeClassEndTime = o.OneTimeClassEndTime,

                }).ToList(),
            }).ToList();

            return Ok(result);
        }

            [HttpGet("room/all")]
        public async Task<IActionResult> GetAllRoomsData()
        {
            var rooms = await _roomRepository.GetAllRoomsAsync();

            if (rooms == null) return BadRequest("Room not found.");

            return Ok(rooms);

        }

        [HttpGet("teacher/all")]
        public async Task<IActionResult> GetAllTeacherData()
        {
            var teachers = await _teacherRepository.GetAllTeachersAsync();

            if (teachers == null) return BadRequest("Teacher not found.");

            return Ok(teachers);
        }

       
        [HttpGet("classes/{roomId}/id")]
        public async Task<IActionResult> GetClassesForRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return BadRequest("Room ID is missing.");

            var room = await _context.Rooms
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.RoomId == roomId);
            Console.WriteLine(room);

            if (room == null) return BadRequest("Room not found.");

            var classes = await _context.Classes
              .Include(c => c.Teacher)
              .Include(c => c.Campus)
              .Include(c => c.ClassesDescription)
              .Include(c => c.OneTimeClassDates)
              .Include(c => c.RecurringClassDates)
              .Include(c => c.CanceledRecurrClass)
              .Where(c => c.RoomId == roomId)
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this room.");


            var result = classes.Select(c => new
            {
                ClassTitle = c.ClassesDescription.Title,
                ClassId = c.ClassesId,
                CampusId = c.CampusId,
                CampusName = c.Campus.CampusName,
                RoomNumber = room.RoomNumber,
                RoomId = room.RoomId,
                TeacherName = c.Teacher.Username,
                TeacherTitle = c.Teacher.Title,
                TeacherId = c.Teacher.TeacherId,
                IsCanceled = c.IsCanceled,


                RecurringClasses = c.RecurringClassDates.Select(r => new
                {
                    RecurrenceDay = r.RecurrenceDay,
                    RecurrenceStartTime = r.RecurrenceStartTime,
                    RecurrenceEndTime = r.RecurrenceEndTime,
                    IsEven = r.IsEven,
                    IsEveryWeek = r.IsEveryWeek,
                    CanceledDates = c.CanceledRecurrClass
                        .Where(cr => cr.ClassesId == c.ClassesId) 
                        .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // Берем дату отмены
                        .ToList()
                }).ToList(),

                OneTimeClasses = c.OneTimeClassDates.Select(o => new
                {
                    OneTimeClassFullDate = o.OneTimeClassFullDate.Value.Date.ToShortDateString(),
                    OneTimeClassStartTime = o.OneTimeClassStartTime,
                    OneTimeClassEndTime = o.OneTimeClassEndTime,

                }).ToList(),
            }).ToList();

            return Ok(result);
        }


        [HttpGet("GetRoomIdByRoomNumber")]
        public async Task<string> GetRoomIdByRoomNumberAsync(string roomNumber)
        {
            // Проверяем, существует ли комната с данным номером
            var room = await _context.Rooms
                .AsNoTracking() // Убираем отслеживание для оптимизации запроса
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

            // Если комната не найдена, возвращаем null
            return room?.RoomId; // Возвращаем RoomId или null, если комната не найдена
        }


        [HttpGet("GetRoomNumberByRoomId")]
        public async Task<IActionResult> GetRoomNumberByRoomIdAsync(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return BadRequest("Room ID is missing.");

            var room = await _context.Rooms
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.RoomId == roomId);
            Console.WriteLine(room);

            if (room == null) return BadRequest("Room not found.");

            var classes = await _context.Classes
              .Include(c => c.Room)
              .Include(c => c.Campus)
              .Where(c => c.RoomId == roomId)
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this room.");

            var result = classes.Select(c => new
            {
                roomId = roomId,
                RoomNumber = room.RoomNumber,
                CampusId = c.CampusId,
                CampusName = c.Campus.CampusName
            }).ToList();

            return Ok(result);
        }

        // Endpoint to get all classes for a specific day of the week
        [HttpGet("classes/day/dayOfWeek")]
        public async Task<IActionResult> GetClassesForDayOfWeek(int dayOfWeek)
        {
          
            if (dayOfWeek < 0 || dayOfWeek > 6) 
            {
                return BadRequest("Invalid day of the week. Please provide a number between 0 (Sunday) and 6 (Saturday).");
            }

            // Приводим int к DayOfWeek
            DayOfWeek selectedDay = (DayOfWeek)dayOfWeek;
            Console.WriteLine($"Selected Day: {selectedDay}");

            var classes = await _context.RecurringClasses
           .Where(c => c.RecurrenceDay == selectedDay)
           .Include(c => c.Classes) 
               .ThenInclude(c => c.Teacher) 
           .Include(c => c.Classes) 
               .ThenInclude(c => c.Room)
           .Include(c => c.Classes)
               .ThenInclude(c => c.Campus)
           .Include(c => c.Classes)
               .ThenInclude(c => c.ClassesDescription) 
           .Include(c => c.Classes)
               .ThenInclude(c => c.CanceledRecurrClass)    
           .ToListAsync();

          
            if (classes.Count == 0)
            {
                return NotFound(new { message = $"No classes found for the specified day of the week: {selectedDay}.", selectedDay });
            }

       
            // Создаем результат с нужными данными
            var result = classes.Select(c => new
            {
                TeacherName = c.Classes.Teacher.Username, 
                TeacherId = c.Classes.Teacher.TeacherId,
                TeacherTitle = c.Classes.Teacher.Title,
                CampusId = c.Classes.CampusId,
                CampusName = c.Classes.Campus.CampusName,
                RoomNumber = c.Classes.Room.RoomNumber, 
                RoomId = c.Classes.Room.RoomId,
                ClassId = c.ClassesId,
                ClassTitle = c.Classes.ClassesDescription.Title, 
                IsCanceled = c.Classes.IsCanceled.ToString(),
                isEveryWeek = c.IsEveryWeek.ToString(),
                IsEven = c.IsEven.ToString(),
                //recurrenceDay = c.RecurrenceDay,
                recurrenceDay = c.RecurrenceDay.ToString(),
                recurrenceStartTime = c.RecurrenceStartTime,
                recurrenceEndTime = c.RecurrenceEndTime,
                CanceledDates = c.Classes.CanceledRecurrClass
                    .Where(cr => cr.ClassesId == c.Classes.ClassesId) 
                    .Select(cr => cr.CanceledDate.Value.ToShortDateString()) 
                    .ToList()

            }).ToList();

            return Ok(result);
        }

        /*     [HttpGet("classes/date/AllClassesByDate")]
             public async Task<IActionResult> GetAllClassesByDate(DateTime date)
             {
                 // Получаем номер дня недели для заданной даты
                 var dayOfWeek = date.DayOfWeek;

                 var today = DateTime.Today;

                 // Загружаем все классы с их данными
                 var classes = await _context.Classes
                     .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия
                     .Include(c => c.Teacher) // Подключаем информацию о преподавателе
                     .Include(c => c.OneTimeClassDates) // Подключаем информацию о одноразовых датах занятий
                     .Include(c => c.RecurringClassDates) // Подключаем информацию о повторяющихся занятиях
                     .ToListAsync();

                 // Фильтрация одноразовых занятий
                 var oneTimeClasses = classes
                     .Where(c => c.OneTimeClassDates.Any(o => o.OneTimeClassFullDate?.Date == date.Date)) // Фильтруем по дате
                     .Select(c => new
                     {
                         ClassId = c.ClassesId,
                         ClassTitle = c.ClassesDescription.Title,
                         TeacherName = c.Teacher.Username,
                         ClassType = "OneTime", // Указываем тип занятия
                         OneTimeClassFullDate = c.OneTimeClassDates
                             .Where(o => o.OneTimeClassFullDate?.Date == date.Date)
                             .Select(o => o.OneTimeClassFullDate?.ToString("yyyy-MM-dd"))
                     });

                 //!!!!!!!!!!!!!!!!!!!!!!! проверка повторяющихся IsEven и IsEveryWeek выводит ли верные даты
                 // Фильтрация повторяющихся занятий
                 var filteredClasses = classes
                     .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek
                         && (r.IsEveryWeek ||
                             (r.IsEven && today.Day % 2 == 0) ||
                             (!r.IsEven && today.Day % 2 != 0)) // Проверка на четность
                         && (today.TimeOfDay >= r.RecurrenceStartTime && today.TimeOfDay <= r.RecurrenceEndTime)) // Проверка по времени
                     )
                     .Select(c => new
                     {
                         ClassId = c.ClassesId,
                         ClassTitle = c.ClassesDescription.Title,
                         TeacherName = c.Teacher.Username,
                         ClassType = "Recurring", // Указываем тип занятия
                         RecurrenceDay = c.RecurringClassDates
                             .Where(r => r.RecurrenceDay == dayOfWeek)
                             .Select(r => r.RecurrenceDay.ToString()) // Для получения имени дня недели
                             .FirstOrDefault()
                     });

                 // Объединяем результаты
                 var allClasses = oneTimeClasses.Cast<object>().Concat(filteredClasses.Cast<object>());

                 if (!allClasses.Any())
                 {
                     return NotFound("No classes found for the specified date.");
                 }

                 return Ok(allClasses);
             }*/

        [HttpGet("classes/date/AllClassesByDate")]
        public async Task<IActionResult> GetAllClassesByDate(DateTime date)
        {
          
            DateTime semesterStartDate = new DateTime(2024, 10, 1);
            DateTime semesterEndDate = new DateTime(2025, 2, 28);

            int daysDifference = (date - semesterStartDate).Days;
            bool isCurrentWeekEven = ((daysDifference / 7) % 2) == 0;

            var dayOfWeek = date.DayOfWeek;

            var classes = await _context.Classes
                .Include(c => c.ClassesDescription)
                .Include(c => c.Teacher)
                .Include(c => c.Campus)
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .ToListAsync();

            var oneTimeClasses = classes
                .Where(c => c.OneTimeClassDates.Any(o => o.OneTimeClassFullDate?.Date == date.Date))
                .Select(c => new
                {
                    ClassId = c.ClassesId,
                    ClassTitle = c.ClassesDescription.Title,
                    TeacherName = c.Teacher.Username,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.CampusName,
                    ClassType = "OneTime",
                    OneTimeClassFullDate = c.OneTimeClassDates
                        .Where(o => o.OneTimeClassFullDate?.Date == date.Date)
                        .Select(o => o.OneTimeClassFullDate?.ToString("yyyy-MM-dd"))
                });

   
            var recurringClasses = classes
                .Where(c => c.RecurringClassDates.Any(r =>
                    r.RecurrenceDay == dayOfWeek
                    && (r.IsEveryWeek || r.IsEven == isCurrentWeekEven) 
                    && date.TimeOfDay >= r.RecurrenceStartTime
                    && date.TimeOfDay <= r.RecurrenceEndTime
                ))
                .Select(c => new
                {
                    ClassId = c.ClassesId,
                    ClassTitle = c.ClassesDescription.Title,
                    TeacherName = c.Teacher.Username,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.CampusName,
                    ClassType = "Recurring",
                    RecurrenceDay = c.RecurringClassDates
                        .Where(r => r.RecurrenceDay == dayOfWeek)
                        .Select(r => r.RecurrenceDay.ToString())
                        .FirstOrDefault()
                });


            var allClasses = oneTimeClasses.Cast<object>().Concat(recurringClasses.Cast<object>());

            if (!allClasses.Any())
            {
                return NotFound("No classes found for the specified date.");
            }

            return Ok(allClasses);
        }

        //Endpoint to get all classes for a specific date
        [HttpGet("classes/date/OneTimeClass")]
        public async Task<IActionResult> GetOneTimeClassByDate(DateTime date)
        {

            var classes = await _context.Classes
                .Include(c => c.ClassesDescription) 
                .Include(c => c.Teacher) 
                .Include(c => c.Room)
                .Include(c => c.Campus)
                .Include(c => c.OneTimeClassDates) 
                .Where(c => c.OneTimeClassDates.Any(o => o.OneTimeClassFullDate.HasValue && o.OneTimeClassFullDate.Value.Date == date.Date)) 
                .Select(c => new
                {
                    ClassId = c.ClassesId,
                    ClassTitle = c.ClassesDescription.Title, 
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher.Username,
                    TeacherTitle = c.Teacher.Title,
                    RoomId = c.RoomId,
                    RoomNumber = c.Room.RoomNumber,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.CampusName,
                    IsCanceled = c.IsCanceled.ToString(),
                    OneTimeClassStartTime = c.OneTimeClassDates
                    .Select(o => o.OneTimeClassStartTime.ToString()),
                    OneTimeClassEndTime = c.OneTimeClassDates
                    .Select(o => o.OneTimeClassEndTime.ToString()),
                    OneTimeClassFullDate = c.OneTimeClassDates
                        .Where(o => o.OneTimeClassFullDate.HasValue && o.OneTimeClassFullDate.Value.Date == date.Date)
                        .Select(o => o.OneTimeClassFullDate.Value.ToString("yyyy-MM-dd"))
                })
                .ToListAsync();

                    if (!classes.Any())
                    {
                        return NotFound("No classes found for the specified date.");
                    }

            return Ok(classes);
        }
        [HttpGet("classes/date/RecurringClassDates")]

        /* public async Task<IActionResult> GetRecurringClassBydate(DateTime date)
         {
             // Получаем номер дня недели для заданной даты
             var dayOfWeek = date.DayOfWeek;
             var today = DateTime.Today;

             // Загружаем все классы с их повторяющимися датами в память
             var classes = await _context.Classes
                 .Include(c => c.ClassesDescription) 
                 .Include(c => c.Teacher) 
                 .Include(c => c.Room)
                 .Include(c => c.Campus)
                 .Include(c => c.RecurringClassDates) 
                 .ToListAsync();

             // Фильтруем на стороне клиента
             var filteredClasses = classes
                 .Where(c => c.RecurringClassDates.Any(r => NormalizeDayOfWeek((int)r.RecurrenceDay) == dayOfWeek
                     && (r.IsEveryWeek ||
                         (r.IsEven && today.Day % 2 == 0) ||
                         (!r.IsEven && today.Day % 2 != 0))) // Проверка по времени
                 )
                 //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! checkout
              Фильтрация по дню недели: Мы проверяем, есть ли у класса запись о повторяющемся занятии на заданный день недели(r.RecurrenceDay == dayOfWeek).
              Четность: Используя r.IsEveryWeek, r.IsEven, и проверяя, четный ли сегодня день(today.Day % 2 == 0), мы определяем, является ли класс повторяющимся 
              для этого дня.
              Проверка времени: Мы сравниваем текущее время(today.TimeOfDay) с временем начала и окончания занятия(RecurrenceStartTime и RecurrenceEndTime), 
             чтобы убедиться, что занятие все еще актуально. 

                .Select(c => new
                 {
                     ClassId = c.ClassesId,
                     ClassTitle = c.ClassesDescription.Title,
                     TeacherId = c.TeacherId,
                     TeacherName = c.Teacher.Username,
                     TeacherTitle = c.Teacher.Title,
                     RoomId = c.RoomId,
                     RoomNumber = c.Room.RoomNumber,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.CampusName,
                    RecurrenceDay = c.RecurringClassDates
                         .Where(r => r.RecurrenceDay == dayOfWeek)
                         .Select(r => r.RecurrenceDay.ToString()) // Для получения имени дня недели
                         .FirstOrDefault()
                 })
                 .ToList(); // Преобразуем результат в список

             if (!filteredClasses.Any())
             {
                 return NotFound (new { Message = "Class not found." , dayOfWeek});
             }


             return Ok(filteredClasses);
         }*/
        public async Task<IActionResult> GetRecurringClassBydate(DateTime date)
        {
            
            var dayOfWeek = date.DayOfWeek;


            bool isEvenWeek = IsEvenWeek(date);

         
            var classes = await _context.Classes
                .Include(c => c.ClassesDescription)
                .Include(c => c.Teacher)
                .Include(c => c.Room)
                .Include(c => c.Campus)
                .Include(c => c.RecurringClassDates)
                .ToListAsync();

            var filteredClasses = classes
                .Where(c => c.RecurringClassDates.Any(r =>
                    r.RecurrenceDay == dayOfWeek && 
                    (r.IsEveryWeek ||
                     (r.IsEven && isEvenWeek) ||
                     (!r.IsEven && !isEvenWeek)) 
                ))
                .Select(c => new
                {
                    ClassId = c.ClassesId,
                    ClassTitle = c.ClassesDescription.Title,
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher.Username,
                    TeacherTitle = c.Teacher.Title,
                    RoomId = c.RoomId,
                    RoomNumber = c.Room.RoomNumber,
                    CampusId = c.CampusId,
                    CampusName = c.Campus.CampusName,
                    RecurrenceDay = c.RecurringClassDates
                        .Where(r => r.RecurrenceDay == dayOfWeek)
                        .Select(r => r.RecurrenceDay.ToString()) 
                        .FirstOrDefault()
                })
                .ToList();

            if (!filteredClasses.Any())
            {
                return NotFound(new { Message = "No classes found for the specified date.", DayOfWeek = dayOfWeek });
            }

            return Ok(filteredClasses);
        }

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
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.TeacherId)

        }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expires in 7 days
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

}




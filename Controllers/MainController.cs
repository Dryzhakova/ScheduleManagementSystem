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

/* 

� ��� ��� ���� ����� � � ���������� ��� ������ ��������� ���� � ������� "2024-12-03", � �� "2024-12-01T00:42:13.553Z" �-� � ��� ������� ��� ��� �� ������ ��
Azami � 01.12.2024 01:49
�, � ���������, ��� �� � ��� ���� ������ �������� ��� ��������� �������, ������� ���� �� ����� �������� ������� ��� id 1 ������� ������� �� ��� ���� � ��,
�-� ��� ��������� ������� ��� ������ ����� ������� ��� ���� ������ ����������� � �������
 * 
 * DB TOKEN TABLE  - ID TOKEN -
 * login/verification
 * 
 * 
 * endpoint
 *  1.1 Delete/Update 
 *   2.1 Class, 
 *   2.2 teacher ���� ���� ��������� ������� ������� 
 *  1.2 IsRoomOccupied FUCK NE RABOTAET
 *  1.4     [HttpGet("{teacherId}/room/all")] ���� ������������� �� ������� ����������� � ������� �������� ����� � �������� 
 *  
 * chackout
 * - regist password(lenght
 * - room only digits
 * - time - fucking simillarity start/end
 */

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
        public async Task<IActionResult> Login([FromBody] TeacherLoginModel model)
        {
            // Check if user exists
            var teacher = _context.Teachers.SingleOrDefault(t => t.Username == model.Username);
            if (teacher == null)return BadRequest("Invalid login");

            var existedToken = await _context.UserTokens.SingleOrDefaultAsync(ut => ut.TeacherID == teacher.TeacherId && ut.Expiration > DateTime.UtcNow);

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
                // ��������� TeacherId � ������
                // HttpContext.Session.SetString("TeacherId", teacher.TeacherId);
                return Ok(new { Message = "Login successful", TeacherId = teacher.TeacherId, Token = token, Expiration = userToken.Expiration.ToShortDateString() });
            }


            return Ok(new { Message = "Login successful", TeacherId = teacher.TeacherId, Token = existedToken.Token, Expiration = existedToken.Expiration.ToShortDateString() });


        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken(string token)
        {
            var userToken = await _context.UserTokens
                .Include(ut => ut.TeacherID)
                .SingleOrDefaultAsync(ut => ut.Token == token && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token" });

            return Ok(new
            {
                Message = "Token is valid",
                Username = userToken.teacher.Username,
                Expiration = userToken.Expiration
            });
        }

        /*   [HttpPost("IsRoomOccupied")]//nie rabotaet
           public async Task<bool> IsRoomOccupied(string roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, string? classId = null)
           {
               var existingClasses = await _context.Classes
                   .Include(c => c.OneTimeClassDates)
                   .Include(c => c.RecurringClassDates)
                   .Where(c => c.RoomId == roomId)
                   .ToListAsync();

               var isOccupiedOneTime = existingClasses
                   .SelectMany(c => c.OneTimeClassDates)
                   .Any(o =>
                           o.OneTimeClassFullDate.Value.Date == date.Date &&
                           o.OneTimeClassStartTime < endTime &&
                           o.OneTimeClassEndTime > startTime &&
                            (string.IsNullOrEmpty(classId) || o.ClassesId != classId)
                           );

               var isOccupiedRecurring = existingClasses
                  .SelectMany(c => c.RecurringClassDates)
                  .Any(r =>
                          r.RecurrenceDay == date.DayOfWeek &&
                          r.RecurrenceStartTime < endTime &&
                          r.RecurrenceEndTime > startTime &&
                           (string.IsNullOrEmpty(classId) || r.ClassesId != classId)
                          );
               return isOccupiedOneTime || isOccupiedRecurring;
           }*/

        [HttpPost("createClass")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest model, string teacherId, string teacherToken)
        {

           // var teacherId = HttpContext.Session.GetString("TeacherId");

            if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

            var userToken = await _context.UserTokens
               .FirstOrDefaultAsync(ut => ut.TeacherID == teacherId && ut.Token == teacherToken && ut.Expiration > DateTime.UtcNow);

            if (userToken == null) return Unauthorized(new { message = "Invalid or expired token." });

            if (userToken.TeacherID == null) return BadRequest(new { message = "Teacher ID is not associated with the token." });

            /*var checkTeacherId = await _context.Teachers
             .AsNoTracking()
             .SingleOrDefaultAsync(r => r.TeacherId == teacherId);

            if (checkTeacherId == null) return NotFound("Teacher ID is not found.");*/

            var existingRoom = await _context.Rooms.SingleOrDefaultAsync(r => r.RoomNumber == model.RoomNumber);
            if (existingRoom == null)
            {
                if (!int.TryParse(model.RoomNumber, out _)) throw new ArgumentException("Room number must contain only digits.");
                existingRoom = new Room { RoomNumber = model.RoomNumber };
                await _context.Rooms.AddAsync(existingRoom);
            }

         /*   var isOccupied = await IsRoomOccupied(
                 roomId: existingRoom.RoomId,
                 date: model.OneTimeClassFullDate ?? DateTime.MinValue,
                 startTime: model.OneTimeClassStartTime.ToTimeSpan(),
                 endTime: model.OneTimeClassEndTime.ToTimeSpan()
                 );

            if (isOccupied)
            {
                return Conflict(new { Message = "The room is occupied during the specified time." });
            }*/

            // o.OneTimeClassEndTime >= model.OneTimeClassStartTime � ������� ����� ������
            /*   // ���������, ��� ������� �� ������ � ��������� �����
               bool isRoomOccupied = _context.Classes
               .Include(c => c.OneTimeClassDates)
               .Include(c => c.RecurringClassDates)
               .Where(c => c.RoomId == existingRoom.RoomId) // ��������� �� ������ �������
               .Any(c =>
                   // �������� ��� ����������� �������
                   c.OneTimeClassDates.Any(o =>
                       o.OneTimeClassFullDate.Value.Date == model.OneTimeClassFullDate.Value.Date &&
                       o.OneTimeClassStartTime <= model.OneTimeClassEndTime &&
                       o.OneTimeClassEndTime >= model.OneTimeClassStartTime
                   ) ||

                   // �������� ��� ������������� �������
                   c.RecurringClassDates.Any(r =>
                       r.RecurrenceDay == model.RecurrenceDay &&
                       r.RecurrenceStartTime <= model.RecurrenceEndTime &&
                       r.RecurrenceEndTime >= model.RecurrenceStartTime &&
                       (r.IsEveryWeek ||
                        (r.IsEven == model.IsEven)) // ���� �������� ������
                   )
               );*/

            Room newRoom;
            Classes newClass;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! ����� ������ ���� ����� ������� �������� ���� ����� � ����� ������� ��� ������ � ������� ��� ����� ������
            // ���� ������� �� ����������, ������� �����
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

            // ������� �������� �������
            var classesDescription = new ClassesDescription
            {
                Title = model.Title, // ��������� �������
                Description = model.Description // �������� �������
            };

            await _context.ClassesDescription.AddAsync(classesDescription); // ��������� �������� ������� � ��������
            await _context.SaveChangesAsync(); // ��������� ��������� � ���� ������

            if (model.IsOneTimeClass && model.OneTimeClassFullDate.HasValue)
            {
                // ������� ����� �������
                newClass = new Classes
                {
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    CampusId = newCampus.Campusid,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); // ��������� ��������� � ���� ������

                // ������� ������ ��� ������������ �������
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

 /*       [HttpPost("cancelClassOneTime/{classId}")]
        public async Task<IActionResult> CancelsOneTimeClass(string classId)
        {
            var classCancel = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classCancel == null) return NotFound(new { Message = "Class not found." });

            classCancel.IsCanceled = true;

            await _context.SaveChangesAsync();

            return Ok(new {Message = "Class canceled successfully."});
        }

        [HttpPost("restoreOneTimeClass/{classId}")]
        public async Task<IActionResult> RestoreOneTimeClass(string classId)
        {
            var classRestore = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classRestore == null) return NotFound(new { Message = "Class not found." });

            classRestore.IsCanceled = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Class canceled successfully." });
        }*/

        [HttpPost("cancelOrRestoreClassOneTime/{classId}")]
        public async Task<IActionResult> RestoreCancelOneTimeClass(string classId)
        {
            var classData = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .FirstOrDefaultAsync(c=>c.ClassesId == classId);

            if (classData == null) return NotFound(new { Message = "Class not found." });

            if(classData.IsCanceled)
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
        public async Task<IActionResult> CancelRecurringClass(string classId, [FromBody] DateTime cancellationDate)
        {

            var reccuringClass = await _context.RecurringClasses
                .FirstOrDefaultAsync(r => r.ClassesId == classId );

            if (reccuringClass == null) return NotFound(new { Message = "Recurring class not found." });

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
        public async Task<IActionResult> RestoreRecurringClass(string classId, [FromBody] DateTime restorationDate)
        {
            var canceledDate = await _context.CanceledRecurringClasses
                .FirstOrDefaultAsync(c => c.ClassesId == classId && c.CanceledDate.Value.Date == restorationDate.Date);

            if (canceledDate != null) return NotFound(new { message = "No canceled class found for the specified date." });

            _context.CanceledRecurringClasses.Remove(canceledDate); 
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Recurring class restored" });
        }

            [HttpPut("updateClass/{classId}")]
        public async Task<IActionResult> UpdateClass(string classId, [FromBody] UpdateClassRequest model)
        {
            // ���������, ���������� �� �����
            var classToUpdate = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToUpdate == null)
                return NotFound(new { Message = "Class not found." });

            // ��������� ������ ��������
            if (!string.IsNullOrEmpty(model.Title))
                classToUpdate.ClassesDescription.Title = model.Title;
            if (!string.IsNullOrEmpty(model.Description))
                classToUpdate.ClassesDescription.Description = model.Description;

            // ��������� ������ �������, ���� ������� �����
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

           /* var isOccupied = await IsRoomOccupied(
             roomId: classToUpdate.RoomId,
             date: model.OneTimeClassFullDate ?? DateTime.MinValue,
             startTime: model.OneTimeClassStartTime.ToTimeSpan(),
             endTime: model.OneTimeClassEndTime.ToTimeSpan(),
             classId: classToUpdate.ClassesId // ��������� ������� �������
               );

            if (isOccupied)
            {
                return Conflict(new { Message = "The room is occupied during the specified time." });
            }*/

            // ���������� ���������� ��� ������������ �������
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

            // ���������� ���������� ��� �������������� �������
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
        public async Task<IActionResult> DeleteClass(string classId)
        {
            // ���������, ���������� �� �����
            var classToDelete = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToDelete == null)
                return NotFound(new { Message = "Class not found." });

            // ������� ��������� ������
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
                // ���������, ���������� �� �����
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
                        // ������� ������ ����������� �������
                        _context.OneTimeClasses.RemoveRange(classToDelete.OneTimeClassDates);
                        break;

                    case "recurring":
                        // ������� ������ ������������� �������
                        _context.RecurringClasses.RemoveRange(classToDelete.RecurringClassDates);
                        break;

                    case null: // ���� deleteType �� ������, ������� ��
                    case "all":
                        // ������� ��������� ������
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
        //[HttpGet("teacher/room/all")]
        /*   public async Task<IActionResult> GetClassesForTeacher()
           {
               // �������� TeacherId �� ������
               var teacherId = HttpContext.Session.GetString("TeacherId");

               if (string.IsNullOrEmpty(teacherId)) return BadRequest("Teacher ID is missing.");

               var classes = await _context.Classes
                 .Include(c => c.Room) // ���������� ���������� � �������
                 .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������
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
                         .Include(c => c.Room)               // ���������� ���������� � �������
                         .Include(c => c.Teacher)            // ���������� ���������� � �������������
                         .Include(c => c.ClassesDescriptions) // ���������� ���������� � �������
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
                    .Include(c => c.Room) // ���������� ���������� � �������
                    .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������
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
            // �������� TeacherId �� ������
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
                        .Where(cr => cr.ClassesId == c.ClassesId) // ���������, ��������� �� ������ � ����� ������
                        .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // ����� ���� ������
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
            // �������� ��� �������� � ������ RoomId � RoomNumber
             var rooms = await _context.Rooms
                .Select(r => new
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber
                })
                .ToListAsync();

             return Ok(rooms);

  

        }

        /*  // Endpoint to get all classes for a specific room
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
              var classes = await _context.Classes
                 .Where(c => c.RoomId == room.RoomId)
                 //.Include(c => c.ClassesDescription) // ���������� �������� �������
                 //.Include(c => c.Teacher) // ���������� ���������� � �������������, ���� �����
                 .ToListAsync();



              if (classes.Count == 0)
              {
                  return NotFound("No classes found for this room.");
              }

              return Ok(classes);
          }*/

        // Endpoint to get all classes for a specific room
        [HttpGet("classes/{roomId}/id")]
        public async Task<IActionResult> GetClassesForRoom(string roomId)
        {
            // var roomId = await GetRoomIdByRoomNumberAsync(roomNumber);
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
                        .Where(cr => cr.ClassesId == c.ClassesId) // ���������, ��������� �� ������ � ����� ������
                        .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // ����� ���� ������
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
            // ���������, ���������� �� ������� � ������ �������
            var room = await _context.Rooms
                .AsNoTracking() // ������� ������������ ��� ����������� �������
                .SingleOrDefaultAsync(r => r.RoomNumber == roomNumber);

            // ���� ������� �� �������, ���������� null
            return room?.RoomId; // ���������� RoomId ��� null, ���� ������� �� �������
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


            //���� ���� ������� � ����� �������� �� �������� ����� ���� ��� ����� ������ �������� � ������
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
            // ���� ������� �� �������, ���������� null
            // return room?.RoomId; // ���������� RoomId ��� null, ���� ������� �� �������
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

            // �������� int � DayOfWeek
            DayOfWeek selectedDay = (DayOfWeek)dayOfWeek;

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

            // ���������, ���� �� ������� ��� ������� ��� ������
            if (classes.Count == 0)
            {
                return NotFound($"No classes found for the specified day of the week: {selectedDay}.");
            }

       
            // ������� ��������� � ������� �������
            var result = classes.Select(c => new
            {
                TeacherName = c.Classes.Teacher.Username, 
                TeacherId = c.Classes.Teacher.TeacherId,
                TeacherTitle = c.Classes.Teacher.Title,
                CampusId = c.Classes.CampusId,
                CampusName = c.Classes.Campus.CampusName,
                RoomNumber = c.Classes.Room.RoomNumber, 
                RoomId = c.Classes.Room.RoomId,
                ClassTitle = c.Classes.ClassesDescription.Title, 
                IsCanceled = c.Classes.IsCanceled.ToString(),
                isEveryWeek = c.IsEveryWeek.ToString(),
                IsEven = c.IsEven.ToString(),
                recurrenceDay = c.RecurrenceDay,
                recurrenceStartTime = c.RecurrenceStartTime,
                recurrenceEndTime = c.RecurrenceEndTime,
                CanceledDates = c.Classes.CanceledRecurrClass
                    .Where(cr => cr.ClassesId == c.Classes.ClassesId) // ���������, ��������� �� ������ � ����� ������
                    .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // ����� ���� ������
                    .ToList()

            }).ToList();

            // ���������� ���������
            return Ok(result);
        }

        /*     [HttpGet("classes/date/AllClassesByDate")]
             public async Task<IActionResult> GetAllClassesByDate(DateTime date)
             {
                 // �������� ����� ��� ������ ��� �������� ����
                 var dayOfWeek = date.DayOfWeek;

                 var today = DateTime.Today;

                 // ��������� ��� ������ � �� �������
                 var classes = await _context.Classes
                     .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������
                     .Include(c => c.Teacher) // ���������� ���������� � �������������
                     .Include(c => c.OneTimeClassDates) // ���������� ���������� � ����������� ����� �������
                     .Include(c => c.RecurringClassDates) // ���������� ���������� � ������������� ��������
                     .ToListAsync();

                 // ���������� ����������� �������
                 var oneTimeClasses = classes
                     .Where(c => c.OneTimeClassDates.Any(o => o.OneTimeClassFullDate?.Date == date.Date)) // ��������� �� ����
                     .Select(c => new
                     {
                         ClassId = c.ClassesId,
                         ClassTitle = c.ClassesDescription.Title,
                         TeacherName = c.Teacher.Username,
                         ClassType = "OneTime", // ��������� ��� �������
                         OneTimeClassFullDate = c.OneTimeClassDates
                             .Where(o => o.OneTimeClassFullDate?.Date == date.Date)
                             .Select(o => o.OneTimeClassFullDate?.ToString("yyyy-MM-dd"))
                     });

                 //!!!!!!!!!!!!!!!!!!!!!!! �������� ������������� IsEven � IsEveryWeek ������� �� ������ ����
                 // ���������� ������������� �������
                 var filteredClasses = classes
                     .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek
                         && (r.IsEveryWeek ||
                             (r.IsEven && today.Day % 2 == 0) ||
                             (!r.IsEven && today.Day % 2 != 0)) // �������� �� ��������
                         && (today.TimeOfDay >= r.RecurrenceStartTime && today.TimeOfDay <= r.RecurrenceEndTime)) // �������� �� �������
                     )
                     .Select(c => new
                     {
                         ClassId = c.ClassesId,
                         ClassTitle = c.ClassesDescription.Title,
                         TeacherName = c.Teacher.Username,
                         ClassType = "Recurring", // ��������� ��� �������
                         RecurrenceDay = c.RecurringClassDates
                             .Where(r => r.RecurrenceDay == dayOfWeek)
                             .Select(r => r.RecurrenceDay.ToString()) // ��� ��������� ����� ��� ������
                             .FirstOrDefault()
                     });

                 // ���������� ����������
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
            // ���������� ������ �������� � ��� ��������
            DateTime semesterStartDate = new DateTime(2024, 10, 1);
            DateTime semesterEndDate = new DateTime(2025, 2, 28);

            /*  // ���������� �������� ������� ������ ������������ ������ ��������
              int daysDifference = (date - semesterStartDate).Days;
              bool isCurrentWeekEven = (daysDifference / 7) % 2 == 0;*/
            int daysDifference = (date - semesterStartDate).Days;
            bool isCurrentWeekEven = ((daysDifference / 7) % 2) == 0;

            // �������� ����� ��� ������ ��� �������� ����
            var dayOfWeek = date.DayOfWeek;

            // ��������� ��� ������ � �� �������
            var classes = await _context.Classes
                .Include(c => c.ClassesDescription)
                .Include(c => c.Teacher)
                .Include(c => c.Campus)
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .ToListAsync();

            // ���������� ����������� �������
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

            // ���������� ������������� �������
            var recurringClasses = classes
                .Where(c => c.RecurringClassDates.Any(r =>
                    r.RecurrenceDay == dayOfWeek
                    && (r.IsEveryWeek || r.IsEven == isCurrentWeekEven)  // �������� �� ������ ������ ��� �������� ������
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

            // ���������� ����������
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

        public async Task<IActionResult> GetRecurringClassBydate(DateTime date)
        {
            // �������� ����� ��� ������ ��� �������� ����
            var dayOfWeek = date.DayOfWeek;

            var today = DateTime.Today;

            // ��������� ��� ������ � �� �������������� ������ � ������
            var classes = await _context.Classes
                .Include(c => c.ClassesDescription) 
                .Include(c => c.Teacher) 
                .Include(c => c.Room)
                .Include(c => c.Campus)
                .Include(c => c.RecurringClassDates) 
                .ToListAsync(); 


            // ��������� �� ������� �������
            var filteredClasses = classes
                .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek
                    && (r.IsEveryWeek ||
                        (r.IsEven && today.Day % 2 == 0) ||
                        (!r.IsEven && today.Day % 2 != 0)) // �������� �� ��������
                    && (today.TimeOfDay >= r.RecurrenceStartTime && today.TimeOfDay <= r.RecurrenceEndTime)) // �������� �� �������
                )
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! checkout
            /* ���������� �� ��� ������: �� ���������, ���� �� � ������ ������ � ������������� ������� �� �������� ���� ������(r.RecurrenceDay == dayOfWeek).
             ��������: ��������� r.IsEveryWeek, r.IsEven, � ��������, ������ �� ������� ����(today.Day % 2 == 0), �� ����������, �������� �� ����� ������������� 
             ��� ����� ���.
             �������� �������: �� ���������� ������� �����(today.TimeOfDay) � �������� ������ � ��������� �������(RecurrenceStartTime � RecurrenceEndTime), 
            ����� ���������, ��� ������� ��� ��� ���������. */

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
                        .Select(r => r.RecurrenceDay.ToString()) // ��� ��������� ����� ��� ������
                        .FirstOrDefault()
                })
                .ToList(); // ����������� ��������� � ������

            if (!filteredClasses.Any())
            {
                return NotFound("No classes found for the specified weekday.");
            }

           
            return Ok(filteredClasses);
        }

             /* // �������� ������ �������, ������������� �� ���� ������
             var classes = await _context.Classes
                 .Include(c => c.ClassesDescription) // ���������� ���������� � �������� �������
                 .Include(c => c.Teacher) // ���������� ���������� � �������������
                 .Include(c => c.RecurringClassDates) // ���������� ���������� � ������������� ��������
                 .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek)) // ��������� �� ��� ������
                 .Select(c => new
                 {
                     ClassId = c.ClassesId,
                     ClassTitle = c.ClassesDescription.Title,
                     TeacherName = c.Teacher.Username,
                     RecurrenceDay = c.RecurringClassDates
                         .Where(r => r.RecurrenceDay == dayOfWeek)
                         .Select(r => r.RecurrenceDay.ToString()) // ��� ��������� ����� ��� ������
                         .FirstOrDefault()
                 })
                 .ToListAsync();

            if (!classes.Any())
            {
                return NotFound("No classes found for the specified weekday.");
            }*/
      
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




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

И ещё мне надо чтобы я в эндпоинтах для отмены отправлял дату в формате "2024-12-03", а не "2024-12-01T00:42:13.553Z" т-к я без понятия что это за формат Хд
Azami — 01.12.2024 01:49
А, и последнее, раз уж у нас есть теперь эндпоинт для изменения заданий, неплохо было бы иметь эндпоинт который для id 1 занятия выводил бы всю инфу о нём,
т-к для изменения занятий мне скорее всего придётся эту инфу заново запрашивать с бэкенда
 * 
 * DB TOKEN TABLE  - ID TOKEN -
 * login/verification
 * 
 * 
 * endpoint
 *  1.1 Delete/Update 
 *   2.1 Class, 
 *   2.2 teacher если есть созданные задания удалить 
 *  1.2 IsRoomOccupied FUCK NE RABOTAET
 *  1.4     [HttpGet("{teacherId}/room/all")] если повторяющиеся не выводит одноразовые и поустое значение также и наоборот 
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
                // Сохраняем TeacherId в сессию
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

            // o.OneTimeClassEndTime >= model.OneTimeClassStartTime у времени разніе модели
            /*   // Проверяем, что комната не занята в указанное время
               bool isRoomOccupied = _context.Classes
               .Include(c => c.OneTimeClassDates)
               .Include(c => c.RecurringClassDates)
               .Where(c => c.RoomId == existingRoom.RoomId) // Фильтруем по номеру комнаты
               .Any(c =>
                   // Проверка для одноразовых занятий
                   c.OneTimeClassDates.Any(o =>
                       o.OneTimeClassFullDate.Value.Date == model.OneTimeClassFullDate.Value.Date &&
                       o.OneTimeClassStartTime <= model.OneTimeClassEndTime &&
                       o.OneTimeClassEndTime >= model.OneTimeClassStartTime
                   ) ||

                   // Проверка для повторяющихся занятий
                   c.RecurringClassDates.Any(r =>
                       r.RecurrenceDay == model.RecurrenceDay &&
                       r.RecurrenceStartTime <= model.RecurrenceEndTime &&
                       r.RecurrenceEndTime >= model.RecurrenceStartTime &&
                       (r.IsEveryWeek ||
                        (r.IsEven == model.IsEven)) // Учет четности недели
                   )
               );*/

            Room newRoom;
            Classes newClass;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! можно только одну такую комнату єксепшин если время в такой комнате уже занято с которой его хотят занять
            // Если комната не существует, создаем новую
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

            // Создаем описание занятия
            var classesDescription = new ClassesDescription
            {
                Title = model.Title, // Заголовок занятия
                Description = model.Description // Описание занятия
            };

            await _context.ClassesDescription.AddAsync(classesDescription); // Добавляем описание занятия в контекст
            await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

            if (model.IsOneTimeClass && model.OneTimeClassFullDate.HasValue)
            {
                // Создаем новое занятие
                newClass = new Classes
                {
                    TeacherId = teacherId,
                    RoomId = newRoom.RoomId,
                    CampusId = newCampus.Campusid,
                    ClassesDescriptionId = classesDescription.ClassesDescriptionId
                };

                await _context.Classes.AddAsync(newClass);
                await _context.SaveChangesAsync(); // Сохраняем изменения в базе данных

                // Создаем запись для одноразового занятия
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
               

                // Запись для повторяющегося занятия
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
            // Проверяем, существует ли класс
            var classToUpdate = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToUpdate == null)
                return NotFound(new { Message = "Class not found." });

            // Обновляем данные описания
            if (!string.IsNullOrEmpty(model.Title))
                classToUpdate.ClassesDescription.Title = model.Title;
            if (!string.IsNullOrEmpty(model.Description))
                classToUpdate.ClassesDescription.Description = model.Description;

            // Обновляем данные комнаты, если указана новая
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
             classId: classToUpdate.ClassesId // Исключаем текущее занятие
               );

            if (isOccupied)
            {
                return Conflict(new { Message = "The room is occupied during the specified time." });
            }*/

            // Обновление расписания для одноразового занятия
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

            // Обновление расписания для повторяющегося занятия
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
            // Проверяем, существует ли класс
            var classToDelete = await _context.Classes
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .Include(c => c.ClassesDescription)
                .FirstOrDefaultAsync(c => c.ClassesId == classId);

            if (classToDelete == null)
                return NotFound(new { Message = "Class not found." });

            // Удаляем связанные записи
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
        [HttpGet("teacher/all")]
        public async Task<IActionResult> GetAllTeacherData()
        {
            // Получаем все кабинеты с полями RoomId и RoomNumber
            var teacher = await _context.Teachers
               .Select(r => new
               {
                   TeacherId = r.TeacherId,
                   TeacherName = r.Username,
                   TeacherTitle =  r.Title
               })
               .ToListAsync();

            return Ok(teacher);

          /*  // Получаем всех учителей
            var teachers = await _context.Teachers
                .Select(t => new
                {
                    TeacherId = t.TeacherId,
                    TeacherName = t.Username,
                    ClassTitle = (string)null // Изначально без данных
                })
                .ToListAsync();

            // Получаем классы с описанием и преподавателем
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

            // Добавляем информацию о классах к учителям
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
            // Получаем TeacherId из сессии
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
                        .Where(cr => cr.ClassesId == c.ClassesId) // Проверяем, относится ли отмена к этому классу
                        .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // Берем дату отмены
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
            // Получаем все кабинеты с полями RoomId и RoomNumber
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
                   .Include(c => c.ClassesDescription)  // Присоединяем информацию о описании занятия, если требуется
                   .Select(c => new
                   {
                       //ClassTitle = c.ClassesDescriptions.Select(d => d.Title).FirstOrDefault()
                       RoomNumber = c.Room.RoomNumber,
                       TeacherName = c.Teacher.Username,
                       ClassTitle = c.ClassesDescription.Title,
                       RoomDescription = c.ClassesDescription.Description  
                   })
                  .ToListAsync();

              // Проверяем, есть ли занятия для данной комнаты
              if (classes.Count == 0)
              {
                  return NotFound("No classes found for this room.");
              }

              // Возвращаем список занятий
              return Ok(classes);

              //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! нужно выводить по айди а не по комнате сделать проверку и вывод
              var classes = await _context.Classes
                 .Where(c => c.RoomId == room.RoomId)
                 //.Include(c => c.ClassesDescription) // Подключаем описание занятия
                 //.Include(c => c.Teacher) // Подключаем информацию о преподавателе, если нужно
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
                        .Where(cr => cr.ClassesId == c.ClassesId) // Проверяем, относится ли отмена к этому классу
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


            //если есть задание к этому кабинету то добавить вывод если нет вывод данных кабинета и здания
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
            // Если комната не найдена, возвращаем null
            // return room?.RoomId; // Возвращаем RoomId или null, если комната не найдена
        }

        // Endpoint to get all classes for a specific day of the week
        [HttpGet("classes/day/dayOfWeek")]
        public async Task<IActionResult> GetClassesForDayOfWeek(int dayOfWeek)
        {
            // Проверяем, что введенное значение дня недели находится в пределах от 0 до 6
            if (dayOfWeek < 0 || dayOfWeek > 6) // Так как DayOfWeek начинается с 0 (воскресенье) по 6 (суббота)
            {
                return BadRequest("Invalid day of the week. Please provide a number between 0 (Sunday) and 6 (Saturday).");
            }

            // Приводим int к DayOfWeek
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

            // Проверяем, есть ли занятия для данного дня недели
            if (classes.Count == 0)
            {
                return NotFound($"No classes found for the specified day of the week: {selectedDay}.");
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
                ClassTitle = c.Classes.ClassesDescription.Title, 
                IsCanceled = c.Classes.IsCanceled.ToString(),
                isEveryWeek = c.IsEveryWeek.ToString(),
                IsEven = c.IsEven.ToString(),
                recurrenceDay = c.RecurrenceDay,
                recurrenceStartTime = c.RecurrenceStartTime,
                recurrenceEndTime = c.RecurrenceEndTime,
                CanceledDates = c.Classes.CanceledRecurrClass
                    .Where(cr => cr.ClassesId == c.Classes.ClassesId) // Проверяем, относится ли отмена к этому классу
                    .Select(cr => cr.CanceledDate.Value.ToShortDateString()) // Берем дату отмены
                    .ToList()

            }).ToList();

            // Возвращаем результат
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
            // Определяем начало семестра и его четность
            DateTime semesterStartDate = new DateTime(2024, 10, 1);
            DateTime semesterEndDate = new DateTime(2025, 2, 28);

            /*  // Определяем четность текущей недели относительно начала семестра
              int daysDifference = (date - semesterStartDate).Days;
              bool isCurrentWeekEven = (daysDifference / 7) % 2 == 0;*/
            int daysDifference = (date - semesterStartDate).Days;
            bool isCurrentWeekEven = ((daysDifference / 7) % 2) == 0;

            // Получаем номер дня недели для заданной даты
            var dayOfWeek = date.DayOfWeek;

            // Загружаем все классы с их данными
            var classes = await _context.Classes
                .Include(c => c.ClassesDescription)
                .Include(c => c.Teacher)
                .Include(c => c.Campus)
                .Include(c => c.OneTimeClassDates)
                .Include(c => c.RecurringClassDates)
                .ToListAsync();

            // Фильтрация одноразовых занятий
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

            // Фильтрация повторяющихся занятий
            var recurringClasses = classes
                .Where(c => c.RecurringClassDates.Any(r =>
                    r.RecurrenceDay == dayOfWeek
                    && (r.IsEveryWeek || r.IsEven == isCurrentWeekEven)  // Проверка на каждую неделю или четность недели
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

            // Объединяем результаты
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
                .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek
                    && (r.IsEveryWeek ||
                        (r.IsEven && today.Day % 2 == 0) ||
                        (!r.IsEven && today.Day % 2 != 0)) // Проверка на четность
                    && (today.TimeOfDay >= r.RecurrenceStartTime && today.TimeOfDay <= r.RecurrenceEndTime)) // Проверка по времени
                )
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! checkout
            /* Фильтрация по дню недели: Мы проверяем, есть ли у класса запись о повторяющемся занятии на заданный день недели(r.RecurrenceDay == dayOfWeek).
             Четность: Используя r.IsEveryWeek, r.IsEven, и проверяя, четный ли сегодня день(today.Day % 2 == 0), мы определяем, является ли класс повторяющимся 
             для этого дня.
             Проверка времени: Мы сравниваем текущее время(today.TimeOfDay) с временем начала и окончания занятия(RecurrenceStartTime и RecurrenceEndTime), 
            чтобы убедиться, что занятие все еще актуально. */

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
                return NotFound("No classes found for the specified weekday.");
            }

           
            return Ok(filteredClasses);
        }

             /* // Получаем список занятий, повторяющихся по дням недели
             var classes = await _context.Classes
                 .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия
                 .Include(c => c.Teacher) // Подключаем информацию о преподавателе
                 .Include(c => c.RecurringClassDates) // Подключаем информацию о повторяющихся занятиях
                 .Where(c => c.RecurringClassDates.Any(r => r.RecurrenceDay == dayOfWeek)) // Фильтруем по дню недели
                 .Select(c => new
                 {
                     ClassId = c.ClassesId,
                     ClassTitle = c.ClassesDescription.Title,
                     TeacherName = c.Teacher.Username,
                     RecurrenceDay = c.RecurringClassDates
                         .Where(r => r.RecurrenceDay == dayOfWeek)
                         .Select(r => r.RecurrenceDay.ToString()) // Для получения имени дня недели
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




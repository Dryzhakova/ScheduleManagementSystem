using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAppsMoodle.Models;
using System.Globalization; // Добавьте это для использования GetWeekOfYear

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

            // Сохраняем TeacherId в сессию
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

            //if (existingRoom == null) { return BadRequest("Room does not exist."); }

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

        //!!!!!!!!!!!!!!! сделать вывод названия, описания и кабинет, IsCanceled or not
        // Получаем все занятия для данного преподавателя

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
        [HttpGet("teacher/room/all")]
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
              .Include(c => c.ClassesDescription) 
              .Include(c => c.OneTimeClassDates)
              .Include(c => c.RecurringClassDates)
              .Where(c => c.TeacherId == teacherId)
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this teacher.");

            var result = classes.Select(c => new
            {
                ClassTitle = c.ClassesDescription.Title,
                ClassId = c.ClassesId,
                RoomNumber = c.Room.RoomNumber,
                RoomId = c.Room.RoomId,
                TeacherName = checkTeacherId.Username,
                TeacherId = checkTeacherId.TeacherId,
                IsCanceled = c.IsCanceled,


              RecurringClasses = c.RecurringClassDates.Select(r => new
                {
                    RecurrenceDay = r.RecurrenceDay,
                    RecurrenceStartTime = r.RecurrenceStartTime,
                    RecurrenceEndTime = r.RecurrenceEndTime,
                    IsEven = r.IsEven,
                    IsEveryWeek = r.IsEveryWeek
                }).ToList(),

                 OneTimeClasses = c.OneTimeClassDates.Select(o => new
                {
                    OneTimeClassFullDate = o.OneTimeClassFullDate,
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

            /* // Получаем все кабинеты
    var rooms = await _context.Rooms
        .Select(r => new
        {
            RoomId = r.RoomId,
            RoomNumber = r.RoomNumber,
            ClassTitle = (string)null // Изначально пустой заголовок для кабинетов без классов
        })
        .ToListAsync();

    // Получаем занятия, связанные с кабинетами
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

    // Добавляем информацию о классах к кабинетам
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
        [HttpGet("classes/room/id")]
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
              .Include(c => c.ClassesDescription)
              .Include(c => c.OneTimeClassDates)
              .Include(c => c.RecurringClassDates)
              .Where(c => c.RoomId == roomId)
              .ToListAsync();

            if (classes.Count == 0) return NotFound("No classes found for this room.");

            var result = classes.Select(c => new
            {
                ClassTitle = c.ClassesDescription.Title,
                ClassId = c.ClassesId,
                RoomNumber = room.RoomNumber,
                RoomId = room.RoomId,
                TeacherName = c.Teacher.Username,
                TeacherId = c.Teacher.TeacherId,
                IsCanceled = c.IsCanceled,


                RecurringClasses = c.RecurringClassDates.Select(r => new
                {
                    RecurrenceDay = r.RecurrenceDay,
                    RecurrenceStartTime = r.RecurrenceStartTime,
                    RecurrenceEndTime = r.RecurrenceEndTime,
                    IsEven = r.IsEven,
                    IsEveryWeek = r.IsEveryWeek
                }).ToList(),

                OneTimeClasses = c.OneTimeClassDates.Select(o => new
                {
                    OneTimeClassFullDate = o.OneTimeClassFullDate,
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
               .ThenInclude(c => c.ClassesDescription) 
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
                RoomNumber = c.Classes.Room.RoomNumber, 
                RoomId = c.Classes.Room.RoomId,
                ClassTitle = c.Classes.ClassesDescription.Title, 
                isEveryWeek = c.IsEveryWeek,
                IsEven = c.IsEven,
                recurrenceDay = c.RecurrenceDay,
                recurrenceStartTime = c.RecurrenceStartTime,
                recurrenceEndTime = c.RecurrenceEndTime

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
                .Include(c => c.ClassesDescription) // Подключаем информацию о описании занятия
                .Include(c => c.Teacher) // Подключаем информацию о преподавателе
                .Include(c => c.OneTimeClassDates) // Подключаем информацию о датах занятий
                .Where(c => c.OneTimeClassDates.Any(o => o.OneTimeClassFullDate.HasValue && o.OneTimeClassFullDate.Value.Date == date.Date)) // Фильтруем по дате
                .Select(c => new
                {
                    ClassId = c.ClassesId,
                    ClassTitle = c.ClassesDescription.Title,
                    TeacherName = c.Teacher.Username,
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
                    TeacherName = c.Teacher.Username,
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




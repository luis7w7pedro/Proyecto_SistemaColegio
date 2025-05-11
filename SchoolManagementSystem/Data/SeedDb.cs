using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Entities;
using SchoolManagementSystem.Helpers;
using Microsoft.EntityFrameworkCore;

public class SeedDb
{
    private readonly SchoolDbContext _context;
    private readonly IUserHelper _userHelper;

    public SeedDb(SchoolDbContext context, IUserHelper userHelper)
    {
        _context = context;
        _userHelper = userHelper;
    }

    public async Task SeedAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        // Crear roles (si los manejas aún con Identity)
        await _userHelper.CheckRoleAsync("Admin");
        await _userHelper.CheckRoleAsync("Student");
        await _userHelper.CheckRoleAsync("Teacher");
        await _userHelper.CheckRoleAsync("Employee");

        // Crear usuarios
        var adminUser = await CreateUserAsync("admin@school.com", "Admin", "User", "Admin123!", "Admin");
        var studentUser1 = await CreateUserAsync("student1@school.com", "Student1", "User", "Student123!", "Student");
        var studentUser2 = await CreateUserAsync("student2@school.com", "Student2", "User", "Student123!", "Student");
        var teacherUser1 = await CreateUserAsync("teacher1@school.com", "Teacher1", "User", "Teacher123!", "Teacher");
        var employeeUser1 = await CreateUserAsync("employee1@school.com", "Employee1", "User", "Employee123!", "Employee");

        // Clases
        if (!_context.SchoolClasses.Any())
        {
            var class1 = new SchoolClass
            {
                ClassName = "Class A",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6)
            };

            var class2 = new SchoolClass
            {
                ClassName = "Class B",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(6)
            };

            _context.SchoolClasses.AddRange(class1, class2);
            await _context.SaveChangesAsync();
        }

        // Materias
        if (!_context.Subjects.Any())
        {
            var subject1 = new Subject
            {
                Name = "Algebra",
                Description = "Basic Algebra",
                Credits = 5,
                TotalClasses = 30
            };

            var subject2 = new Subject
            {
                Name = "Physics",
                Description = "Basic Physics",
                Credits = 4,
                TotalClasses = 25
            };

            _context.Subjects.AddRange(subject1, subject2);
            await _context.SaveChangesAsync();
        }

        // Cursos
        if (!_context.Courses.Any())
        {
            var class1 = _context.SchoolClasses.FirstOrDefault(c => c.ClassName == "Class A");
            var class2 = _context.SchoolClasses.FirstOrDefault(c => c.ClassName == "Class B");
            var subject1 = _context.Subjects.FirstOrDefault(s => s.Name == "Algebra");
            var subject2 = _context.Subjects.FirstOrDefault(s => s.Name == "Physics");

            var course1 = new Course
            {
                Name = "Mathematics",
                Description = "Mathematics Course",
                Duration = 16,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SchoolClasses = new List<SchoolClass> { class1 }
            };

            var course2 = new Course
            {
                Name = "Science",
                Description = "Science Course",
                Duration = 16,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SchoolClasses = new List<SchoolClass> { class2 }
            };

            _context.Courses.AddRange(course1, course2);
            await _context.SaveChangesAsync();

            var courseSubject1 = new CourseSubject
            {
                CourseId = course1.Id,
                SubjectId = subject1.Id
            };

            var courseSubject2 = new CourseSubject
            {
                CourseId = course2.Id,
                SubjectId = subject2.Id
            };

            _context.CourseSubjects.AddRange(courseSubject1, courseSubject2);
            await _context.SaveChangesAsync();
        }

        // Profesores
        if (!_context.Teachers.Any())
        {
            var teacher = new Teacher
            {
                FirstName = "Teacher1",
                LastName = "User",
                UserId = teacherUser1.Id.ToString(), // Conversión de int a string
                HireDate = DateTime.UtcNow,
                Status = TeacherStatus.Active,
                AcademicDegree = AcademicDegree.MastersDegree
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            var subject1 = _context.Subjects.FirstOrDefault(s => s.Name == "Algebra");
            var subject2 = _context.Subjects.FirstOrDefault(s => s.Name == "Physics");
            var class1 = _context.SchoolClasses.FirstOrDefault(c => c.ClassName == "Class A");

            var teacherSubject1 = new TeacherSubject
            {
                TeacherId = teacher.Id,
                SubjectId = subject1?.Id ?? 0
            };

            var teacherSubject2 = new TeacherSubject
            {
                TeacherId = teacher.Id,
                SubjectId = subject2?.Id ?? 0
            };

            var teacherSchoolClass = new TeacherSchoolClass
            {
                TeacherId = teacher.Id,
                SchoolClassId = class1?.Id ?? 0
            };

            _context.TeacherSubjects.AddRange(teacherSubject1, teacherSubject2);
            _context.TeacherSchoolClasses.Add(teacherSchoolClass);
            await _context.SaveChangesAsync();
        }

        // Empleados
        if (!_context.Employees.Any())
        {
            var employee1 = new Employee
            {
                FirstName = "Employee1",
                LastName = "User",
                UserId = employeeUser1.Id.ToString(), // Conversión de int a string
                Department = Department.Administration,
                HireDate = DateTime.UtcNow,
                Status = EmployeeStatus.Active,
                PhoneNumber = "1234567890"
            };

            _context.Employees.Add(employee1);
            await _context.SaveChangesAsync();
        }
    }

    // Método modificado para guardar contraseñas en texto plano
    private async Task<User> CreateUserAsync(string email, string firstName, string lastName, string password, string role)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = email,
                Password = password, // Almacena la contraseña en texto plano
                EmailConfirmed = true,
                DateCreated = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Si deseas mantener asignación de roles
            await _userHelper.AddUserToRoleAsync(user, role);
        }
        return user;
    }

}

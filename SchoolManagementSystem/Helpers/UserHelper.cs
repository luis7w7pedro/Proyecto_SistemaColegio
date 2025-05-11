using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolManagementSystem.Data.Entities;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Repositories;
using System.Security.Claims;

namespace SchoolManagementSystem.Helpers
{
    public class UserHelper : IUserHelper
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IAlertRepository _alertRepository;

        private readonly SchoolDbContext _context;

        public UserHelper(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmployeeRepository employeeRepository,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository,
            IAlertRepository alertRepository,
            SchoolDbContext context) // Agrega SchoolDbContext como dependencia
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _employeeRepository = employeeRepository;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _alertRepository = alertRepository;
            _context = context; // Inicializa _context
        }


        public async Task<IdentityResult> AddUserAsync(User user, string password)
        {
            return await _userManager.CreateAsync(user, password);
        }

        public async Task AddUserToRoleAsync(User user, string roleName)
        {
            await _userManager.AddToRoleAsync(user, roleName);
        }

        public async Task<IdentityResult> ChangePasswordAsync(User user, string oldPassword, string newPassword)
        {
            return await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
        }

        public async Task CheckRoleAsync(string roleName)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole
                {
                    Name = roleName
                });
            }
        }

        public async Task<IdentityResult> ConfirmEmailAsync(User user, string token)
        {
            // Simula una confirmación exitosa
            return IdentityResult.Success;
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(User user)
        {
            // Devuelve un token simulado
            return "SimulatedToken";
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<bool> IsUserInRoleAsync(User user, string roleName)
        {
            return await _userManager.IsInRoleAsync(user, roleName);
        }

        public async Task<SignInResult> LoginWithClaimsAsync(string email, string password, HttpContext httpContext)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.Password != password)
            {
                return SignInResult.Failed; // Devuelve un resultado fallido si las credenciales no son válidas
            }

            // Crear los claims del usuario
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "Admin") // O podrías obtener el rol dinámicamente
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            return SignInResult.Success; // Devuelve un resultado exitoso
        }


        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string password)
        {
            return await _userManager.ResetPasswordAsync(user, token, password);
        }

        public async Task<IdentityResult> UpdateUserAsync(User user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<SignInResult> ValidatePasswordAsync(User user, string password)
        {
            return await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                false);
        }

        public async Task RemoveUserFromRoleAsync(User user, string roleName)
        {
            await _userManager.RemoveFromRoleAsync(user, roleName);
        }

        public async Task<List<User>> GetAllUsersInRoleAsync(string roleName)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            return usersInRole.ToList();
        }

        public async Task NotifyAdministrativeEmployeesPendingUserAsync(User user)
        {
            var administrativeEmployees = await _employeeRepository.GetAdministrativeEmployeesAsync();

            foreach (var adminEmployee in administrativeEmployees)
            {
                var alert = new Alert
                {
                    Message = $"New User 'Pending': {user.FullName}",
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false,
                    EmployeeId = adminEmployee.Id
                };

                await _alertRepository.CreateAsync(alert);
            }
        }

        public async Task<IdentityResult> ResetPasswordWithoutTokenAsync(User user, string password)
        {
            // Update the password directly, creating a hash from the new password
            user.Password = _userManager.PasswordHasher.HashPassword(user, password);
            return await _userManager.UpdateAsync(user);
        }

        public async Task<string> GetRoleAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault(); // Returns the first role associated with the user
        }

        public async Task UpdateUserDataByRoleAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Employee"))
            {
                var employee = await _employeeRepository.GetEmployeeByUserIdAsync(user.Id.ToString());
                if (employee != null)
                {
                    employee.FirstName = user.FirstName;
                    employee.LastName = user.LastName;
                    await _employeeRepository.UpdateAsync(employee);
                }
            }
            else if (roles.Contains("Student"))
            {
                var student = await _studentRepository.GetStudentByUserIdAsync(user.Id.ToString());
                if (student != null)
                {
                    student.FirstName = user.FirstName;
                    student.LastName = user.LastName;
                    await _studentRepository.UpdateAsync(student);
                }
            }
            else if (roles.Contains("Teacher"))
            {
                var teacher = await _teacherRepository.GetTeacherByUserIdAsync(user.Id.ToString());
                if (teacher != null)
                {
                    teacher.FirstName = user.FirstName;
                    teacher.LastName = user.LastName;
                    await _teacherRepository.UpdateAsync(teacher);
                }
            }
        }

        public async Task<Employee> GetEmployeeByUserAsync(string userEmail)
        {
            var user = await GetUserByEmailAsync(userEmail);  // Search for the user by email
            if (user == null) return null;

            return await _employeeRepository.GetEmployeeByUserIdAsync(user.Id.ToString());
        }


    }
}

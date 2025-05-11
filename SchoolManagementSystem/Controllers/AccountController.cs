using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SchoolManagementSystem.Helpers;
using SchoolManagementSystem.Models;
using SchoolManagementSystem.Data.Entities;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure;

namespace SchoolManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserHelper _userHelper;
        private readonly IMailHelper _mailHelper;
        private readonly IConfiguration _configuration;

        public AccountController(
            IUserHelper userHelper,
            IMailHelper mailHelper,
            IConfiguration configuration)
        {
            _userHelper = userHelper;
            _mailHelper = mailHelper;
            _configuration = configuration;
        }

        // Displays the login page
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Processes the login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userHelper.LoginWithClaimsAsync(model.Username, model.Password, HttpContext);

                if (result.Succeeded)
                {
                    // Redirige al dashboard correspondiente según el rol del usuario
                    var user = await _userHelper.GetUserByEmailAsync(model.Username);
                    var userRole = await _userHelper.GetRoleAsync(user);

                    switch (userRole)
                    {
                        case "Admin":
                            return RedirectToAction("AdminDashboard", "Dashboard");
                        case "Employee":
                            return RedirectToAction("EmployeeDashboard", "Dashboard");
                        case "Teacher":
                            return RedirectToAction("TeacherDashboard", "Dashboard");
                        case "Student":
                            return RedirectToAction("StudentDashboard", "Dashboard");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View(model);
        }



        // Logs out the user
        public async Task<IActionResult> Logout()
        {
            await _userHelper.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Displays the registration page
        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterNewUserViewModel
            {
                TemporaryPassword = GenerateRandomPassword()
            };

            // Fill in temporary password automatically
            model.Password = model.TemporaryPassword;
            model.Confirm = model.TemporaryPassword;

            return View(model);
        }

        // Processes the registration
        [HttpPost]
        public async Task<IActionResult> Register(RegisterNewUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Username);

                if (user == null)
                {
                    user = new User
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Username,
                        UserName = model.Username,
                        Address = model.Address,
                        PhoneNumber = model.PhoneNumber,
                        DateCreated = DateTime.UtcNow
                    };

                    string temporaryPassword = GenerateRandomPassword();
                    model.TemporaryPassword = temporaryPassword;

                    // Create user with temporary password
                    var result = await _userHelper.AddUserAsync(user, temporaryPassword);
                    if (result != IdentityResult.Success)
                    {
                        ModelState.AddModelError(string.Empty, "The user could not be created.");
                        return View(model);
                    }

                    // Assign the role "Pending"
                    await _userHelper.AddUserToRoleAsync(user, "Pending");

                    // Generates the email activation token
                    string myToken = await _userHelper.GenerateEmailConfirmationTokenAsync(user);
                    string tokenLink = Url.Action("ConfirmEmail", "Account", new { userid = user.Id, token = myToken }, protocol: HttpContext.Request.Scheme);

                    // Send email with temporary password and activation link
                    string emailBody = $"<h1>Account Created</h1><p>Your temporary password is: {temporaryPassword}</p><p>Click here to activate your account and change your password: <a href=\"{tokenLink}\">Activate Account</a></p>";

                    Helpers.Response response = _mailHelper.SendEmail(model.Username, "Account Created", emailBody);

                    if (response.IsSuccess)
                    {
                        ViewBag.Message = "User created successfully. An email was sent with further instructions.";

                        ViewBag.Links = new Dictionary<string, string>
                        {
                            { "Create Student", Url.Action("Create", "Students") },
                            { "Create Employee", Url.Action("Create", "Employees") },
                            { "Create Teacher", Url.Action("Create", "Teachers") }
                        };

                        return View(model);
                    }

                    ModelState.AddModelError(string.Empty, "Error sending confirmation email.");
                }
            }

            return View(model);
        }

        // Displays the change user details page
        public async Task<IActionResult> ChangeUser()
        {
            var user = await _userHelper.GetUserByEmailAsync(User.Identity.Name);

            if (user == null)
            {
                return RedirectToAction("NotAuthorized");
            }

            var model = new ChangeUserViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // Processes the change user details request
        [HttpPost]
        public async Task<IActionResult> ChangeUser(ChangeUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(User.Identity.Name);

                if (user != null)
                {
                    // Update the User entity
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Address = model.Address;
                    user.PhoneNumber = model.PhoneNumber;

                    // Save changes to the user
                    var result = await _userHelper.UpdateUserAsync(user);

                    if (result.Succeeded)
                    {
                        // Call the method to update associated entities (Employee, Student, Teacher)
                        await _userHelper.UpdateUserDataByRoleAsync(user);

                        return RedirectToAction("Index", "Home");
                    }

                    ModelState.AddModelError(string.Empty, "Failed to update user details.");
                }
                else
                {
                    return RedirectToAction("NotAuthorized");
                }
            }

            return View(model);
        }

        // Displays the not authorized page
        public IActionResult NotAuthorized()
        {
            return View();
        }

        // Email confirmation
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var user = await _userHelper.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userHelper.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return View("Error");
            }

            // Redirects to the page to change the first password
            return RedirectToAction("ChangeFirstPassword", new { email = user.Email, temporaryPassword = GenerateRandomPassword() });
        }

        // Displays the Change First Password page
        public IActionResult ChangeFirstPassword(string email, string temporaryPassword)
        {
            var model = new ChangeFirstPasswordViewModel
            {
                Email = email,
                TemporaryPassword = temporaryPassword
            };

            return View(model);
        }

        // Processes the Change First Password request
        [HttpPost]
        public async Task<IActionResult> ChangeFirstPassword(ChangeFirstPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);
                if (user != null)
                {
                    // Check if the temporary password is correct
                    var signInResult = await _userHelper.ValidatePasswordAsync(user, model.TemporaryPassword);
                    if (!signInResult.Succeeded) 
                    {
                        ModelState.AddModelError(string.Empty, "The temporary password is incorrect.");
                        return View(model);
                    }

                    //Resets the user's password
                    var result = await _userHelper.ResetPasswordWithoutTokenAsync(user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        ViewBag.Message = "Your password has been changed successfully. You can now log in.";
                        return RedirectToAction("Login");
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                }
            }

            return View(model);
        }


        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()?_-";
            Random random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Displays the change password page
        public IActionResult ChangePassword()
        {
            return View();
        }

        // Processes the change password request
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(this.User.Identity.Name);
                if (user != null)
                {
                    var result = await _userHelper.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("ChangeUser");
                    }
                    ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault().Description);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                }
            }

            return View(model);
        }

        // Displays the password recovery page
        public IActionResult RecoverPassword()
        {
            return View();
        }

        // Processes the password recovery request
        [HttpPost]
        public async Task<IActionResult> RecoverPassword(RecoverPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userHelper.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "The email does not correspond to a registered user.");
                    return View(model);
                }

                var myToken = await _userHelper.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Account", new { token = myToken }, protocol: HttpContext.Request.Scheme);

                Helpers.Response response = _mailHelper.SendEmail(model.Email, "Password Reset", $"<h1>Password Reset</h1>Click here to reset your password: <a href=\"{link}\">Reset Password</a>");

                if (response.IsSuccess)
                {
                    ViewBag.Message = "Instructions to recover your password have been sent to your email.";
                }

                return View();
            }

            return View(model);
        }

        // Displays the password reset page
        public IActionResult ResetPassword(string token)
        {
            return View();
        }

        // Processes the password reset request
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _userHelper.GetUserByEmailAsync(model.UserName);
            if (user != null)
            {
                var result = await _userHelper.ResetPasswordAsync(user, model.Token, model.Password);
                if (result.Succeeded)
                {
                    ViewBag.Message = "Password reset successfully.";
                    return View();
                }

                ViewBag.Message = "Error resetting the password.";
                return View(model);
            }

            ViewBag.Message = "User not found.";
            return View(model);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}

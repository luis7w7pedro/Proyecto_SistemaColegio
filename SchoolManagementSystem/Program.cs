using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Data.Entities;
using SchoolManagementSystem.Helpers;
using SchoolManagementSystem.Repositories;
using System.Text;

namespace SchoolManagementSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddFile("Logs/schoolmanagementsystem-{Date}.txt");

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // DbContext
            builder.Services.AddDbContext<SchoolDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity
            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<SchoolDbContext>()
                .AddDefaultTokenProviders();

            // Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(cfg =>
            {
                cfg.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = builder.Configuration["Tokens:Issuer"],
                    ValidAudience = builder.Configuration["Tokens:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Tokens:Key"]))
                };
            });

            // Repositories
            builder.Services.AddScoped<IAlertRepository, AlertRepository>();
            builder.Services.AddScoped<ICourseRepository, CourseRepository>();
            builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            builder.Services.AddScoped<IGradeRepository, GradeRepository>();
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
            builder.Services.AddScoped<ISchoolClassRepository, SchoolClassRepository>();
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
            builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
            builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<LocalFileHelper>();

            // Helpers
            builder.Services.AddScoped<IUserHelper, UserHelper>();
            builder.Services.AddTransient<IMailHelper, MailHelper>();
            //builder.Services.AddScoped<IBlobHelper, BlobHelper>();
            builder.Services.AddScoped<IConverterHelper, ConverterHelper>();

            // Seed data
            builder.Services.AddTransient<SeedDb>();

            
            var app = builder.Build();

            // Syncfusion License
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
                "Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWH9ec3RTRWhfWUx3XUY=");

            // Seed the database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var seedDb = services.GetRequiredService<SeedDb>();
                await seedDb.SeedAsync();

                var userHelper = services.GetRequiredService<IUserHelper>();
                await userHelper.CheckRoleAsync("Admin");
                await userHelper.CheckRoleAsync("Employee");
                await userHelper.CheckRoleAsync("Student");
                await userHelper.CheckRoleAsync("Teacher");
                await userHelper.CheckRoleAsync("Anonymous");
                await userHelper.CheckRoleAsync("Pending");
            }

            // Middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Account}/{action=Login}/{id?}");


            app.Run();
        }
    }
}


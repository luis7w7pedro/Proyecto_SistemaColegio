using MimeKit;

namespace SchoolManagementSystem.Helpers
{
    public class MailHelper : IMailHelper
    {
        private readonly IConfiguration _configuration;

        public MailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Response SendEmail(string to, string subject, string body)
        {
            // Simular el envío de correos electrónicos
            return new Response
            {
                IsSuccess = true,
                Message = "El envío de correos electrónicos está deshabilitado en esta configuración."
            };
        }
    }
}

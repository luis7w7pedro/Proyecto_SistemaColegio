using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SchoolManagementSystem.Helpers
{
    public class LocalFileHelper
    {
        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploads = Path.Combine("wwwroot", folder);
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return Path.Combine(folder, fileName); // Ruta relativa
        }
    }
}
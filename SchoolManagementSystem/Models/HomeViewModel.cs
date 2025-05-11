namespace SchoolManagementSystem.Models
{
    public class HomeViewModel
    {
        public IEnumerable<CourseViewModel> Courses { get; set; }
        public IEnumerable<SchoolClassViewModel> SchoolClasses { get; set; } 

    }
}

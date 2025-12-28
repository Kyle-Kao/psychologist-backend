using System.ComponentModel.DataAnnotations;

namespace MyProject.Data
{
    public class Staff
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string Photo { get; set; }
        public bool IsAcive { get; set; }
        public string Password { get; set; }
    }
}

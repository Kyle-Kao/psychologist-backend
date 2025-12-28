using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ✅ 必須加入這一行


namespace MyProject.Data
{
    public class Profile
    {
        [Key]
        [Column(" Id")]
        public Guid Id { get; set; }
        public string? Certification { get; set; }  
        public string? Education { get; set; }
        public string Experience { get; set; }
        public string Description { get; set; }
    }
}

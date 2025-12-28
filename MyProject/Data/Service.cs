using System.ComponentModel.DataAnnotations;

namespace MyProject.Data
{
    public class Service
    {
        [Key]
        public string Name { get; set; }
        public string Label { get; set; }
        public string Target { get; set; }
        public string Type { get; set; }
    }
}

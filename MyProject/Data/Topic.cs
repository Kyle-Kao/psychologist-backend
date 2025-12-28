using System.ComponentModel.DataAnnotations;

namespace MyProject.Data
{
    public class Topic
    {
        [Key]
        public string Name { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }
}

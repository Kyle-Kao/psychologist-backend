namespace MyProject.Data
{
    public class News
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Notice { get; set; }
        public string? Link { get; set; }
        public string CreateTime { get; set; }
        public string? UpdateTime { get; set; }
        public string TitleName { get; set; }
        public string ServiceName { get; set; }
        public string Status { get; set; }
        public string? Img { get; set; }
    }
}

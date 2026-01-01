namespace MyProject.Data
{
    public class SqlRequest
    {
        // 建議加上 ? 代表可為空 (Nullable)，或者給預設值
        public string? Sql { get; set; }
    }
}

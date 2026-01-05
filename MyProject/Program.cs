using Npgsql; // 關鍵引用
using MyProject.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using System.Data; // 關鍵引用 (ConnectionState)
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowVite", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add PostgreSQL Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddNpgsql<ApplicationDbContext>(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 開發階段建議註解掉，避免 HTTPS Port 找不到的警告
// app.UseHttpsRedirection();

app.UseCors("AllowVite");

// ==========================================
// 1. 萬能 SQL 執行器 (修復崩潰版)
// ==========================================
app.MapPost("/api/general/query", async (ApplicationDbContext dbContext, [FromBody] SqlRequest request) =>
{
    var resultList = new List<Dictionary<string, object>>();

    try
    {
        if (string.IsNullOrWhiteSpace(request?.Sql))
        {
            return Results.BadRequest(new { error = "SQL query is required", httpStatus = 400 });
        }

        // [關鍵修復] 取得連線字串，建立「全新獨立」的連線
        // 避免直接使用 dbContext.Database.GetDbConnection() 導致 macOS Error 134 崩潰
        var connStr = dbContext.Database.GetConnectionString();
        
        using var connection = new NpgsqlConnection(connStr);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = request.Sql;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.GetValue(i);
                
                // 處理 DBNull
                row.Add(columnName, value == DBNull.Value ? null : value);
            }
            resultList.Add(row);
        }
    }
    catch (PostgresException pgEx)
    {
        return Results.BadRequest(new { error = pgEx.MessageText, position = pgEx.Position, httpStatus = 400 });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, httpStatus = 500 }, statusCode: 500);
    }

    return Results.Ok(new { data = resultList, httpStatus = 200 });
})
.WithName("RunAnySql")
.WithOpenApi();


// ==========================================
// 2. Welcome 相關 API
// ==========================================
app.MapPost("/api/welcome", async (ApplicationDbContext dbContext) =>
{
    try
    {
        var welcome = await dbContext.Welcome.OrderBy(w => w.Id).FirstOrDefaultAsync();
        
        if (welcome == null)
        {
            welcome = new Welcome { Id = "1", Count = 1 };
            dbContext.Welcome.Add(welcome);
        }
        else
        {
            welcome.Count++;
            dbContext.Welcome.Update(welcome);
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { data = welcome, httpStatus = 200 });
    }
    catch (Exception ex)
    {
        return Results.Json(new { message = ex.Message, httpStatus = 500 }, statusCode: 500);
    }
})
.WithName("TrackWelcome")
.WithOpenApi();

// ==========================================
// 3. Test 相關 API
// ==========================================
app.MapGet("/api/test", async (ApplicationDbContext dbContext) =>
{
    // 使用 FromSqlRaw 範例，保持彈性
    var data = await dbContext.Test
        .FromSqlRaw("SELECT * FROM \"Test\"")
        .ToListAsync();

    return Results.Ok(new { data = data, httpStatus = 200 });
})
.WithName("GetAllTests")
.WithOpenApi();

// ==========================================
// 4. News 相關 API
// ==========================================
app.MapGet("/api/news", async (ApplicationDbContext dbContext) =>
{
    var newsQuery = await (
        from n in dbContext.News.AsNoTracking()
        join s in dbContext.Service.AsNoTracking() 
        on n.ServiceName equals s.Name into joinedServices
        from s in joinedServices.DefaultIfEmpty()
        orderby n.CreateTime descending
        select new
        {
            n.Id,
            n.Title,
            n.Content,
            n.Notice,
            n.Link,
            n.CreateTime,
            n.UpdateTime,
            n.TitleName,
            n.ServiceName,
            ServiceLabel = s != null ? s.Label : null,          
            n.Status,
            n.Img
        }
    ).ToListAsync();

    return Results.Ok(new { data = newsQuery, httpStatus = 200 });
})
.WithName("GetAllNews")
.WithOpenApi();

// ==========================================
// 5. Service & Topics 相關 API
// ==========================================
app.MapGet("/api/service", async (ApplicationDbContext dbContext) =>
{
    return Results.Ok(new { data = await dbContext.Service.ToListAsync(), httpStatus = 200 });
})
.WithName("GetAllServices")
.WithOpenApi();

app.MapGet("/api/serviceTopic", async (ApplicationDbContext dbContext) =>
{
    var joinedData = await (
            from s in dbContext.Service.AsNoTracking()
            join st in dbContext.ServiceTopic.AsNoTracking() on s.Name equals st.ServiceName
            join t in dbContext.Topic.AsNoTracking() on st.TopicName equals t.Name
            select new
            {
                name = s.Name,
                label = s.Label,
                target = s.Target,
                type = s.Type,
                topicLabel = t.Label,
                topicDescription = t.Description
            }
        ).ToListAsync();

    return Results.Ok(new { data = joinedData, httpStatus = 200, count = joinedData.Count });
})
.WithName("GetServiceTopics")
.WithOpenApi();

app.MapGet("/api/topics", async (ApplicationDbContext dbContext) =>
{
    return Results.Ok(new { data = await dbContext.Topic.ToListAsync(), httpStatus = 200 });
})
.WithName("GetAllTopics")
.WithOpenApi();

app.MapPost("/api/addTopics", async ([FromBody] Topic topic, ApplicationDbContext dbContext) =>
{
    var exists = await dbContext.Topic.AnyAsync(t => t.Name == topic.Name);
    if (exists) return Results.BadRequest(new { message = "Topic exists", httpStatus = 400 });

    dbContext.Topic.Add(topic);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/api/topics/{topic.Name}", new { data = topic, httpStatus = 201 });
})
.WithName("CreateTopic")
.WithOpenApi();

app.MapDelete("/api/removeTopics", async ([FromBody] DeleteTopicRequest request, ApplicationDbContext dbContext) =>
{
    var topic = await dbContext.Topic.FirstOrDefaultAsync(t => t.Name == request.Name);
    if (topic == null) return Results.NotFound(new { message = "Not found", httpStatus = 404 });

    dbContext.Topic.Remove(topic);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { data = topic, message = "Deleted", httpStatus = 200 });
})
.WithName("DeleteTopic")
.WithOpenApi();

app.MapPut("/api/updateTopics", async ([FromBody] Topic topic, ApplicationDbContext dbContext) =>
{
    var existingTopic = await dbContext.Topic.FirstOrDefaultAsync(t => t.Name == topic.Name);
    if (existingTopic == null) return Results.NotFound(new { message = "Not found", httpStatus = 404 });

    existingTopic.Label = topic.Label;
    existingTopic.Description = topic.Description;
    
    await dbContext.SaveChangesAsync();
    return Results.Ok(new { data = existingTopic, message = "Updated", httpStatus = 200 });
})
.WithName("UpdateTopic")
.WithOpenApi();

// ==========================================
// 6. Staff & Login 相關 API
// ==========================================
app.MapPost("/api/login", async ([FromBody] LoginRequest request, ApplicationDbContext dbContext) =>
{
    var staff = await (
        from s in dbContext.Staff.AsNoTracking()
        where s.Email == request.Email && s.Password == request.Password
        select new {
            s.Id,
            s.Name,
            s.Email,
            s.Title,
            s.Photo,
            s.IsAcive // 注意：您的 DB 欄位拼字若是 IsAcive 請維持這樣
        }
    ).FirstOrDefaultAsync();

    return Results.Ok(new { 
        data = staff, 
        httpStatus = staff != null ? 200 : 401 
    });
})
.WithName("Login")
.WithOpenApi();

app.MapGet("/api/staff", async (ApplicationDbContext dbContext) =>
{
    var staff = await dbContext.Staff.AsNoTracking()
        .Where(s => s.Title == "counselor")
        .ToListAsync();
    return Results.Ok(new { data = staff, httpStatus = 200 });
})
.WithName("GetStaff")
.WithOpenApi();

app.MapGet("/api/allstaff", async (ApplicationDbContext dbContext) =>
{
    var staff = await dbContext.Staff.AsNoTracking().ToListAsync();
    return Results.Ok(new { data = staff, httpStatus = 200 });
})
.WithName("GetAllStaff")
.WithOpenApi();

// ==========================================
// 7. Profile 相關 API
// ==========================================
app.MapGet("/api/profile", async (Guid id, ApplicationDbContext dbContext) =>
{
    var data = await (
        from p in dbContext.Profile.AsNoTracking()
        join s in dbContext.Staff.AsNoTracking() 
        on p.Id equals s.Id into joinedServices
        from s in joinedServices.DefaultIfEmpty()
        where p.Id == id
        select new {
            p.Id,
            p.Certification,
            p.Education,
            p.Experience,
            p.Description,
            StaffName = s != null ? s.Name : null,
            StaffEmail = s != null ? s.Email : null,
            StaffTitle = s != null ? s.Title : null,
            StaffPhoto = s != null ? s.Photo : null
        }
    ).FirstOrDefaultAsync();
    
    return Results.Ok(new { data = data, httpStatus = 200 });
})
.WithName("GetProfileById")
.WithOpenApi();

app.Run();

// ==========================================
// DTO 定義區域 (補上缺少的類別)
// ==========================================
internal record SqlRequest(string Sql);
internal record LoginRequest(string Email, string Password);
internal record DeleteTopicRequest(string Name);
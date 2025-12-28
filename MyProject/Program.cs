using MyProject.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseHttpsRedirection();

// API endpoint to get all Test data
app.MapGet("/api/test", async (ApplicationDbContext dbContext) =>
{
    var data = await dbContext.Test.ToListAsync();
    var response = new {
        data = data,
        httpStatus= (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetAllTests")
.WithOpenApi();

// API endpoint to get all News data
app.MapGet("/api/news", async (ApplicationDbContext dbContext) =>
{
    // var news = await dbContext.News.ToListAsync();
    // var news = await dbContext.Service.ToListAsync();

    var newsQuery = await (
        from n in dbContext.News.AsNoTracking()
        join s in dbContext.Service.AsNoTracking() 
        on n.ServiceName equals s.Name into joinedServices
        from s in joinedServices.DefaultIfEmpty()
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
            // ServiceLabel = n.ServiceName == s.Name ? s.Label : null,            
            n.Status,
            n.Img
        }
    ).ToListAsync();

    var response = new {
        data = newsQuery,
        httpStatus = (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetAllNews")
.WithOpenApi();

app.MapGet("/api/service", async (ApplicationDbContext dbContext) =>
{
    var data = await dbContext.Service.ToListAsync();
    var response = new {
        data = data,
        httpStatus= (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetAllServices")
.WithOpenApi();

app.MapGet("/api/topics", async (ApplicationDbContext dbContext) =>
{
    var data = await dbContext.Topic.ToListAsync();
    var response = new {
        data = data,
        httpStatus= (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetAllTopics")
.WithOpenApi();

app.MapPost("/api/addTopics", async ([FromBody] Topic topic, ApplicationDbContext dbContext) =>
{
    try
    {
        // 檢查 Name 是否已存在
        var existingTopic = await dbContext.Topic
            .Where(t => t.Name == topic.Name)
            .FirstOrDefaultAsync();

        if (existingTopic != null)
        {
            var response = new {
                data = (object)null,
                message = "Topic with this name already exists",
                httpStatus = (int)HttpStatusCode.BadRequest
            };
            return Results.BadRequest(response);
        }

        dbContext.Topic.Add(topic);
        await dbContext.SaveChangesAsync();

        var successResponse = new {
            data = topic,
            message = "Topic created successfully",
            httpStatus = (int)HttpStatusCode.Created
        };

        return Results.Created($"/api/topics/{topic.Name}", successResponse);
    }
    catch (Exception ex)
    {
        var errorResponse = new {
            data = (object)null,
            message = ex.Message,
            httpStatus = (int)HttpStatusCode.InternalServerError
        };
        return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("CreateTopic")
.WithOpenApi();

app.MapDelete("/api/removeTopics", async ([FromBody] DeleteTopicRequest request, ApplicationDbContext dbContext) =>
{
    try
    {
        // 查詢要刪除的 Topic
        var topicToDelete = await dbContext.Topic
            .Where(t => t.Name == request.Name)
            .FirstOrDefaultAsync();

        if (topicToDelete == null)
        {
            var response = new {
                data = (object)null,
                message = "Topic not found",
                httpStatus = (int)HttpStatusCode.NotFound
            };
            return Results.NotFound(response);
        }

        dbContext.Topic.Remove(topicToDelete);
        await dbContext.SaveChangesAsync();

        var successResponse = new {
            data = topicToDelete,
            message = "Topic deleted successfully",
            httpStatus = (int)HttpStatusCode.OK
        };

        return Results.Ok(successResponse);
    }
    catch (Exception ex)
    {
        var errorResponse = new {
            data = (object)null,
            message = ex.Message,
            httpStatus = (int)HttpStatusCode.InternalServerError
        };
        return Results.Json(errorResponse, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.WithName("DeleteTopic")
.WithOpenApi();

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
            s.IsAcive
        }
    ).FirstOrDefaultAsync();

    var response = new {
        data = staff,
        httpStatus = staff != null ? (int)HttpStatusCode.OK : (int)HttpStatusCode.Unauthorized
    };

    return Results.Ok(response);
})
.WithName("Login")
.WithOpenApi();

app.MapGet("/api/staff", async (ApplicationDbContext dbContext) =>
{
    var staffQuery = await (
        from s in dbContext.Staff.AsNoTracking()
        where s.Title == "counselor"
        select new {
            s.Id,
            s.Name,
            s.Email,
            s.Title,
            s.Photo,
            s.IsAcive,
            s.Password,
        }
    ).ToListAsync();

    var response = new {
        data = staffQuery,
        httpStatus = (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetStaff")
.WithOpenApi();

app.MapGet("/api/allstaff", async (ApplicationDbContext dbContext) =>
{
    var staffQuery = await (
        from s in dbContext.Staff.AsNoTracking()
        select new {
            s.Id,
            s.Name,
            s.Email,
            s.Title,
            s.Photo,
            s.IsAcive,
            s.Password,
        }
    ).ToListAsync();

    var response = new {
        data = staffQuery,
        httpStatus = (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetAllStaff")
.WithOpenApi();

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
    
    var response = new {
        data = data,
        httpStatus = (int)HttpStatusCode.OK
    };

    return Results.Ok(response);
})
.WithName("GetProfileById")
.WithOpenApi();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

using RDCMS.Infrastructure.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

// 注册数据库 + Redis
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddConnections();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
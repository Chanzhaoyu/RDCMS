using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RDCMS.Api.Middlewares;
using RDCMS.Application.Interfaces;
using RDCMS.Application.Services;
using RDCMS.Infrastructure.Cache;
using RDCMS.Infrastructure.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

// 注册数据库 + Redis
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddConnections();
builder.Services.AddControllers();

// 注册 Redis 缓存
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// 注册认证服务
builder.Services.AddScoped<IAuthService, AuthService>();

// 注册 JWT 认证
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret 未配置");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer 未配置");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience 未配置");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // 不允许时间偏移，过期即失效
        };
    });

var app = builder.Build();

// 全局异常中间件
app.UseMiddleware<ExceptionMiddleware>();

// 认证 & 授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
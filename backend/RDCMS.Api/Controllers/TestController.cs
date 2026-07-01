using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RDCMS.Infrastructure.Data;

namespace RDCMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _db;

    public TestController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("db-check")]
    public async Task<IActionResult> CheckDb()
    {
        var count = await _db.Users.CountAsync();
        return Ok(new { message = "数据库连接成功", userCount = count });
    }
}
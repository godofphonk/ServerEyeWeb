namespace ServerEye.API.Controllers;

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    [HttpPost("echo")]
    public IActionResult Echo([FromBody] object data)
    {
        Console.WriteLine($"Received data: {JsonSerializer.Serialize(data)}");
        return this.Ok(new 
        { 
            received = data,
            contentType = this.Request.ContentType,
            headers = this.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
        });
    }
}

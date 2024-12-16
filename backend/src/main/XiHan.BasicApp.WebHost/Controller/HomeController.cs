using Microsoft.AspNetCore.Mvc;

namespace XiHan.BasicApp.WebHost.Controller;

/// <summary>
/// HomeController
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1")]
public class HomeController : ControllerBase
{
    /// <summary>
    /// 测试
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    [HttpPost("Test")]
    public IActionResult Test(IFormCollection collection)
    {
        return Ok(collection);
    }

    /// <summary>
    /// 测试1
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    [HttpPost("Test1")]
    public IActionResult Test1(IFormCollection collection)
    {
        return Ok(collection);
    }

    /// <summary>
    /// 测试2
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    [HttpPost("Test2")]
    public IActionResult Test2(IFormCollection collection)
    {
        return Ok(collection);
    }
}

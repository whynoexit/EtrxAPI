using Etrx.Application.Interfaces;
using Etrx.Application.Dtos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Etrx.API.Security;

namespace Etrx.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequestDto dto)
    {
        var user = await _usersService.CreateUserAsync(dto);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUsersWithSortAsync(
        [FromQuery] GetSortUserRequestDto dto)
    {
        return Ok(await _usersService.GetUsersWithSortAsync(dto));
    }

    [HttpGet("{handle}")]
    public async Task<IActionResult> GetUserByHandleAsync(string handle)
    {
        return Ok(await _usersService.GetUserByHandleAsync(handle));
    }

    [HttpDelete("{handle}")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> DeleteUserByHandleAsync(string handle)
    {
        if (AuthSecurity.IsAdmin(handle, HttpContext.RequestServices.GetRequiredService<IConfiguration>()))
            return BadRequest(new { message = "Нельзя удалить администратора." });
        await _usersService.DeleteUserByHandleAsync(handle);

        return NoContent();
    }

    [HttpDelete]
    [Authorize(Policy = "Admin")]
    public IActionResult DeleteAllUsersAsync()
    {
        return StatusCode(403, new { message = "Массовое удаление пользователей отключено." });
    }
}
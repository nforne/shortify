using Microsoft.AspNetCore.Mvc;
using Shortify.DTOs.UserDTOs;
using Shortify.Services.Interfaces;

namespace Shortify.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _svc;
        public UsersController(IUserService svc) => _svc = svc;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var res = await _svc.GetAllAsync(page, pageSize);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _svc.GetByIdAsync(id);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Replace(int id, [FromBody] UserDto dto)
        {
            var r = await _svc.ReplaceAsync(id, dto);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpPatch("{id:int}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchUserDto dto)
        {
            var r = await _svc.PatchAsync(id, dto);
            return r == null ? NotFound() : Ok(r);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool soft = true)
        {
            await _svc.DeleteAsync(id, soft);
            return Ok();
        }
    }
}

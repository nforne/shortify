using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shortify.DTOs.GroupDTOs;
using Shortify.Services.Interfaces;

namespace Shortify.Controllers
{
    [ApiController]
    [Route("api/groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _svc;

        public GroupsController(IGroupService svc) => _svc = svc;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GroupDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string tenantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
            => Ok(await _svc.GetAllAsync(tenantId, page, pageSize));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GroupDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _svc.GetByIdAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpPost]
        [ProducesResponseType(typeof(GroupDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Post([FromBody] CreateGroupDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(GroupDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Put(int id, [FromBody] GroupDto dto)
        {
            var updated = await _svc.ReplaceAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpPatch("{id:int}")]
        [ProducesResponseType(typeof(GroupDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Patch(int id, [FromBody] PatchGroupDto dto)
        {
            var updated = await _svc.PatchAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
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

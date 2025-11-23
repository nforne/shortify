using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shortify.DTOs.AttrDTOs;
using Shortify.Services.Interfaces;

namespace Shortify.Controllers
{
    [ApiController]
    [Route("api/attributes")]
    public class AttributesController : ControllerBase
    {
        private readonly IAttributeService _svc;
        public AttributesController(IAttributeService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateAttributeDto dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _svc.GetByIdAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string tenantId, [FromQuery] bool includePrivate = false)
        {
            var list = await _svc.ListAsync(tenantId, includePrivate);
            return Ok(list);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] UpdateAttributeDto dto)
        {
            await _svc.UpdateAsync(id, dto);
            var updated = await _svc.GetByIdAsync(id);
            return Ok(updated);
        }

        [HttpPatch("{id:int}/visibility")]
        public async Task<IActionResult> PatchVisibility(int id, [FromQuery] string visibility)
        {
            await _svc.ToggleVisibilityAsync(id, visibility);
            var updated = await _svc.GetByIdAsync(id);
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _svc.DeleteAsync(id);
            return Ok();
        }
    }
}

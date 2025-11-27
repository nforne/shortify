using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Shortify.DTOs.MetricDTOs;
using Shortify.Services.Interfaces;

namespace Shortify.Controllers
{
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricService _svc;

        public MetricsController(IMetricService svc) => _svc = svc;

        //[HttpPost]
        //public async Task<IActionResult> Post([FromBody] CreateMetricDto dto)
        //{
        //    var created = await _svc.GenerateAsync(dto);
        //    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        //}

        [HttpPost("json")]
        public async Task<IActionResult> PostJson([FromBody] CreateJsonMetricDto dto)
        {
            var created = await _svc.GenerateAsync(dto);
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
        public async Task<IActionResult> List([FromQuery] string? name = null)
        {
            var list = await _svc.ListAsync(name);
            return Ok(list);
        }

        [HttpPatch("{id:int}")]
        public async Task<IActionResult> Patch(int id, [FromBody] MetricPatchDto patch)
        {
            if (patch == null) return BadRequest();
            await _svc.PatchAsync(id, patch);
            var updated = await _svc.GetByIdAsync(id);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        //[HttpPatch("{id:int}")]
        //public async Task<IActionResult> PatchJson(int id, [FromBody] JsonPatchDocument<MetricDto> patchDoc)
        //{
        //    if (patchDoc == null) return BadRequest();
        //    var current = await _svc.GetByIdAsync(id);
        //    if (current == null) return NotFound();
        //    // apply patch in-memory DTO
        //    patchDoc.ApplyTo(current);
        //    // translate DTO back to patch/update call
        //    var patchDto = new MetricPatchDto { Name = current.Name, From = current.From, To = current.To };
        //    await _svc.PatchAsync(id, patchDto);
        //    return Ok(await _svc.GetByIdAsync(id));
        //}

        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var bytes = await _svc.DownloadAsync(id);
            if (bytes == null) return NotFound();
            return File(bytes, "application/zip", $"metric_{id}.zip");
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] CreateJsonMetricDto dto, CancellationToken ct)
        {
            if (dto == null) return BadRequest();

            try
            {
                // ReplaceAsync will either replace metadata and return MetricDto, or create+generate
                var result = await _svc.ReplaceAsync(id, dto, ct);
                if (result == null)
                {
                    // created: retrieve and return Created
                    var created = await _svc.GetByIdAsync(id, ct);
                    return CreatedAtAction(nameof(GetById), new { id = created!.Id }, created);
                }

                return Ok(result);
            }
            catch (Shortify.Core.Exceptions.NotFoundException)
            {
                return NotFound();
            }
            catch (Shortify.Core.Exceptions.ForbiddenException)
            {
                return Forbid();
            }
            catch (Shortify.Repositories.ConflictException)
            {
                return Conflict();
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _svc.DeleteAsync(id);
            return Ok();
        }

    }
}

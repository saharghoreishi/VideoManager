using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoManager.Api.DTOs;
using VideoManager.Api.Services;
namespace VideoManager.Api.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class VideoController(IVideoService service) : ControllerBase
    {
        private readonly IVideoService _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? sortBy,
            [FromQuery] string? sortDir, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;
            var result = await _service.GetAsync(search, sortBy, sortDir, page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var video = await _service.GetByIdAsync(id, ct);
            return video is null ? NotFound() : Ok(video);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVideoRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateVideoRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var updated = await _service.UpdateAsync(id, request, ct);
            return Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }


        [HttpPost("detect-text")]
        public async Task<IActionResult> DetectText([FromServices] ITextDetectionService ocr, [FromBody] DetectTextRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.VideoPath) || string.IsNullOrWhiteSpace(request.TargetText))
                return BadRequest("VideoPath and TargetText are required");

            bool found = await ocr.ContainsTextAsync(request.VideoPath, request.TargetText, request.SampleRateFps ?? 1.0, ct);
            return Ok(new { found });
        }
    }
}

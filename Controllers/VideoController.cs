using Microsoft.AspNetCore.Mvc;
using VideoManager.Models;
using VideoManager.Services;

namespace VideoManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController(IVideoService service) : ControllerBase
    {
        // Dependency Injection
        private readonly IVideoService _service = service;

        // GET /video
        [HttpGet(Name = "GetVideos")]
        public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

        // POST /video
        [HttpPost(Name = "CreateVideos")]
        public async Task<IActionResult> Create([FromBody] Video video)
            => Ok(await _service.CreateAsync(video));
    }
}

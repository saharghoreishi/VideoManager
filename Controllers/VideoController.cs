using Microsoft.AspNetCore.Mvc;
using VideoManager.Models;
using VideoManager.Services;

namespace VideoManager.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoController(IVideoService service) : ControllerBase
    {
        private readonly IVideoService _service = service;

        [HttpGet(Name = "GetVideos")]
        public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

        [HttpPost(Name = "CreateVideos")]
        public async Task<IActionResult> Create([FromBody] Video video)
            => Ok(await _service.CreateAsync(video));
    }
}

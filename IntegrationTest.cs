using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace VideoManager.Test
{
    public class IntegrationTest(WebApplicationFactory<Program> factory)
    {
        private readonly WebApplicationFactory<Program> _factory = factory;

        [Fact]
        public async Task Videos_Get_All_Should_Return_200()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/videos");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
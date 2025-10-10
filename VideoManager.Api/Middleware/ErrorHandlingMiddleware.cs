using System.Net;
using System.Text.Json;


namespace VideoManager.Api.Middleware
{
    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteProblem(context, (int)HttpStatusCode.NotFound, "Not Found", ex.Message);
            }
            catch (ArgumentException ex)
            {
                await WriteProblem(context, (int)HttpStatusCode.BadRequest, "Bad Request", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteProblem(context, (int)HttpStatusCode.InternalServerError, "Internal Server Error", "Unexpected error occurred.");
            }
        }

        private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
        {
            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = status;
            var problem = new
            {
                type = "about:blank",
                title,
                status,
                detail,
                instance = ctx.Request.Path
            };
            var json = JsonSerializer.Serialize(problem);
            await ctx.Response.WriteAsync(json);
        }
    }
}

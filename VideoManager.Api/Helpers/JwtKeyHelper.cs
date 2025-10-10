using System.Text;

namespace VideoManager.Api.Helpers
{
    public static class JwtKeyHelper
    {
        public static byte[] GetSecretBytes(IConfiguration cfg)
        {
            var keyStr = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
            var isB64 = cfg.GetValue("Jwt:KeyIsBase64", false);
            return isB64 ? Convert.FromBase64String(keyStr) : Encoding.UTF8.GetBytes(keyStr);
        }
    }
}

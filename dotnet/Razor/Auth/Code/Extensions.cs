using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Auth
{
    internal static class Extensions
    {
        internal static async Task<AuthenticationScheme> GetGitHubAuthenticationSchemeAsync(this HttpContext httpContext)
        {
            var provider = httpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await provider.GetSchemeAsync(GitHubAuthenticationDefaults.AuthenticationScheme);
            return scheme;
        }
    }
}
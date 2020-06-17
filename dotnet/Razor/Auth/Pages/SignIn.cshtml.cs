using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Auth.Pages
{
    public class SignInModel : PageModel
    {
        public AuthenticationScheme AuthenticationScheme { get; set; }

        public async Task OnGetAsync()
        {
            AuthenticationScheme = await HttpContext.GetGitHubAuthenticationSchemeAsync();
        }

        public IActionResult OnPost()
        {
            return Challenge(
                new AuthenticationProperties()
                {
                    RedirectUri = "/"
                },
                GitHubAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
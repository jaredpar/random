using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Auth.Pages
{
    public class SignOutModel : PageModel
    {
        public IActionResult OnGet() => 
            SignOut(
                new AuthenticationProperties()
                {
                    RedirectUri = "/"
                },
                CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
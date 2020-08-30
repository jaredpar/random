using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace AuthAzdo.Controllers
{
    public class AuthenticationController : Controller
    {
        [HttpGet("~/signin")]
        [HttpPost("~/signin")]
        public IActionResult SignIn([FromForm] string provider) => 
            Challenge(new AuthenticationProperties { RedirectUri = "/" });

        [HttpPost("~/signout")]
        public IActionResult SignOut() =>
            SignOut(new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

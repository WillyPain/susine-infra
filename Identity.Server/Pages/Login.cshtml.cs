using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using System.Security.Claims;

namespace Identity.Server.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet(string? returnUrl = "/")
        {
            // This just renders the page
            ErrorMessage = null;

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPost()
        {
            if (Password!="PassWord12")
            {
                ErrorMessage = "Invalid login attempt.";
                return Page(); // Reloads the page with an error message
            }
            // TODO:
            // 1. check valid return url
            // 2. check valid credentials
            // 3. sign in user
            // 4. return sign-in result

            // Create the claims-based identity that will be used by OpenIddict to generate tokens.
            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: ClaimTypes.Name,
                roleType: ClaimTypes.Role);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Email ?? ""));
            identity.AddClaim(new Claim(ClaimTypes.Name, Email ?? ""));

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                IdentityConstants.ApplicationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTime.UtcNow.AddDays(7) });

            return Redirect(ReturnUrl ?? "/");
        }
    }
}

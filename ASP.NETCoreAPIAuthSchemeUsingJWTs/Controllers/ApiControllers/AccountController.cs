using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ASP.NETCoreAPIAuthSchemeUsingJWTs.Models;
using ASP.NETCoreAPIAuthSchemeUsingJWTs.Models.ApiDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ASP.NETCoreAPIAuthSchemeUsingJWTs.Controllers.ApiControllers
{
    [Produces("application/json")]
    [Route("api/Account")]
    public class AccountController : Controller
    {
        private const string SecretKey = "don't_reveal_me_to_anybody_in_production";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
        }

        [Route("token")]
        [HttpPost]
        public async Task<IActionResult> GetAuthenticationToken([FromBody]AccountApiDto userCredentials)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest();
            }

            //try to find an application user with the provided user credentials 
            var user = await this._userManager.FindByNameAsync(userCredentials.Username);

            //if user doesn't exist return 400
            if (user == null)
            {
                return this.BadRequest();
            }

            //authenticate user with the provided user credentials 
            var credentialsCheckResult =
                await this._signInManager
                    .CheckPasswordSignInAsync(user, userCredentials.Password, false);

            //return 401 if authentication is not successful
            if (!credentialsCheckResult.Succeeded)
            {
                return this.Unauthorized();
            }

            var result = this.GenerateToken(user);

            return this.Ok(new { result });
        }

        private string GenerateToken(ApplicationUser user)
        {
            var key =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                "TokenApiAuthenticationGuide",
                "Client consuming the API",
                claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(token);

            return tokenToReturn;
        }
    }
}
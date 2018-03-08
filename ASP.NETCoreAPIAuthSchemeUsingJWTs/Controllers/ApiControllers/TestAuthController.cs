using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASP.NETCoreAPIAuthSchemeUsingJWTs.Controllers.ApiControllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [Route("api/TestAuth")]
    public class TestAuthController : Controller
    {
        [HttpGet]
        [Route("example")]
        public IActionResult GetExampleValues()
        {
            var testResult = "You have been successfully authorized via a bearer token!";

            return new ObjectResult(testResult);
        }
    }
}
# ASP.NET-Core-2.x-API-Authentication-Scheme-Using-JWT-Tokens

The purpose of this repo is to provide a simple guide on how to add an API authentication scheme using JWT tokens.

Note: This post targets ASP.NET 2.x. However, the implementation differences are not that significant. Just remember that the authentication scheme in 1.x is named in the Configure method of the Startup.cs class, while in 2.x this has been moved to the ConfigureServices method. More information can be found here.

Token authentication has been adopted as the go-to API authentication method for some time now. I won’t go into the pros and cons of token-based authentication since those have been discussed at length. The point here is that ASP.NET Core makes this process incredibly simple and can be achieved in a few steps with the use of JSON Web Tokens (JWTs).

While the UseOAuthAuthorizationServer middleware provides a convenient endpoint for token generation in ASP.NET 4.x, you can easily write your own .NET Core middleware from scratch in three easy short steps!

Step 1: New Project Setup

Start by creating a new ASP.NET Core project. I have added a new ASP.NET Core 2.0 MVC Web project with the authentication option set to “Individual User Accounts”. This is important since we need the provided out-of-the-box register/login functionality. Those user accounts will be used to make sure that a JWT is granted to a successfully authenticated user.

I’ve also added a couple of API controllers in a custom ApiControllers folder under the Controllers folder - one will be used to obtain a JWT after successful user authentication and the other will be used to test the token-based authentication scheme once complete.

Step 2: Create Endpoint For JWT Generation

In order to be able to construct a JWT you will need the JwtSecurityToken class, which comes with the System.IdentityModel.Tokens package. Its classes are responsible for the generation of security tokens, the associated handlers and various other items. It will basically do all the hard work for you!

Install it by executing the following Command Line Interface (CLI) command:

dotnet add package System.IdentityModel.Tokens.Jwt

Just remember that you need to authenticate the user before actually granting a token! This is why we created the project with the handy out-of-the-box ASP.NET Core Identity membership system. Just pass the username and password in the body of the token request:

Remember to also set the Content-Type to application/json in the request header. The rest is quite straightforward:

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

Once the user has proved his authenticity we can actually go ahead and issue that token already! That will happen in the GenerateToken method.

The first thing we need are the signing credentials that will verify the validity of the token once it makes its way back to the server. Sounds difficult? Not really! We just need to generate a new SymmetricSecurityKey from a super secret master key (preferably in the form of GUID because it has its own length/complexity requirements) and encode it using a security algorithm of choice:
{
//generate signing creds with an encoded key
private const string SecretKey = "6be3d782-bf25-47f3-90ff-74c963b916d0";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
}

We will be using the JwtSecurityToken(IssuerString, AudienceString, IEnumerable<Claim>, Lifetime, SigningCredentials) class constructor in order to specify some optional parameter such as the issuer, the intended audience, a set of claims (you can add your own custom claims if you’d like!) and token expiration. A simple claims set might look like this:

{
//add a couple of claims	
var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
}
This is all we need for that token! Just go ahead and generate it using the above-mentioned JwtSecurityToken class:
{
//generate token
var token = new JwtSecurityToken(
                "TokenApiAuthenticationGuide",
                "Client consuming the API",
                claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );
}
One last thing is required before actually returning it to the user. We need to invoke the WriteToken method of the JwtSecurityTokenHandler class in order to encode the token as a string by passing the token as a parameter:
{
//encode token to string
var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(token);
}
That’s it! Now let’s go ahead and make sure that we can actually use those tokens for user authentication.

Step 3: Add a token-based authentication scheme

Once again, we won’t be reinventing the wheel and will continue to use the existing authentication middleware with a minor tweak to make it accept JWTs as a form of authentication.

We will need to install the Microsoft.AspNetCore.Authentication.JwtBearer NuGet package on our project to actually achieve that:

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

The last step will be to update ConfigureServices method of the Startup.cs class in order to add bearer token authentication and authorization:

 // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser().Build());
            });

            services.AddAuthentication()
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;

                    cfg.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidIssuer = "TokenApiAuthenticationGuide",
                        ValidAudience = "Client consuming the API",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey))
                    };

                });
	
            //the rest of the method implementation has been omitted for brevity
        }

This is it! Let’s test it real quick and be done with it! We need to make sure the TestAuthController has been marked with the well-known [Authorize] attribute in order to secure it against unauthorized requests. The problem here is that if you don’t specify the authentication scheme as JwtBearerDefaults.AuthenticationScheme, the default cookie behaviour will occur and you will always get the login page as a return for your HTTP request. Make sure to use the [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] implementation of the Authorize Attribute for the endpoints you want to protect via bearer tokens.

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

Let’s generate a token by sending a request to the token generation endpoint - in my case /api/account/token.

Now we are ready to check if the secured test endpoint, /api/TestAuth/example, will accept the generated token as a form of authentication. We should get the above test result as a response.

Success!!! We have added a token-based authentication scheme for our ASP.NET Core API in three simple steps!!!

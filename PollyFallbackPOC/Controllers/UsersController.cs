using Microsoft.AspNetCore.Mvc;
using PollyFallbackPOC.Services;

namespace PollyFallbackPOC.Controllers
{
    [Route("/v1/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromServices] IUsersService userService,
            CancellationToken cancellationToken)
        {
            var response = await userService.GetUsers(cancellationToken);
            return Ok(response);
        }
    }
}

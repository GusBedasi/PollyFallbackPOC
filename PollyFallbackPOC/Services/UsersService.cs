using Polly;
using Polly.Fallback;

namespace PollyFallbackPOC.Services
{
    public class UsersService : IUsersService
    {
        private readonly HttpClient _httpClient;

        public UsersService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetUsers(CancellationToken cancellationToken)
        {
            //Creates a fallback policy
            var policy = CreateFallbackPolicy();

            //Executes the request which will return an error because of a typo on the resource name
            var response = await policy.ExecuteAsync(async () =>
            {
                return await _httpClient.GetStringAsync("/userss", cancellationToken);
            });

            return response;
        }

        private AsyncFallbackPolicy<string> CreateFallbackPolicy()
        {
            var policy = Policy<string>
             .Handle<Exception>()
             .FallbackAsync(async (cancellationToken) =>
             {
                 //Execute the request but this time with the right resource
                 var response = await _httpClient.GetStringAsync("/users", cancellationToken);

                 return response;
             });

            return policy;
        }
    }
}

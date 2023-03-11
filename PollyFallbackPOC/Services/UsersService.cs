using Polly;

namespace PollyFallbackPOC.Services
{
    public class UsersService : IUsersService
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy<string> _policy;
        public UsersService(HttpClient httpClient, IAsyncPolicy<string> policy)
        {
            _httpClient = httpClient;
            _policy = policy;
        }

        public async Task<string> GetUsers(CancellationToken cancellationToken)
        {
            //Executes the request which will return an error because of a typo on the resource name
            var response = await _policy.ExecuteAsync(async () =>
            {
                return await _httpClient.GetStringAsync("/userss", cancellationToken);
            });

            return response;
        }
    }
}

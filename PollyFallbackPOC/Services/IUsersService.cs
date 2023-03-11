namespace PollyFallbackPOC.Services
{
    public interface IUsersService
    {
        Task<string> GetUsers(CancellationToken cancellationToken);
    }
}

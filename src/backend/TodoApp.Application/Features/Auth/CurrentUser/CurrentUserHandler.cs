using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Auth.CurrentUser;

/// <summary>
/// Handler for retrieving the current authenticated user's profile.
/// </summary>
public class CurrentUserHandler
{
    private readonly IUserRepository _userRepository;

    public CurrentUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Retrieves the current user's profile by ID.
    /// Returns null if user no longer exists.
    /// </summary>
    public async Task<CurrentUserResponse?> Handle(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return new CurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt
        };
    }
}

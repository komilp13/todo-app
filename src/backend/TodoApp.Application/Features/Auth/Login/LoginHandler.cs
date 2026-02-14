using TodoApp.Application.Features.Auth.Register;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Auth.Login;

/// <summary>
/// Handler for user login command.
/// </summary>
public class LoginHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Handles user login by verifying email and password, then returning JWT.
    /// Uses generic error message to prevent user enumeration.
    /// </summary>
    public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        // Find user by email (case-insensitive)
        var user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);

        // Generic error for both "user not found" and "wrong password" to prevent enumeration
        if (user == null || !_passwordHashingService.VerifyPassword(command.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new InvalidOperationException("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);

        // Return response with token and user info
        return new LoginResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName
            }
        };
    }
}

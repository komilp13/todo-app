using TodoApp.Domain.Entities;
using TodoApp.Domain.Interfaces;

namespace TodoApp.Application.Features.Auth.Register;

/// <summary>
/// Handler for user registration command.
/// </summary>
public class RegisterHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterHandler(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Handles user registration by validating email uniqueness, hashing password, creating user, and returning JWT.
    /// </summary>
    public async Task<RegisterResponse> Handle(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        // Check if email already exists (case-insensitive)
        var existingUser = await _userRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"Email '{command.Email}' is already registered");
        }

        // Hash the password
        var (passwordHash, passwordSalt) = _passwordHashingService.HashPassword(command.Password);

        // Create new user entity
        var newUser = User.Create(
            email: command.Email,
            passwordHash: passwordHash,
            passwordSalt: passwordSalt,
            displayName: command.DisplayName);

        // Save user to database
        await _userRepository.AddAsync(newUser, cancellationToken);

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(newUser);

        // Return response with token and user info
        return new RegisterResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = newUser.Id,
                Email = newUser.Email,
                DisplayName = newUser.DisplayName
            }
        };
    }
}

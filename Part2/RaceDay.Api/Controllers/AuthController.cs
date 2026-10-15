using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration and login (public endpoints).</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, ITokenService tokens, IConfiguration config)
    {
        _db = db;
        _tokens = tokens;
        _config = config;
    }

    /// <summary>Registers a new user as either an Organiser or a Participant.</summary>
    /// <remarks>
    /// Role required: none (public). Registering as an Organiser also needs the organiser registration
    /// code, so that ordinary users cannot give themselves organiser powers. The password is hashed (BCrypt)
    /// before it is stored.
    /// </remarks>
    /// <response code="201">User created. Returns the new user (never the password).</response>
    /// <response code="400">Validation failed, invalid role, or missing/incorrect organiser code.</response>
    /// <response code="409">Email address is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var role = string.Equals(request.Role?.Trim(), Roles.Organiser, StringComparison.OrdinalIgnoreCase) ? Roles.Organiser
                 : string.Equals(request.Role?.Trim(), Roles.Participant, StringComparison.OrdinalIgnoreCase) ? Roles.Participant
                 : null;

        if (role is null)
        {
            return Problem(title: "Invalid role", detail: "Role must be 'Organiser' or 'Participant'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (role == Roles.Organiser)
        {
            var expectedCode = _config["Organiser:RegistrationCode"];
            if (string.IsNullOrWhiteSpace(expectedCode) || request.OrganiserCode != expectedCode)
            {
                return Problem(title: "Organiser registration refused",
                    detail: "A valid organiser registration code is required to register as an Organiser.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return Problem(title: "Email already registered", detail: "An account with this email already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", user.ToDto());
    }

    /// <summary>Authenticates a user and issues a JWT for subsequent requests.</summary>
    /// <remarks>Role required: none (public). Paste the returned token into the Swagger "Authorize" button.</remarks>
    /// <response code="200">Login successful. Returns the JWT token, its expiry and the user summary.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Problem(title: "Invalid credentials", detail: "Incorrect email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = _tokens.CreateToken(user);
        return Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt, User = user.ToDto() });
    }
}

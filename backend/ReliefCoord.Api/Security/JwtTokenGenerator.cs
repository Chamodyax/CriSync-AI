using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReliefCoord.Domain.Entities;

namespace ReliefCoord.Api.Security;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(AppUser user)
    {
        string secretKey = _configuration["Jwt:Key"] ?? "HumanitarianCrisisPlatformSuperSecretSigningKey2026!@#";
        string issuer = _configuration["Jwt:Issuer"] ?? "ReliefCoordApi";
        string audience = _configuration["Jwt:Audience"] ?? "ReliefCoordClients";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("preferred_language", user.PreferredLanguage)
        };

        if (user.OrganisationId.HasValue)
        {
            claims.Add(new Claim("organisation_id", user.OrganisationId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

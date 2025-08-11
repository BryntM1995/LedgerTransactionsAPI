using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    public AuthController(IConfiguration cfg) => _cfg = cfg;

    public record TokenRequest(string Username, string Password);
    public record TokenResponse(string AccessToken, DateTime ExpiresAtUtc);

    [AllowAnonymous]
    [HttpPost("token")]
    public ActionResult<TokenResponse> Issue([FromBody] TokenRequest req)
    {
        // DEMO: usuarios hard-coded
        // teller -> role Teller + claim acct (una cuenta)
        // auditor -> role Auditor
        string? role = null;
        var claims = new List<Claim>();

        if (req.Username == "teller" && req.Password == "teller123")
        {
            role = "Teller";
            // Asocia al teller con UNA cuenta (ajusta con el GUID real de tu Seeder)
            claims.Add(new Claim("acct", "5991b3be-4241-42b7-8b3c-59b954d6d4e6"));
            claims.Add(new Claim(ClaimTypes.Role, "Teller"));
            claims.Add(new Claim("sub", "user-teller-1"));
        }
        else if (req.Username == "auditor" && req.Password == "auditor123")
        {
            role = "Auditor";
            claims.Add(new Claim(ClaimTypes.Role, "Auditor"));
            claims.Add(new Claim("sub", "user-auditor-1"));
        }
        else
        {
            return Unauthorized(new { code = "INVALID_CREDENTIALS" });
        }

        var issuer = _cfg["Jwt:Issuer"];
        var audience = _cfg["Jwt:Audience"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:SigningKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(4);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new TokenResponse(jwt, expires));
    }
}

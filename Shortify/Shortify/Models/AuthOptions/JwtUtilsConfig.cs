namespace Shortify.Models.AuthOptions
{
    public class JwtUtilsConfig
    {

    }
}

/*

// Example: generate once (offline) and store Base64 in secret store or environment variable
var (bytes, hex, base64) = JwtKeyUtils.GenerateKeyAndFormats();
// Save `base64` to your secret store (do not commit to source control).
Console.WriteLine("Base64 (store this securely): " + base64);
Console.WriteLine("Hex (optional): " + hex);

// At application startup, read from config/secret store:
string base64FromConfig = configuration["Jwt:KeyBase64"];
var signingCreds = JwtKeyUtils.CreateSigningCredentialsFromBase64(base64FromConfig);

// Use signingCreds when configuring JWT authentication
services.AddAuthentication(...)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = (SymmetricSecurityKey)signingCreds.Key,
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });
 
 
*/
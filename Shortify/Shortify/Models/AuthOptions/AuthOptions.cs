// File: Auth/AuthOptions.cs
using Microsoft.IdentityModel.Tokens;

namespace Shortify.Models.AuthOptions
{
    public class AuthOptions
    {
        public bool DisableAuth { get; set; } = false;
        public bool PermissiveMode { get; set; } = false;
        public JwtOptions Jwt { get; set; } = new JwtOptions();
        public List<string> DevAdminEmails { get; set; } = new List<string>();
    }

    public class JwtOptions
    {
        private readonly string _hex = "e37a9b4c5f2da19e8b3c7f112ad46b9fc35e8a77b12c9d4f6a88b3e45c1a2b9f";
        public JwtOptions(string? hex = null)
        {            
                this.SigningKey = hex ?? _hex;            
        }
        public string SigningKey { get; set; } = "dev-placeholder-key-change-me";
        public string Issuer { get; set; } = "shortify";
        public string Audience { get; set; } = "shortify-audience";
        public int ExpMinutes { get; set; } = 60;
    }
}

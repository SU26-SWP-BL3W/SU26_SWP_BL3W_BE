namespace SEAL_Domain.Base
{
    public class JwtSettings
    {
        public required string SecretKey { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                throw new Exception("SecretKey cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(Issuer))
            {
                throw new Exception("Issuer cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(Audience))
            {
                throw new Exception("Audience cannot be null or empty.");
            }

            if (AccessTokenExpirationMinutes <= 0)
            {
                throw new Exception("AccessTokenExpirationMinutes must be greater than 0.");
            }

            if (RefreshTokenExpirationDays <= 0)
            {
                throw new Exception("RefreshTokenExpirationDays must be greater than 0.");
            }

            return true;
        }
    }
}

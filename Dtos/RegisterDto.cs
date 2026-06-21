namespace AuthServer.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "Client";
        public string ExternalReferenceId { get; set; }
        public string EmailAlternativo { get; set; }
    }

    public class RegisterResponseDto
    {
        public string Id { get; set; }
        public string Errors { get; set; }
        public bool Success { get; set; } = true;
    }
}

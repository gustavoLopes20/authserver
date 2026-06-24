using System.Collections.Generic;

namespace AuthServer.Dtos;

public class RegisterDto
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } = "Client";
    public string ExternalReferenceId { get; set; }
    public string EmailAlternativo { get; set; }

    // Nova lista de aplicações permitidas enviada pelo admin/sistema corporativo
    public List<string> AllowedClientIds { get; set; } = [];
}

public class RegisterResponseDto
{
    public string Id { get; set; }
    public string Errors { get; set; }
    public bool Success { get; set; } = true;
}

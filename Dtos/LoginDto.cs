namespace AuthServer.Dtos;

public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// DTO necessário para receber a requisição do código
public class RequestCodeDto
{
    public string Email { get; set; }
}

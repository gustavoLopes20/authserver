using System;

namespace AuthServer.Services.Helpers;

public static class EmailTemplates
{
    public static string GetTwoFactorTemplate(string userName, string code)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Seu Código de Acesso</title>
        </head>
        <body style='font-family: Arial, sans-serif; background-color: #f4f5f7; margin: 0; padding: 0;'>
            <table align='center' border='0' cellpadding='0' cellspacing='0' width='100%%' style='max-width: 600px; background-color: #ffffff; margin: 40px auto; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.1); overflow: hidden;'>
                <tr>
                    <td style='background-color: #050607; padding: 30px; text-align: center;'>
                        <img src='https://rentainvest.com.br/wp-content/uploads/2022/09/renta-logo-1024x252.png' alt='RentaInvest' style='max-width: 200px; height: auto; margin: 0 auto; display: block;' />
                    </td>
                </tr>
                <tr>
                    <td style='padding: 40px 30px; color: #334155;'>
                        <p style='font-size: 16px; line-height: 1.6; margin-bottom: 20px;'>Olá, <strong>{userName}</strong>,</p>
                        <p style='font-size: 16px; line-height: 1.6;'>Foi solicitada uma tentativa de login na sua conta. Use o código de verificação abaixo para concluir o acesso:</p>
                        
                        <div style='text-align: center; margin: 35px 0;'>
                            <span style='display: inline-block; background-color: #f1f5f9; color: #0f172a; font-size: 32px; font-weight: bold; letter-spacing: 6px; padding: 15px 30px; border-radius: 6px; border: 1px solid #e2e8f0;'>
                                {code}
                            </span>
                        </div>
                        
                        <p style='font-size: 14px; color: #64748b; line-height: 1.6;'>Este código é válido por apenas <strong>5 minutos</strong>. Se você não solicitou este código, ignore este e-mail por segurança.</p>
                    </td>
                </tr>
                <tr>
                    <td style='background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0;'>
                        &copy; {DateTime.UtcNow.Year} RentaInvest App. Todos os direitos reservados.
                    </td>
                </tr>
            </table>
        </body>
        </html>";
    }
}

using System;

namespace AuthServer.Infra
{
    public class InicializeDb
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated(); // nao comentar essa linha
        }
    }
}

using System;
using System.Threading.Tasks;
using Mediclin.Data.Context;

namespace Mediclin;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Testando conexão com a base de dados...");
        
        var connectionOk = await ConnectionFactory.TestConnectionAsync();
        
        if (connectionOk)
        {
            Console.WriteLine("✓ Conexão bem-sucedida!");
        }
        else
        {
            Console.WriteLine("✗ Erro na conexão:");
            Console.WriteLine(ConnectionFactory.LastErrorMessage);
        }
    }
}

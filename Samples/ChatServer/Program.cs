using System.Threading.Tasks;

namespace ChatServer
{
    public static class Program
    {
        static async Task<int> Main(string[] args)
        {
            await new ChatServer().StartAsync(args);
            return 0;
        }
    }
}

using ExamenFinal.Clases.Bots;
using System;
using System.Threading.Tasks;

namespace ExamenFinal
{
    class Program
    {
        public static async Task Main()
        {
            await new ClsTelegramBot().iniciarBot();
        }
    }
}

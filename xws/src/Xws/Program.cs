using System;
using System.Threading;
using Annium.Core.Entrypoint;

namespace Xws
{
    internal static class Program
    {
        private static void Run(
            IServiceProvider provider,
            string[] args,
            CancellationToken token
        )
        {
            Console.WriteLine("Hello from Xws");
        }

        internal static int Main(string[] args) => new Entrypoint()
            .UseServicePack<ServicePack>()
            .Run(Run, args);
    }
}
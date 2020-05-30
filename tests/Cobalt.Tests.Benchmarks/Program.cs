using BenchmarkDotNet.Running;

namespace Cobalt.Tests.Benchmarks
{
    public class Program
    {
        // TODO use benchmark switcher
        // https://benchmarkdotnet.org/articles/guides/how-to-run.html
        // https://benchmarkdotnet.org/articles/guides/console-args.html
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
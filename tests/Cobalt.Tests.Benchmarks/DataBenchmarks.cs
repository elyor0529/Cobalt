using System;
using BenchmarkDotNet.Attributes;
using Cobalt.Common.Data.Entities;

namespace Cobalt.Tests.Benchmarks
{
    public class DataBenchmarks
    {
        [Benchmark]
        public void InsertApp()
        {
            var app = new App
            {
                Id = 0L,
                Name = "Chrome",
                Background = "#fefefe",
                Icon = new Lazy<byte[]>(() => new byte[0]),
                Identification = Win32.Id("C:\\Desktop\\dumb_file.txt")
            };

            var alert = new Alert
            {
                Id = 1L,
                Limit = TimeSpan.FromHours(4),
                Reaction = Reaction.NewMessage("pls no more web browsing"),
                Target = Target.NewApp(app),
                TimeRange = Repeated.TimeRange(TimeSpan.FromHours(08), TimeSpan.FromHours(18), RepeatType.Monthly)
            };


        }
    }
}

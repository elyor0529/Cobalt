using System;
using System.IO;
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
                Icon = new MemoryStream(),
                Identification = AppIdentification.NewWin32("C:\\Desktop\\dumb_file.txt")
            };

            var alert = new Alert
            {
                Id = 1L,
                UsageLimit = TimeSpan.FromHours(4),
                ExceededReaction = Reaction.NewMessage("pls no more web browsing"),
                Target = Target.NewApp(app),
                TimeRange = TimeRange.NewRepeated(RepeatType.Monthly, TimeSpan.FromHours(08), TimeSpan.FromHours(18))
            };

            var perDay = alert.TimeRange switch
            {
                TimeRange.Once o => o.End - o.Start,
                TimeRange.Repeated r when r.Type == RepeatType.Weekly => (r.EndOfDay - r.StartOfDay) * 7,
                TimeRange.Repeated r => r.EndOfDay - r.StartOfDay,
                _ => throw new NotImplementedException()
            };

            var actions = alert.ExceededReaction switch
            {
                var x when x.IsKill => 1,
                Reaction.Message x => x.Message.Length,
                _ => throw new NotImplementedException()
            };
        }
    }
}
﻿using BenchmarkDotNet.Running;

namespace Cobalt.Tests.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
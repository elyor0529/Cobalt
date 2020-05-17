using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace Cobalt.Common.Infrastructure
{
    public static class HostRunner
    {
        public static void RunWithLogging(this IWebHost host)
        {
            try
            {
                Log.Information("Starting up host");
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unexpected Error!");
                Debugger.Break();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
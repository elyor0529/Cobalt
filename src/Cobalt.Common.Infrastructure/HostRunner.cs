using System;
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
                Log.Information("Starting up");
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application unexpectedly terminated!");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
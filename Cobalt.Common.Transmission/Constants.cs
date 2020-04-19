using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace Cobalt.Common.Transmission
{
    public static class Constants
    {
        public static string HostName = "localhost";
        public static int Port = 0x8085;
        public static ServerCredentials ServerCredentials = ServerCredentials.Insecure;
        public static ChannelCredentials ClientCredentials = ChannelCredentials.Insecure;
    }
}

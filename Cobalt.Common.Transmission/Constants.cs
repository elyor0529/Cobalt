using Grpc.Core;

namespace Cobalt.Common.Transmission
{
    public static class Constants
    {
        public static string HostName = "localhost";
        public static int Port = 0x8085;
        public static readonly ServerCredentials ServerCredentials = ServerCredentials.Insecure;
        public static readonly ChannelCredentials ClientCredentials = ChannelCredentials.Insecure;
    }
}
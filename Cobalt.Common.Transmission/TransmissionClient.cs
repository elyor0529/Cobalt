﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Cobalt.Common.Transmission.Messages;
using Cobalt.Common.Transmission.Util;
using Newtonsoft.Json;
using Serilog;

namespace Cobalt.Common.Transmission
{
    public interface ITransmissionClient : IDisposable
    {
        event EventHandler<MessageReceivedArgs> MessageReceived;
    }

    public class TransmissionClient : ITransmissionClient
    {
        private readonly Thread _listeningThread;
        private readonly NamedPipeClientStream _pipe;
        private bool _keepAlive;

        public TransmissionClient()
        {
            _pipe = new NamedPipeClientStream(
                Utilities.LocalComputer,
                Utilities.PipeName,
                PipeDirection.In,
                PipeOptions.Asynchronous);
            _pipe.Connect(Utilities.PipeConnectionTimeout);
            //_pipe.ReadMode = PipeTransmissionMode.Message;
            _keepAlive = true;

            //TODO wtf this is bugging out, causing inconsitent reads (out of order/delayed), creating another json reader is a workaround
            //var reader = new JsonTextReader(new StreamReader(_pipe)) {SupportMultipleContent = true};
            var serializer = Utilities.CreateSerializer();
            var streamReader = new StreamReader(_pipe);

            _listeningThread = new Thread(() =>
            {
                try
                {
                    while (_keepAlive)
                        using (var reader = new JsonTextReader(streamReader) {CloseInput = false})
                        {
                            SingalMessageReceived(serializer.Deserialize<MessageBase>(reader));
                        }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error on client listener thread: ");
                }
            });
            _listeningThread.Start();
        }

        public void Dispose()
        {
            _keepAlive = false;
            _pipe.Dispose();
        }

        public event EventHandler<MessageReceivedArgs> MessageReceived;

        private void SingalMessageReceived(MessageBase message)
        {
            if (MessageReceived != null)
                MessageReceived(this, new MessageReceivedArgs(message));
        }
    }
}
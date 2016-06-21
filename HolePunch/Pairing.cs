using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace HolePunch
{
    public class Pairing
    {
        public IPEndPoint hostPublic;
        public IPEndPoint hostPrivate;
        public IPEndPoint clientPublic;
        public IPEndPoint clientPrivate;
        public bool sentToHost;
        public bool sentToClient;
        public bool pingFromClientRecieved;
    }
}

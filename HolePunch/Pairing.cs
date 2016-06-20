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
        public IPEndPoint host;
        public IPEndPoint client;
        public bool sentToHost;
    }
}

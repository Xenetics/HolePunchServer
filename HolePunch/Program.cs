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
    class Program
    {
        static private float m_Version = 0.21f;
        /// <summary> Our games port </summary>
        static private int m_Port = 34197;
        /// <summary> The enpoint we listen to </summary>
        static private IPEndPoint m_Endpoint;
        /// <summary> UDP Socket </summary>
        static private UdpClient m_UdpClient;
        /// <summary> List of pairings </summary>
        static private List<Pairing> m_Pairings;

        static void Main(string[] args)
        {
            Console.WriteLine("Punch it Chewy Version " + m_Version);
            Console.WriteLine(">");
            // Setting endpoint 
            m_Endpoint = new IPEndPoint(IPAddress.Any, m_Port);
            // Spin up UDP with our endpoint
            m_UdpClient = new UdpClient(m_Endpoint);
            // Create List for pairs
            m_Pairings = new List<Pairing>();
            // Spin up a thread with the Recieve listener on it
            Thread UDPThread = new Thread(new ThreadStart(RecieveListener));
            // make thread run in background
            UDPThread.IsBackground = true;
            // Start Recieving on other thread
            UDPThread.Start();

            // Exit loop (ends thread, closes udp socket and exits the app)
            Console.WriteLine("type 'exit' to shutdown the server");
            Console.WriteLine(">");
            //Console.WriteLine("type 'ping' folloed by a ' ' followed by an IP address, to Ping a client");
            //Console.WriteLine(">");
            while (true)
            {
                string typed = Console.ReadLine();
                if(typed == "exit")
                {
                    Console.WriteLine("Shutting down...");
                    Console.WriteLine(">");
                    UDPThread.Abort();
                    m_UdpClient.Close();
                    Environment.Exit(0);
                }

                //if(typed.Contains("ping"))
                //{
                //    int start = typed.IndexOf(' ') + 1;
                //    int length = (typed.Length - start);
                //    string desiredIP = typed.Substring(start, length);
                //    Console.WriteLine("IP to ping: " + desiredIP);
                //    Console.WriteLine(">");
                //    try
                //    {
                //        byte[] data = Encoding.UTF8.GetBytes("Manual Server Ping");
                //        m_UdpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Parse(desiredIP), m_Port));
                //        Console.WriteLine("Pinged: " + desiredIP);
                //        Console.WriteLine(">");
                //    }
                //    catch (Exception e)
                //    {
                //        Console.WriteLine("Error Print: " + e);
                //    }
                //}
            }
        }

        /// <summary> Recieve listener </summary>
        static private void RecieveListener()
        {
            // Recieve loop
            while (true)
            {
                try
                {
                    IPEndPoint newEnd = new IPEndPoint(IPAddress.Any, m_Port);
                    // Recieve the data and daves packet data at byte array and makes m_endpoint match the recieve remote endpoint
                    byte[] data = m_UdpClient.Receive(ref newEnd);
                    // decodes message
                    string message = Encoding.UTF8.GetString(data);
                    // prints message
                    Console.WriteLine("From: " + newEnd.Address + " - Message: " + message);
                    Console.WriteLine(">");
                    // Handles the message depending on what was sent
                    HandleMessage(newEnd, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Print: " + e);
                }
            }
        }

        /// <summary> Matches 2 partners </summary>
        /// <param name="newEndpoint"> new Endpoint to process </param>
        /// <param name="partner"> The expcted partner of this endpoint </param>
        static private void HandlePairing(IPEndPoint newEndpoint, string message)
        {

            // if the partner field is not an IP make a new paairing for the list with a host
            if (message.Contains("Hosting"))
            {
                //TODO check for duplicate host

                // obtain IP from message
                int start = message.IndexOf('#') + 1;
                int length = (message.Length - start);
                string messageIP = message.Substring(start, length);

                Pairing newPair = new Pairing();
                newPair.hostPublic = newEndpoint;
                newPair.hostPrivate = new IPEndPoint(IPAddress.Parse(messageIP), m_Port);
                m_Pairings.Add(newPair);
                Console.WriteLine("New Host Public: " + newPair.hostPublic.Address + " - New Host Private: " + newPair.hostPrivate.Address);
                Console.WriteLine(">");
                // notify host
                byte[] response = Encoding.UTF8.GetBytes("Waiting for client" /*#" + newEndpoint.Address*/);
                m_UdpClient.SendAsync(response, response.Length, newEndpoint);
            }
            else if (message.Contains("Joining")) // If there is a partner IP then match it with a current hosting
            {
                // Pull Ip out of message for the handle pairing function
                int start = message.IndexOf('#') + 1;
                int start2 = message.IndexOf('$') + 1;
                int length = ((start2 - 1) - start);
                string hostIP = message.Substring(start, length);
                int length2 = (message.Length - start2);
                string clientPrivate = message.Substring(start2, length2);

                int i = 0;
                foreach (Pairing pair in m_Pairings)
                {
                    if (pair.hostPrivate.Address.ToString() == hostIP)
                    {
                        pair.clientPublic = newEndpoint;
                        pair.clientPrivate = new IPEndPoint(IPAddress.Parse(clientPrivate), m_Port);

                        // notify client
                        byte[] response = Encoding.UTF8.GetBytes("Host found");
                        m_UdpClient.SendAsync(response, response.Length, newEndpoint);

                        // notify host
                        byte[] response2 = Encoding.UTF8.GetBytes("Client found");
                        m_UdpClient.SendAsync(response2, response2.Length, pair.hostPublic);

                        Console.WriteLine("New Client Public: " + pair.clientPublic.Address + " - New Client Private: " + pair.clientPrivate.Address);
                        Console.WriteLine("Matching Host" + message);
                        Console.WriteLine(">");

                        InitHost(i);

                        i++;
                        return;
                    }
                }
            }
        }

        static private void InitHost(int i)
        {
            m_Pairings[i].sentToHost = true;
            Console.WriteLine("Init Host: " + m_Pairings[i].hostPublic.Address);
            Console.WriteLine(">");
            byte[] message = Encoding.UTF8.GetBytes("Ping Client #" + m_Pairings[i].clientPublic.Address + "$" + m_Pairings[i].clientPrivate.Address);
            m_UdpClient.SendAsync(message, message.Length, m_Pairings[i].hostPublic);
        }

        static private void InitClient(int i)
        {
            m_Pairings[i].sentToClient = true;
            Console.WriteLine("Init Client: " + m_Pairings[i].clientPublic.Address);
            Console.WriteLine(">");
            byte[] response = Encoding.UTF8.GetBytes("Ping Host #" + m_Pairings[i].hostPublic.Address + "$" + m_Pairings[i].hostPrivate.Address);
            m_UdpClient.SendAsync(response, response.Length, m_Pairings[i].clientPublic);
        }

        static private void HandleMessage(IPEndPoint endpoint, string message)
        {
            if (message.Contains("Hosting")) // a Player Making a host request
            {
                // encodes a response into bytes
                byte[] response = Encoding.UTF8.GetBytes("Server Recieved Host Request");
                // Sends the response to the enpoint just recieved from
                m_UdpClient.SendAsync(response, response.Length, endpoint);

                HandlePairing(endpoint, message);
            }
            else if (message.Contains("Joining")) // player making a join request
            {
                // encodes a response into bytes
                byte[] response = Encoding.UTF8.GetBytes("Server Recieved Join Request");
                // Sends the response to the enpoint just recieved from
                m_UdpClient.SendAsync(response, response.Length, endpoint);

                HandlePairing(endpoint, message);
            }
            else if (message.Contains("Client Pinged")) // host has pinged client
            {
                int i = 0;
                foreach (Pairing pair in m_Pairings)
                {
                    if (pair.hostPublic.Address.ToString() == endpoint.Address.ToString())
                    {
                        Console.WriteLine("Host: " + pair.hostPublic.Address + " Pinged: " + pair.clientPublic.Address);
                        Console.WriteLine(">");
                        InitClient(i);

                        i++;
                        return;
                    }
                }
            }
            else if (message.Contains("Host Pinged")) // client has pinged host
            {

            }
            else if (message.Contains("Ping Recieved from Client")) // host recieved ping from client
            {
                int i = 0;
                foreach (Pairing pair in m_Pairings)
                {
                    if (pair.hostPublic.Address.ToString() == endpoint.Address.ToString() && pair.clientPublic.Address.ToString() == endpoint.Address.ToString())
                    {
                        if (pair.hostPublic.Address.ToString() == endpoint.Address.ToString())
                        {
                            Console.WriteLine(pair.hostPublic.Address + " - Recieved ping from - " + pair.clientPublic.Address);
                        }
                        else if (pair.clientPublic.Address.ToString() == endpoint.Address.ToString())
                        {
                            Console.WriteLine(pair.clientPublic.Address + " - Recieved ping from - " + pair.hostPublic.Address);
                        }
                        Console.WriteLine(">");
                        m_Pairings[i].pingFromClientRecieved = true;

                        i++;
                        return;
                    }
                }
            }
        }
    }
}

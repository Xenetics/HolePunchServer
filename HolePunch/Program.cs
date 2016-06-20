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
        /// <summary> Our games port </summary>
        static private int m_Port = 34197;
        /// <summary> The enpoint we listen to </summary>
        static private IPEndPoint m_Endpoint;
        /// <summary> UDP Socket </summary>
        static private UdpClient m_UdpClient;
        /// <summary> List of pairings </summary>
        static private List<Pairing> m_Pairings;

        // for testing
        static private int messagesRecieved = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Punch it Chewy");
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
            while(true)
            {
                if(Console.ReadLine() == "exit")
                {
                    Console.WriteLine("Shutting down...");
                    UDPThread.Abort();
                    m_UdpClient.Close();
                    Environment.Exit(0);
                }
            }
        }

        /// <summary> Recieve listener </summary>
        static private void RecieveListener()
        {
            // Recieve loop
            while (true)
            {
                Console.WriteLine("Messages: " + messagesRecieved);
                try
                {
                    IPEndPoint newEnd = new IPEndPoint(IPAddress.Any, m_Port);
                    // Recieve the data and daves packet data at byte array and makes m_endpoint match the recieve remote endpoint
                    byte[] data = m_UdpClient.Receive(ref newEnd);
                    // decodes message
                    string message = Encoding.UTF8.GetString(data);
                    // prints message
                    Console.WriteLine("From: " + newEnd.Address + " - Message: " + message);
                    // Handles the message depending on what was sent
                    HandleMessage(newEnd, message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error Print: " + e);
                }
                messagesRecieved++;
            }
        }

        /// <summary> Matches 2 partners </summary>
        /// <param name="newEndpoint"> new Endpoint to process </param>
        /// <param name="partner"> The expcted partner of this endpoint </param>
        static private void HandlePairing(IPEndPoint newEndpoint, string partner = "")
        {
            // if the partner field is not an IP make a new paairing for the list with a host
            if (partner == "")
            {
                //TODO check for duplicate host


                Pairing newPair = new Pairing();
                newPair.host = newEndpoint;
                m_Pairings.Add(newPair);
                Console.WriteLine("New Host: " + newPair.host.Address);
                // notify host
                byte[] response = Encoding.UTF8.GetBytes("Waiting for client");
                m_UdpClient.SendAsync(response, response.Length, newEndpoint);
            }
            else // If there is a partner IP then match it with a current hosting
            {
                int i = 0;
                foreach (Pairing pair in m_Pairings)
                {
                    if (pair.host.Address == IPAddress.Parse(partner))
                    {
                        pair.client = newEndpoint;

                        // notify client
                        byte[] response = Encoding.UTF8.GetBytes("Host found");
                        m_UdpClient.SendAsync(response, response.Length, newEndpoint);

                        // notify host
                        byte[] response2 = Encoding.UTF8.GetBytes("Client found");
                        m_UdpClient.SendAsync(response2, response2.Length, pair.host);

                        Console.WriteLine("Host found: " + pair.host.Address + " for: " + partner);

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
            byte[] message = Encoding.UTF8.GetBytes("Ping Client #" + m_Pairings[i].client.Address);
            m_UdpClient.SendAsync(message, message.Length, m_Pairings[i].client);
        }

        static private void InitClient(int i)
        {

        }

        static private void HandleMessage(IPEndPoint endpoint, string message)
        {
            if(message.Contains("Hosting")) // a Player Making a host request
            {
                // encodes a response into bytes
                byte[] response = Encoding.UTF8.GetBytes("Server Recieved Host Request");
                // Sends the response to the enpoint just recieved from
                m_UdpClient.SendAsync(response, response.Length, endpoint);

                HandlePairing(endpoint, "");
            }
            else if(message.Contains("Joining")) // player making a join request
            {
                // Pull Ip out of message for the handle pairing function
                int start = message.IndexOf('#') + 1;
                int length = (message.Length - start);
                string desiredHost = message.Substring(message.IndexOf('#') + 1, length);

                // encodes a response into bytes
                byte[] response = Encoding.UTF8.GetBytes("Server Recieved Join Request");
                // Sends the response to the enpoint just recieved from
                m_UdpClient.SendAsync(response, response.Length, endpoint);

                HandlePairing(endpoint, desiredHost);
            }
            else if(message.Contains("Client Pinged")) // host has pinged client
            {

            }
            else if (message.Contains("Host Pinged")) // client has pinged host
            {

            }
        }
    }
}

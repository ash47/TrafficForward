using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TrafficForward
{
    class Program
    {
        // This will contain the IP address of host we will forward traffic to
        private static IPAddress forwardIP;

        // This will contain the remote endpoint we will connect to
        private static IPEndPoint remoteEP;

        // Size of the buffer to read data into (in bytes)
        private static int bufferSize = 1024 * 1024;

        static void Main(string[] args)
        {
            // Ensure we have enough arguments to forward traffic
            if(args.Length < 3)
            {
                Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName +" <listenPort> <forwardHost> <forwardPort> [<listenIP>]");
                return;
            }

            // The options for forwarding
            int listenPort;                     // The port we will listen on
            string listenInterface = "0.0.0.0"; // The interface (ip) we will listen on
            string forwardHost = args[1];       // The host/ip we will connect to
            int forwardPort;                    // The port we will listen on

            // Try to parse the command line options for ports
            try
            {
                // Read in the settings
                listenPort = Int32.Parse(args[0]);
                forwardPort = Int32.Parse(args[2]);
            }
            catch
            {
                // Log the error
                Console.WriteLine("Ports should be an number.");

                // Exit
                return;
            }

            // Did they give us an ip to listen on?
            if (args.Length >= 4)
            {
                listenInterface = args[3];
            }

            // This will hold the TCP server
            TcpListener server = null;

            // Attempt to use it as an IP Address
            try
            {
                // Try to parse it as an IP Address
                forwardIP = IPAddress.Parse(forwardHost);
            }
            catch
            {
                // Failed, try to do a DNS lookup of the hostname
                try
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(forwardHost);
                    forwardIP = ipHostInfo.AddressList[0];
                }
                catch(Exception e)
                {
                    // Failed, DNS name doesn't exist?
                    Console.WriteLine("Failed to resolve the host we will forward data to (" + forwardHost + "): " + e.Message);
                    return;
                }
            }

            // Store the remote endpoint
            remoteEP = new IPEndPoint(forwardIP, forwardPort);

            // Attempt to listen on the given port
            try
            {
                // Listen IP
                IPAddress listenIP = IPAddress.Parse(listenInterface);

                // Create the server
                server = new TcpListener(listenIP, listenPort);
                server.Start();
            }
            catch (SocketException e)
            {
                // Failed to listen, log it
                Console.WriteLine("Failed to listen on port " + listenPort + ": " + e.Message);

                // Exit
                return;
            }
            catch (Exception e)
            {
                // No idea what happened, log the error message
                Console.WriteLine("An unknown error occurred while starting the server: " + e.Message);

                // Exit
                return;
            }

            // Log what is happening
            Console.WriteLine(listenInterface + ":" + listenPort + " -> " + forwardHost + ":" + forwardPort);

            // Start loop to listen for incoming connections
            while(true)
            {
                try
                {
                    // Accept a client
                    TcpClient client = server.AcceptTcpClient();

                    // Create a new thread to handle this client
                    ThreadPool.QueueUserWorkItem(handleClientConnection, client);
                }
                catch(Exception e)
                {
                    // This should never happen
                    Console.WriteLine("An error occured while forwarding traffic: " + e.Message);
                }
            }
        }

        private static void handleClientConnection(Object state)
        {
            // Grab the client connection
            TcpClient client = (TcpClient)state;

            // This will store the socket we use to talk to the forward host
            Socket socket = null;

            // Connect to the forward host
            try
            {
                // Setup the socket to connect to the forward host
                socket = new Socket(forwardIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Store the buffer size
                socket.ReceiveBufferSize = bufferSize;
                socket.SendBufferSize = bufferSize;

                // Connect to the foward host
                socket.Connect(remoteEP);
            }
            catch(Exception e)
            {
                // An error occured while connecting
                Console.WriteLine("An error occured while connecting to the forward host: " + e.Message);
                return;
            }

            // Grab the client's network stream
            NetworkStream clientStream = client.GetStream();

            // This will be true as long as both connections are active
            bool connectionActive = true;

            // Craete a thread to read data from forward host
            ThreadPool.QueueUserWorkItem(delegate
            {
                // Will contain how many bytes are read from the remote host
                int bytesRead;

                // Create a buffer to read data into
                byte[] buffer = new byte[bufferSize];

                // Read data as long as the connection is still alive
                while (connectionActive)
                {
                    try
                    {
                        // Read data into the buffer
                        bytesRead = socket.Receive(buffer);

                        // If this value is 0, the connection is dead
                        if (bytesRead == 0)
                        {
                            // Exit the loop
                            break;
                        }

                        // Push the data to the client
                        clientStream.Write(buffer, 0, bytesRead);

                        // Flush the data
                        clientStream.Flush();
                    }
                    catch(Exception e)
                    {
                        // Log the error
                        Console.WriteLine("An unknown exception occurred while reading data from the forward host: " + e.Message);

                        // Exit the loop
                        break;
                    }
                }
            });

            // Craete a thread to read data from the client
            ThreadPool.QueueUserWorkItem(delegate
            {
                // Will contain how many bytes are read from the client
                int bytesRead;

                // Create a buffer to read data into
                byte[] buffer = new byte[bufferSize];

                // Read data as long as the connection is still alive
                while (connectionActive)
                {
                    try
                    {
                        // Read data into the buffer
                        bytesRead = clientStream.Read(buffer, 0, bufferSize);

                        // If this value is 0, the connection is dead
                        if (bytesRead == 0)
                        {
                            // Exit the loop
                            break;
                        }

                        // Push the data to the client
                        socket.Send(buffer, bytesRead, SocketFlags.None);
                    }
                    catch (Exception e)
                    {
                        // Log the error
                        Console.WriteLine("An unknown exception occurred while reading data from the client: " + e.Message);

                        // Exit the loop
                        break;
                    }
                }
            });

            // Resource cleanup
            while(connectionActive)
            {
                Thread.Sleep(100);
            }

            // Connections are dead, or there was an error
            try
            {
                // Close the socket
                socket.Close();
                clientStream.Dispose();
                client.Close();
            }
            catch
            {
                // do nothing
            }
        }
    }
}

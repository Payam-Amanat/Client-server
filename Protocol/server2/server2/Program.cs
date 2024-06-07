using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConsoleApp5
{
    class Program
    {
        static TcpListener tcpServer;
        static TcpClient tcpClient;
        static Thread th;
        static string input;
        public static void Main()
        {
            th = new Thread(new ThreadStart(StartListen));
            th.Start();
        }
        public static void StartListen()
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                //IPAddress localAddr = IPAddress.Parse("192.168.1.122");

                tcpServer = new TcpListener(localAddr, 5000);
                tcpServer.Start();

                // Keep on accepting Client Connection
                while (true)
                {

                    // New Client connected, call Event to handle it.
                    Thread t = new Thread(new ParameterizedThreadStart(NewClient));
                    tcpClient = tcpServer.AcceptTcpClient();
                    t.Start(tcpClient);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static void NewClient(Object obj)
        {
            Console.WriteLine("Client Added");
            var client = (TcpClient)obj;
            NetworkStream ns = client.GetStream();

            StateObject state = new StateObject();
            state.workSocket = client.Client;
            client.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback((new Program()).OnReceive), state);

            while (true)
            {
                input = Console.ReadLine();
                if (input == "exit")
                    break;
                else if (input.StartsWith("send:"))
                {
                    string filePath = input.Substring(5);
                    if (File.Exists(filePath))
                    {
                        SendFile(client, filePath);
                    }
                    else
                    {
                        Console.WriteLine("File not found.");
                    }
                }
            }
            Console.ReadKey();
        }
        public static void SendFile(TcpClient client, string filePath)
        {
            NetworkStream ns = client.GetStream();
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] fileData = new byte[fs.Length];
            fs.Read(fileData, 0, (int)fs.Length);
            fs.Close();

            byte[] fileNameByteArray = Encoding.ASCII.GetBytes(filePath);
            byte[] fileNameLengthByteArray = BitConverter.GetBytes(fileNameByteArray.Length);

            ns.Write(fileNameLengthByteArray, 0, fileNameLengthByteArray.Length);
            ns.Write(fileNameByteArray, 0, fileNameByteArray.Length);
            ns.Write(fileData, 0, fileData.Length);
            ns.Flush();
        }
        public void OnReceive(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead;

            if (handler.Connected)
            {

                // Read data from the client socket. 
                try
                {
                    bytesRead = handler.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        // There  might be more data, so store the data received so far.
                        state.sb.Remove(0, state.sb.Length);
                        state.sb.Append(Encoding.ASCII.GetString(
                                         state.buffer, 0, bytesRead));

                        // Display Text in Rich Text Box
                        content = state.sb.ToString();
                        Console.WriteLine(content);

                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(OnReceive), state);

                    }
                }

                catch (SocketException socketException)
                {
                    //WSAECONNRESET, the other side closed impolitely
                    if (socketException.ErrorCode == 10054 || ((socketException.ErrorCode != 10004) && (socketException.ErrorCode != 10053)))
                    {
                        handler.Close();
                    }
                }

                // Eat up exception....Hmmmm I'm loving eat!!!
                catch (Exception exception)
                {
                    //MessageBox.Show(exception.Message + "\n" + exception.StackTrace);

                }
            }
        }
    }
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
}

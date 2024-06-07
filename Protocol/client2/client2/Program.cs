using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class TcpClientSample
{
    private TcpClient server;
    private NetworkStream ns;

    public static void Main()
    {
        TcpClientSample client = new TcpClientSample();
        client.Run();
    }

    public void Run()
    {
        byte[] data = new byte[1024];
        string input;
        int port;

        Console.WriteLine("Please Enter the port number of Server:\n");
        port = Int32.Parse(Console.ReadLine());
        try
        {
            server = new TcpClient("127.0.0.1", port);
        }
        catch (SocketException)
        {
            Console.WriteLine("Unable to connect to server");
            return;
        }
        Console.WriteLine("Connected to the Server...");
        Console.WriteLine("Enter the file path to send to the Server");

        ns = server.GetStream();

        StateObject state = new StateObject();
        state.workSocket = server.Client;
        server.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(OnReceive), state);

        while (true)
        {
            input = Console.ReadLine();
            if (input == "exit")
                break;

            // Send file to server
            SendFile(ns, input);
        }
        Console.WriteLine("Disconnecting from server...");
        ns.Close();
        server.Close();
    }

    public void SendFile(NetworkStream ns, string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
        {
            byte[] fileBuffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = fileStream.Read(fileBuffer, 0, fileBuffer.Length)) > 0)
            {
                ns.Write(fileBuffer, 0, bytesRead);
                ns.Flush();
            }
        }
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
                    // Save the received file
                    using (FileStream fileStream = new FileStream("received_file.txt", FileMode.Append))
                    {
                        fileStream.Write(state.buffer, 0, bytesRead);
                    }

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
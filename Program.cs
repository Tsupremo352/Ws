using dotenv.net;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Ws;
class Ws
{
    static async Task Main(string[] args)
    {
        TcpListener server = new(IPAddress.Any, 6001);
        server.Start();
        Console.WriteLine("Server has started on localhost:6001.\r\nWaiting for a connection...");
        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("A client connected.");
            await Task.Run(() => HandleClient(client));
        }
    }
    private static async Task HandleClient(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (request.Contains("Upgrade: websocket"))
                {
                    await HandshakeWebSocket(client, request);
                }
            }
        }
    }

    private static async Task HandshakeWebSocket(TcpClient client, string request)
    {
        string response = "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n" +
            "Upgrade: websocket\r\n" +
            "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                System.Security.Cryptography.SHA1.Create().ComputeHash(
                    Encoding.UTF8.GetBytes(
                        new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(request).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                    )
                )
            ) + "\r\n\r\n";
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        await client.GetStream().WriteAsync(responseBytes, 0, responseBytes.Length);
        await HandleWebSocket(client);
    }

    private static async Task HandleWebSocket(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;
                byte[] messageBytes = new byte[bytesRead];
                Array.Copy(buffer, messageBytes, bytesRead);
                string message = Encoding.UTF8.GetString(messageBytes);
                Console.WriteLine("Received: " + message);
            }
        }
    }
}

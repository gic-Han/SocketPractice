using System;
using System.Data.Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace SocketPractice;


public class Server
{
    static TcpListener? listener;
    const int Port = 50001;

    public static void ServerOpen()
    {
        listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine("서버가 시작되었습니다. 클라이언트 연결을 기다립니다...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("클라이언트가 연결되었습니다.");
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }
    
    private static void HandleClient(TcpClient client)
    {
        Client _client = new Client(client, 1);

        Console.WriteLine("클라이언트 추가");

        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = _client.StreamRead(buffer)) > 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            if(message == "exit")
            {
                Console.WriteLine("클라이언트 종료");
                BroadcastMessage("client " + _client.id + " has left");
                break;
            }
            Console.WriteLine("클라이언트로부터 메시지 수신: " + message);

            // 받은 메시지를 다른 클라이언트에게 전송
            BroadcastMessage(message);
        }

        _client.ClientClose();
    }

    private static void BroadcastMessage(string message)
    {
        foreach (var connectedClient in ConnectedClients.Clients)
        {
            connectedClient.SendMessage(message);
        }
    }
}

public static class ConnectedClients
{
    public static readonly List<Client> Clients = [];
}

public class Client
{
    public int id;
    private TcpClient client;
    private NetworkStream stream;

    public Client(TcpClient client, int i)
    {
        id = i;
        this.client = client;
        stream = client.GetStream();
        ConnectedClients.Clients.Add(this);
    }

    public int StreamRead(byte[] buffer)
    {
        return stream.Read(buffer, 0, buffer.Length);
    }

    public void ClientClose()
    {
        stream.Close();
        client.Close();
        Console.WriteLine("클라이언트 삭제");
    }

    public void SendMessage(string message)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        stream.Write(bytes, 0, bytes.Length);
    }
}
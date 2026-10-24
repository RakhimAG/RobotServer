using RobotServer;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole;

// SERVER

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

bool clientExists = false;

Socket socket = new
(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp
);

Log.Debug("Server's socket created successfully");

IPEndPoint iPEndPoint = new(IPAddress.Any, 5000);
socket.Bind(iPEndPoint);

Log.Debug("Binded IpEndPoint to server's socket");

socket.Listen();
Log.Debug("Socket turned to listening state");

byte[] commandBuffer = new byte[1024];

var robotStats = new RobotCordStats();

async void SendMessageToClientAsync(Socket client, string message)
{
    var messageBytes = Encoding.UTF8.GetBytes(message);
    Log.Debug("Server is preparing to send a message..");
    try
    {
        await client.SendAsync(messageBytes);
        Log.Debug($"Server sent a message to client succesfully => \"{message}\"");
    }
    catch (ObjectDisposedException)
    {
        clientExists = false;
        Log.Error("Client's socket was disposed in Server, when tried to send a message");
        return;
    }
    catch (SocketException)
    {
        client.Close();
        clientExists = false;
        Log.Error("Client's socket closed the connection, when tried to send a message");
        return;
    }
}

async Task HandleClient(Socket client)
{
    while (clientExists)
    {
        string command;
        try
        {
            var receivedCommand = await client.ReceiveAsync(commandBuffer);
            command = Encoding.UTF8.GetString(commandBuffer, 0, receivedCommand);
            Log.Information($"Received COMMAND => {command}");
        }
        catch (ObjectDisposedException)
        {
            clientExists = false;
            Log.Error("Client's socket was disposed in server, when waited to receive a message");
            break;
        }
        catch (SocketException)
        {
            client.Close();
            clientExists = false;
            Log.Error("Client's socket was closed in client side, when waited to receive a message");
            break;
        }
        
        switch (command)
        {
            case "RIGHT":
                ++robotStats.XCord;
                SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "LEFT":
                --robotStats.XCord;
                SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "UP":
                ++robotStats.YCord;
                SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "DOWN":
                --robotStats.YCord;
                SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "POSITION":
                SendMessageToClientAsync(client, robotStats.ToString());
                break;

            default:
                SendMessageToClientAsync(client, "This command does not exist!");
                Log.Warning("Client sent non-existent command, sending a response-warning to client..");
                break;
        }
    }
}

while (true)
{
    Log.Information("Waiting for new connection...");
    var client = await socket.AcceptAsync();
    clientExists = true;
    Log.Information("New connection was received!");

    Log.Information("Working with the connection...");
    await HandleClient(client);
    Log.Information("Connection was closed.");
}
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
bool isRobotOnline = true;

Log.Debug("Binded IpEndPoint to server's socket");

socket.Listen();
Log.Debug("Socket turned to listening state");

byte[] commandBuffer = new byte[1024];

var robotStats = new RobotCordStats();

async Task SendMessageToClientAsync(Socket client, string message)
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
            var receivedCommandLength = await client.ReceiveAsync(commandBuffer);
            if (receivedCommandLength == 0)
            {
                Log.Warning("Client closed connection gracefully");
                client.Close();
                clientExists = false;
                continue;
            }
            command = Encoding.UTF8.GetString(commandBuffer, 0, receivedCommandLength)
                      .Trim()
                      .ToUpper();

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
            Log.Error("Client crashed, when waited to receive a message");
            break;
        }
        
        switch (command)
        {
            case "RIGHT":
                ++robotStats.XCord;
                await SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "LEFT":
                --robotStats.XCord;
                await SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "UP":
                ++robotStats.YCord;
                await SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "DOWN":
                --robotStats.YCord;
                await SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "POSITION":
                await SendMessageToClientAsync(client, robotStats.ToString());
                break;

            case "EXIT":
                Log.Information("Client requested EXIT command, turning off the robot..");
                await SendMessageToClientAsync(client, "Robot is going offline..");
                isRobotOnline = false;
                clientExists = false;
                break;

            default:
                await SendMessageToClientAsync(client, "This command does not exist!");
                Log.Warning("Client sent non-existent command, sending a response-warning to client..");
                break;
        }
    }
}

Log.Information("Robot is now online!");
while (isRobotOnline)
{
    Log.Information("Waiting for client to join...");
    var client = await socket.AcceptAsync();
    clientExists = true;
    Log.Information("New connection was received!");

    Log.Information("Working with the connection...");
    await HandleClient(client);
}
Log.Information("Robot is now offline.");
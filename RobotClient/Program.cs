
using System.Net;
using System.Net.Sockets;
using System.Text;

bool isRunning = true;

Socket socket = new
(
    AddressFamily.InterNetwork,
    SocketType.Stream,
    ProtocolType.Tcp
);

try
{
    await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000));
    Console.WriteLine("Connected to server successfully");
}
catch (Exception err)
{
    Console.WriteLine(err.Message);
    Console.WriteLine("Programm will close automaticly");
    socket.Close();
    return;
}

var buffer = new byte[1024];
while (isRunning)
{
    Console.Clear();

    Console.WriteLine("COMMANDS : ");
    Console.WriteLine
    (
        " LEFT : X - 1\n RIGHT : X + 1\n UP : Y + 1\n DOWN : Y - 1\n POSITION : CURRENT CORDS\n EXIT : CLOSE PROGRAM"
    );

    var userInput = Console.ReadLine()!.ToUpper();
    
    if (userInput == "EXIT")
    {
        Console.WriteLine("Exiting...");
        socket.Close();
        return;
    }

    var userInputBytes = Encoding.UTF8.GetBytes(userInput);

    try
    {
        await socket.SendAsync(userInputBytes);
    }
    catch (SocketException)
    {
        Console.WriteLine("Server closed connection");
        Console.WriteLine("Programm will close automaticly");
        socket.Close();
        return;
    }


    int receivedSize;
    try
    {
        receivedSize = await socket.ReceiveAsync(buffer);
    }
    catch (SocketException)
    {
        Console.WriteLine("Server closed connection");
        Console.WriteLine("Programm will close automaticly");
        socket.Close();
        return;
    }
    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedSize);
    Console.WriteLine($"[SER] => {receivedMessage}");
    Console.WriteLine("Press any key to continue!");
    Console.ReadKey(true);
}


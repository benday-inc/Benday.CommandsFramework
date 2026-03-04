using System.Net;
using System.Net.Sockets;

namespace Benday.CommandsFramework.CmdUI.Services;

public static class PortFinder
{
    public static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

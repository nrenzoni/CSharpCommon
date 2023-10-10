using System.Net.Sockets;

namespace CustomShared;

public class NetworkUtils
{
    public static bool IsHostAlive(
        string hostUri,
        int portNumber)
    {
        try
        {
            using var _ = new TcpClient(
                hostUri,
                portNumber);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}

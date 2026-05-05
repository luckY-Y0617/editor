using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NS.Framework.Core.Utilities.Network;

/// <summary>
/// IP CIDR 网段检查工具类
/// </summary>
public static class CidrHelper
{
    /// <summary>
    /// 检查 IP 是否在指定的 CIDR 列表中
    /// </summary>
    public static bool IsInCidrs(IPAddress ip, string[] cidrs)
    {
        foreach (var cidr in cidrs.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (TryParseCidr(cidr.Trim(), out var network, out var prefix) && IsInCidr(ip, network, prefix))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查 IP 是否在指定的 CIDR 网段中
    /// </summary>
    public static bool IsInCidr(IPAddress ip, IPAddress network, int prefixLength)
    {
        if (ip.AddressFamily != network.AddressFamily) return false;

        var ipBytes = ip.GetAddressBytes();
        var netBytes = network.GetAddressBytes();

        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (ipBytes[i] != netBytes[i]) return false;
        }

        if (remainingBits == 0) return true;

        var mask = (byte)(~(0xFF >> remainingBits));
        return (ipBytes[fullBytes] & mask) == (netBytes[fullBytes] & mask);
    }

    /// <summary>
    /// 尝试解析 CIDR 格式字符串（如 "192.168.0.0/24"）
    /// </summary>
    public static bool TryParseCidr(string cidr, out IPAddress network, out int prefixLength)
    {
        network = IPAddress.None;
        prefixLength = 0;

        if (string.IsNullOrWhiteSpace(cidr)) return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var parsed) || parsed is null) return false;
        
        network = parsed;
        if (!int.TryParse(parts[1], out prefixLength)) return false;

        var maxPrefix = network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        return prefixLength >= 0 && prefixLength <= maxPrefix;
    }
}


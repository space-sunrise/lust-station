using System.Net;

namespace Content.Shared.Connection.IPBlocking;

public interface IIPBlockingSystem
{
    void Initialize();

    bool IsBlocked(IPAddress ip);

    bool CheckAndBlockSuspiciousLength(IPAddress ip, int length, string context);

    bool CheckAndBlockUnhandledMessageRate(IPAddress ip, string messageType);

    void Update();
}


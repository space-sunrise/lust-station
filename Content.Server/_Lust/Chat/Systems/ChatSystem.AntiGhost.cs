using Content.Server.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Пространство имён расширяемой системы не совпадает с папкой _Lust
namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private static readonly Color AntiGhostColor = Color.FromHex("#F4C1C1");

    public void TrySendAntiGhostMessage(
        EntityUid source,
        string message,
        IConsoleShell shell,
        ICommonSession player)
    {
        if (HasComp<GhostComponent>(source) ||
            !CanSendInGame(message, shell, player) ||
            _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed ||
            player.AttachedEntity != source)
        {
            return;
        }

        if (!TryProcessSunriseChatMessage(source, ref message, oocChatType: InGameOOCChatType.Looc))
            return;

        message = SanitizeInGameOOCMessage(message);
        if (string.IsNullOrEmpty(message))
            return;

        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));
        var localizedMessage = Loc.GetString("chat-manager-entity-antighost-wrap-message",
            ("entityName", name),
            ("message", message));
        var wrappedMessage = $"[color={AntiGhostColor.ToHex()}][italic]{localizedMessage}[/italic][/color]";
        var recipients = new List<INetChannel>();

        foreach (var session in _playerManager.Sessions)
        {
            if (session.AttachedEntity is not { Valid: true } target)
                continue;

            if (TryComp<GhostComponent>(target, out var ghost))
            {
                if (ghost.CanGhostInteract)
                    recipients.Add(session.Channel);

                continue;
            }

            if (_examineSystem.InRangeUnOccluded(source, target, VoiceRange))
                recipients.Add(session.Channel);
        }

        _chatManager.ChatMessageToMany(
            ChatChannel.Emotes,
            message,
            wrappedMessage,
            source,
            false,
            false,
            recipients,
            AntiGhostColor,
            author: player.UserId);

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"Anti-ghost chat from {ToPrettyString(source):Player}: {message}");
    }
}

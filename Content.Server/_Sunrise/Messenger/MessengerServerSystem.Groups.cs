using System.Linq;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared._Sunrise.Messenger;

namespace Content.Server._Sunrise.Messenger;

/// <summary>
/// Часть системы мессенджера, отвечающая за управление группами
/// </summary>
public sealed partial class MessengerServerSystem
{
    private void HandleCreateGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommands.CmdCreateGroup, out NetworkPayload? groupData))
            return;

        if (!groupData.TryGetValue("name", out string? groupName) || string.IsNullOrWhiteSpace(groupName))
            return;

        if (!component.Users.TryGetValue(args.SenderAddress, out var creator))
            return;

        var groupId = $"group_{++component.GroupIdCounter}";
        var members = new HashSet<string> { creator.UserId };

        var group = new MessengerGroup(groupId, groupName, members, MessengerGroupType.UserCreated, null, creator.UserId);
        component.Groups[groupId] = group;

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        var membersList = new List<object>();
        foreach (var memberId in group.Members)
        {
            membersList.Add(memberId);
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdGroupCreated,
            ["group_id"] = group.GroupId,
            ["group_name"] = group.Name,
            ["group_type"] = (int)group.Type,
            ["auto_group_prototype_id"] = group.AutoGroupPrototypeId ?? string.Empty,
            ["owner_id"] = group.OwnerId ?? string.Empty,
            ["members"] = membersList
        };

        if (pdaFrequency.HasValue)
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
        }
        else
        {
            _deviceNetwork.QueuePacket(uid, args.SenderAddress, payload);
        }
    }

    private void HandleAddToGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommands.CmdAddToGroup, out NetworkPayload? addData))
            return;

        if (!addData.TryGetValue("group_id", out string? groupId))
            return;

        if (!addData.TryGetValue("user_id", out string? userId))
            return;

        if (!component.Groups.TryGetValue(groupId, out var group))
            return;

        if (group.Type != MessengerGroupType.UserCreated)
        {
            if (group.AutoGroupPrototypeId != null)
            {
                if (_prototypeManager.TryIndex<MessengerAutoGroupPrototype>(group.AutoGroupPrototypeId, out var autoGroupProto))
                {
                    if (!autoGroupProto.AllowManualMemberManagement)
                        return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (!component.Users.TryGetValue(args.SenderAddress, out var adder))
            return;

        if (group.OwnerId != adder.UserId)
            return;

        if (!component.Users.TryGetValue(userId, out _))
            return;

        if (group.Members.Contains(userId))
            return;

        group.Members.Add(userId);

        if (!component.Users.TryGetValue(userId, out var addedUser))
            return;

        var timestamp = GetStationTime();
        var messageText = Loc.GetString("messenger-system-user-added-by", ("adderName", adder.Name), ("userName", addedUser.Name));
        var messageId = GetNextMessageId(uid, component);
        var systemMessage = new MessengerMessage("system", Loc.GetString("messenger-system-name"), messageText, timestamp, groupId, null, false, messageId);

        if (!component.MessageHistory.TryGetValue(groupId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[groupId] = history;
        }
        history.Add(systemMessage);
        TrimMessageHistory(history, component.MaxMessageHistory);

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var messagePayload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessageReceived,
            ["sender_id"] = "system",
            ["sender_name"] = Loc.GetString("messenger-system-name"),
            ["content"] = messageText,
            ["timestamp"] = timestamp.TotalSeconds,
            ["group_id"] = groupId,
            ["recipient_id"] = string.Empty,
            ["is_read"] = false,
            ["message_id"] = systemMessage.MessageId
        };

        foreach (var memberId in group.Members)
        {
            var isMemberChatOpen = component.OpenChats.TryGetValue(memberId, out var memberOpenChatId) && memberOpenChatId == groupId;

            if (!isMemberChatOpen)
            {
                if (!component.UnreadCounts.TryGetValue(memberId, out var memberUnreads))
                {
                    memberUnreads = new Dictionary<string, int>();
                    component.UnreadCounts[memberId] = memberUnreads;
                }
                memberUnreads.TryGetValue(groupId, out var currentCount);
                memberUnreads[groupId] = currentCount + 1;
            }

            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload);
            }
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdUserAddedToGroup,
            ["group_id"] = groupId,
            ["user_id"] = userId
        };

        foreach (var memberId in group.Members)
        {
            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, payload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, payload);
            }
        }

        if (component.MessageHistory.TryGetValue(groupId, out var groupHistory) && groupHistory.Count > 0)
        {
            var sortedMessages = groupHistory.OrderBy(m => m.Timestamp)
                .ThenBy(m => m.MessageId)
                .ThenBy(m => m.SenderId)
                .ToList();

            var messagesData = new List<Dictionary<string, object>>();
            foreach (var msg in sortedMessages)
            {
                messagesData.Add(new Dictionary<string, object>
                {
                    ["sender_id"] = msg.SenderId,
                    ["sender_name"] = msg.SenderName,
                    ["content"] = msg.Content,
                    ["timestamp"] = msg.Timestamp.TotalSeconds,
                    ["group_id"] = msg.GroupId ?? string.Empty,
                    ["recipient_id"] = msg.RecipientId ?? string.Empty,
                    ["is_read"] = msg.IsRead,
                    ["message_id"] = msg.MessageId
                });
            }

            var historyPayload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessagesList,
                ["messages"] = messagesData,
                ["chat_id"] = groupId
            };

            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, userId, historyPayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, userId, historyPayload);
            }
        }
    }

    private void HandleRemoveFromGroup(EntityUid uid, MessengerServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommands.CmdRemoveFromGroup, out NetworkPayload? removeData))
            return;

        if (!removeData.TryGetValue("group_id", out string? groupId))
            return;

        if (!removeData.TryGetValue("user_id", out string? userId))
            return;

        if (!component.Groups.TryGetValue(groupId, out var group))
            return;

        if (group.Type != MessengerGroupType.UserCreated)
        {
            if (group.AutoGroupPrototypeId != null)
            {
                if (_prototypeManager.TryIndex<MessengerAutoGroupPrototype>(group.AutoGroupPrototypeId, out var autoGroupProto))
                {
                    if (!autoGroupProto.AllowManualMemberManagement)
                        return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (!component.Users.TryGetValue(args.SenderAddress, out var remover))
            return;

        if (group.OwnerId != remover.UserId)
            return;

        if (userId == group.OwnerId)
            return;

        if (!group.Members.Contains(userId))
            return;

        if (!component.Users.TryGetValue(userId, out var removedUser))
            return;

        group.Members.Remove(userId);

        var timestamp = GetStationTime();
        var messageText = Loc.GetString("messenger-system-user-removed-by", ("removerName", remover.Name), ("userName", removedUser.Name));
        var messageId = GetNextMessageId(uid, component);
        var systemMessage = new MessengerMessage("system", Loc.GetString("messenger-system-name"), messageText, timestamp, groupId, null, false, messageId);

        if (!component.MessageHistory.TryGetValue(groupId, out var history))
        {
            history = new List<MessengerMessage>();
            component.MessageHistory[groupId] = history;
        }
        history.Add(systemMessage);
        TrimMessageHistory(history, component.MaxMessageHistory);

        uint? pdaFrequency = null;
        if (_prototypeManager.TryIndex(component.PdaFrequencyId, out var pdaFreq))
        {
            pdaFrequency = pdaFreq.Frequency;
        }

        if (!TryComp<DeviceNetworkComponent>(uid, out var serverDevice))
            return;

        var messagePayload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = MessengerCommands.CmdMessageReceived,
            ["sender_id"] = "system",
            ["sender_name"] = Loc.GetString("messenger-system-name"),
            ["content"] = messageText,
            ["timestamp"] = timestamp.TotalSeconds,
            ["group_id"] = groupId,
            ["recipient_id"] = string.Empty,
            ["is_read"] = false
        };

        foreach (var memberId in group.Members)
        {
            if (pdaFrequency.HasValue)
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload, frequency: pdaFrequency, network: serverDevice.DeviceNetId);
            }
            else
            {
                _deviceNetwork.QueuePacket(uid, memberId, messagePayload);
            }
        }
    }
}

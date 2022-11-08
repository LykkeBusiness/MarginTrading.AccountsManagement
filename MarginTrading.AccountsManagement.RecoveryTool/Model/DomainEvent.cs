// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.AccountsManagement.RecoveryTool.Model;

public class DomainEvent
{
    public string Json { get; set; }
    public EventType Type { get; set; }

    public DomainEvent(string json, EventType type)
    {
        Json = json;
        Type = type;
    }
}
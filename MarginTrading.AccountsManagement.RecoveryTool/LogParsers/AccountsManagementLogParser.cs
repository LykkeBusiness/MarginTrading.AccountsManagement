// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using MarginTrading.AccountsManagement.RecoveryTool.Model;

namespace MarginTrading.AccountsManagement.RecoveryTool.LogParsers

{
    public class AccountsManagementLogParser
    {
        public List<DomainEvent> Parse(string log)
        {
            var result = new List<DomainEvent>();

            var regex = Create("UpdateBalanceCommandsHandler --");
            result.AddRange(Parse(log, regex));

            return result.Where(x => x.Type != EventType.None).ToList();
        }

        private IEnumerable<DomainEvent> Parse(string log, Regex regex)
        {
            return regex.Matches(log)
                .Select(x =>
                {
                    EventType type = EventType.None;
                    if (x.Value.Contains("UpdateBalanceInternalCommand"))
                    {
                        type = EventType.UpdateBalanceInternalCommand;
                    }

                    if (x.Value.Contains("ChangeBalanceCommand"))
                    {
                        type = EventType.ChangeBalanceCommand;
                    }

                    var json = ExtractJson(x.Value);

                    return new DomainEvent(json, type);
                });
        }

        private string ExtractJson(string val)
        {
            var start = val.IndexOf('{');
            var end = val.LastIndexOf('}');
            var json = val.Substring(start, end - start + 1);

            return json;
        }

        private Regex Create(string start)
        {
            var str = $"ChangeBalanceCommand \\[\\{{[\\s\\S]*?\\}}\\]|UpdateBalanceInternalCommand \\[\\{{[\\s\\S]*?\\}}\\]";
            return new Regex(str, RegexOptions.Singleline);
        }
    }
}
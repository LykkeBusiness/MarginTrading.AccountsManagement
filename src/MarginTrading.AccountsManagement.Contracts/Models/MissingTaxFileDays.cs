// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class MissingTaxFileDays
    {
        public List<DateTime> MissingDays { get; set; }
    }
}
// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using FsCheck;

namespace MarginTrading.AccountsManagement.Tests
{
    internal static class Gens
    {
        internal static Gen<int> LessThan(int max) =>
            Gen.Choose(int.MinValue, max - 1);
        
        internal static Gen<int> BetweenInclusive(int min, int max) =>
            Gen.Choose(min, max);
    }
}
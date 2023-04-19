// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using MarginTrading.AccountsManagement.InternalModels;

namespace MarginTrading.AccountsManagement.Extensions
{
    public static class OperationIdExtensions
    {
        private const string PostfixSeparator = "-";
        private const string UpdateBalancePostfix = "update-balance";
        private const string NegativeProtectionPostfix = "negative-protection";

        /// <summary>
        /// Creates operation id from the current one but adds a postfix.
        /// Apart from postfix itself, the separator (1 symbol) is added.
        /// If postfix is already present, the current instance is returned.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="postfix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">When postfix is empty or null</exception> 
        /// <exception cref="ArgumentOutOfRangeException">
        /// When postfix length is greater than <see cref="OperationId.MaxLength"/>
        /// </exception>
        public static OperationId Extend(this OperationId source, string postfix)
        {
            if (string.IsNullOrWhiteSpace(postfix))
                throw new ArgumentNullException(nameof(postfix));

            if (postfix.Length > OperationId.MaxLength)
                throw new ArgumentOutOfRangeException(nameof(postfix),
                    $"The length of {nameof(postfix)} must be less than {OperationId.MaxLength}.");
            
            if (source.Value.EndsWith(postfix, StringComparison.InvariantCultureIgnoreCase))
                return source;
            
            return new OperationId($"{source.Value}{PostfixSeparator}{postfix}");
        }
        
        /// <summary>
        /// Creates new operation id (extends the existing one) with "update-balance" postfix
        /// if it is not already extended
        /// </summary>
        /// <returns></returns>
        public static OperationId ExtendWithUpdateBalance(this OperationId source)
        {
            return source.Extend(UpdateBalancePostfix);
        }
        
        /// <summary>
        /// Creates new operation id (extends the existing one) with "negative-protection" postfix
        /// if it is not already extended
        /// </summary>
        /// <returns></returns>
        public static OperationId ExtendWithNegativeProtection(this OperationId source)
        {
            return source.Extend(NegativeProtectionPostfix);
        }
    }
}
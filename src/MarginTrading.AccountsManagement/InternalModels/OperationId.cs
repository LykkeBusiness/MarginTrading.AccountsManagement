// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace MarginTrading.AccountsManagement.InternalModels
{
    /// <summary>
    /// The operation identifier
    /// </summary>
    public sealed class OperationId : IEquatable<OperationId>
    {
        internal const int MaxLength = 128;
        private const string Separator = "-";
        
        public string Value { get; }
        
        public bool Equals(OperationId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationId) obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }

        public static bool operator ==(OperationId left, OperationId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OperationId left, OperationId right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Creates a new instance of <see cref="OperationId"/> from string
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public OperationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value.Length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"The length of {nameof(value)} must be less than {MaxLength}.");
            
            Value = value;
        }

        /// <summary>
        /// Creates a new instance of <see cref="OperationId"/> with a random guid
        /// </summary>
        public OperationId():this(CreateDigitsGuid())
        {
        }
        
        /// <summary>
        /// Adds a postfix to the operation id. Apart from postfix itself, the separator (1 symbol) is added.
        /// </summary>
        /// <param name="postfix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal OperationId Extend(string postfix)
        {
            if (string.IsNullOrWhiteSpace(postfix))
                throw new ArgumentNullException(nameof(postfix));

            if (postfix.Length > MaxLength)
                throw new ArgumentOutOfRangeException(nameof(postfix),
                    $"The length of {nameof(postfix)} must be less than {MaxLength}.");
            
            if (Value.EndsWith(postfix, StringComparison.InvariantCultureIgnoreCase))
                return this;
            
            return new OperationId($"{Value}{Separator}{postfix}");
        }
        
        private static string CreateDigitsGuid() => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        /// <summary>
        /// Extends the operation id with "update-balance" postfix if it is not already extended
        /// </summary>
        /// <returns></returns>
        public OperationId ExtendWithUpdateBalance()
        {
            return Extend("update-balance");
        }
        
        /// <summary>
        /// Extends the operation id with "negative-protection" postfix if it is not already extended
        /// </summary>
        /// <returns></returns>
        public OperationId ExtendWithNegativeProtection()
        {
            return Extend("negative-protection");
        }
        
        public static implicit operator string(OperationId operationId)
        {
            return operationId.Value;
        }
        
        public static implicit operator OperationId(string operationId)
        {
            return new OperationId(operationId);
        }
    }
}
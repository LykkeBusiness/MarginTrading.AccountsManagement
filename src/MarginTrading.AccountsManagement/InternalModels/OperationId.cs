// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace MarginTrading.AccountsManagement.InternalModels
{
    /// <summary>
    /// The immutable operation identifier with MaxLength = 128
    /// </summary>
    public sealed class OperationId : IEquatable<OperationId>
    {
        internal const int MaxLength = 128;

        internal string Value { get; }
        
        # region IEquatable implementation
        
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
        
        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="OperationId"/> from any string
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException">When value is empty or null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When value length is greater than <see cref="MaxLength"/>
        /// </exception>
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

        private static string CreateDigitsGuid() => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

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
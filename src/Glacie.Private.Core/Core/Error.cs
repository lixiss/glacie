using System;
using System.Globalization;

namespace Glacie
{
    public static class Error
    {
        public static InvalidOperationException Unreachable()
        {
            return new InvalidOperationException("Unreachable.");
        }

        public static NotSupportedException NotSupported()
        {
            return new NotSupportedException();
        }

        public static InvalidOperationException InvalidOperation()
        {
            return new InvalidOperationException();
        }

        public static InvalidOperationException InvalidOperation(string message)
        {
            return new InvalidOperationException(message);
        }

        public static InvalidOperationException InvalidOperation(string format, params object[] args)
        {
            return new InvalidOperationException(Format(format, args));
        }

        public static ArgumentException Argument(string parameterName)
        {
            return new ArgumentException("Invalid value.", parameterName);
        }

        public static ArgumentException Argument(string parameterName, string message)
        {
            return new ArgumentException(message, parameterName);
        }

        public static ArgumentNullException ArgumentNull(string parameterName)
        {
            return new ArgumentNullException(parameterName);
        }

        public static NotImplementedException NotImplemented()
        {
            return new NotImplementedException();
        }

        public static NotImplementedException NotImplemented(string message)
        {
            return new NotImplementedException(message);
        }

        public static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName)
        {
            return new ArgumentOutOfRangeException(parameterName);
        }

        public static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName, string message)
        {
            return new ArgumentOutOfRangeException(parameterName, message);
        }

        public static ArgumentOutOfRangeException ArgumentOutOfRange(string parameterName, string messageFormat, params object[] messageArgs)
        {
            return new ArgumentOutOfRangeException(parameterName, messageFormat.FormatWith(messageArgs));
        }

        public static IndexOutOfRangeException IndexOutOfRange()
        {
            return new IndexOutOfRangeException();
        }

        // TODO: (VeryLow) Add Format helper, and use more overloads / generic parameters.
        private static string Format(string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}

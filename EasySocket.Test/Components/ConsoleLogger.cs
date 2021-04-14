using System;
using EasySocket.Common.Logging;

namespace EasySocket.Test.Components
{
    public class ConsoleLogger : ILogger
    {
        public bool IsDebugEnabled => true;

        public bool IsErrorEnabled => true;

        public bool IsFatalEnabled => true;

        public bool IsInfoEnabled => true;

        public bool IsWarnEnabled => true;

        public void Debug(object message)
        {
            Console.WriteLine(message);
        }

        public void DebugFormat(string format, object arg0)
        {
            Console.WriteLine(format, arg0);

        }

        public void DebugFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            Console.WriteLine(message);
        }

        public void Error(object message, Exception exception)
        {
            Console.WriteLine("Exception : {0} : {1}", message, exception);
        }

        public void ErrorFormat(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void Fatal(object message)
        {
            Console.WriteLine(message);
        }

        public void Fatal(object message, Exception exception)
        {
            Console.WriteLine("Exeption : {0} : {1}", message, exception);
        }

        public void FatalFormat(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public void FatalFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            Console.WriteLine(message);
        }

        public void Info(object message, Exception exception)
        {
            Console.WriteLine("Exception : {0} : {1}", message, exception);
        }

        public void InfoFormat(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public void InfoFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            Console.WriteLine(message);
        }

        public void Warn(object message, Exception exception)
        {
            Console.WriteLine("Exception : {0} : {1}", message, exception);
        }

        public void WarnFormat(string format, object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public void WarnFormat(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }
    }
}
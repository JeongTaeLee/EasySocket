using System;

namespace EasySocket.Logging
{
    public class NLogLogger : ILogger
    {
        private NLog.ILogger _logger = null;

        public NLogLogger(NLog.ILogger logger)
        {
            this._logger = logger;
        }

        public bool IsDebugEnabled => _logger.IsDebugEnabled;

        public bool IsErrorEnabled => _logger.IsErrorEnabled;

        public bool IsFatalEnabled => _logger.IsFatalEnabled;

        public bool IsInfoEnabled => _logger.IsInfoEnabled;

        public bool IsWarnEnabled => _logger.IsWarnEnabled;

        public void Debug(object message)
        {
            _logger.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            _logger.Debug($"message : {message}, exception : {exception}");
        }

        public void DebugFormat(string format, object arg0)
        {
            _logger.Debug(format, arg0);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _logger.Debug(format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Debug(provider, format, args);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            _logger.Debug(format, arg0, arg1);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Debug(format, arg0, arg1, arg2);
        }

        public void Error(object message)
        {
            _logger.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            _logger.Error($"message : {message}, exception : {exception}");
        }

        public void ErrorFormat(string format, object arg0)
        {
            _logger.Error(format, arg0);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Error(provider, format, args);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _logger.Error(format, arg0, arg1);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Error(format, arg0, arg1, arg2) ;
        }

        public void Fatal(object message)
        {
            _logger.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            _logger.Fatal($"message : {message}, exception : {exception}");
        }

        public void FatalFormat(string format, object arg0)
        {
            _logger.Fatal(format, arg0);
        }

        public void FatalFormat(string format, params object[] args)
        {
            _logger.Fatal(format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Fatal(provider, format, args);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            _logger.Fatal(format, arg0, arg1);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Fatal(format, arg0, arg1, arg2);
        }

        public void Info(object message)
        {
            _logger.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            _logger.Info($"message : {message}, exception : {exception}");
        }

        public void InfoFormat(string format, object arg0)
        {
            _logger.Info(format, arg0);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _logger.Info(format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Info(provider, format, args);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            _logger.Info(format, arg0, arg1);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Info(format, arg0, arg1, arg2);
        }

        public void Warn(object message)
        {
            _logger.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _logger.Warn($"message : {message}, exception : {exception}");
        }

        public void WarnFormat(string format, object arg0)
        {
            _logger.Warn(format, arg0);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _logger.Warn(format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Warn(provider, format, args);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            _logger.Warn(format, arg0, arg1);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Warn(format, arg0, arg1, arg2);
        }
    }
}

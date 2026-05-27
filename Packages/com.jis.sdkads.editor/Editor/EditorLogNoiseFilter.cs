#if UNITY_EDITOR
using System;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Suppresses known benign Unity Editor warnings during SDK setup operations.
    /// </summary>
    sealed class EditorLogNoiseFilter : ILogHandler, IDisposable
    {
        readonly ILogHandler _inner;
        readonly bool _previousEnabled;

        public EditorLogNoiseFilter()
        {
            _inner = Debug.unityLogger.logHandler;
            _previousEnabled = Debug.unityLogger.logEnabled;
            Debug.unityLogger.logHandler = this;
        }

        public void Dispose()
        {
            Debug.unityLogger.logHandler = _inner;
            Debug.unityLogger.logEnabled = _previousEnabled;
        }

        public void LogFormat(LogType logType, LogOption logOptions, Object context, string format, params object[] args)
        {
            var message = args != null && args.Length > 0 ? string.Format(format, args) : format;
            if (ShouldSuppress(logType, message))
                return;

            _inner.LogFormat(logType, logOptions, context, format, args);
        }

        public void LogException(Exception exception, Object context) =>
            _inner.LogException(exception, context);

        static bool ShouldSuppress(LogType logType, string message)
        {
            if (logType != LogType.Warning || string.IsNullOrEmpty(message))
                return false;

            return message.Contains("Particle system meshes will only work with exactly one", StringComparison.Ordinal);
        }
    }
}
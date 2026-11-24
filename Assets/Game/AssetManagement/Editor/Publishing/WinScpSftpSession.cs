using System;
using System.IO;
using System.Reflection;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Reflection adapter over WinSCP.Session. Transfers are binary, without resume files and without timestamp changes.</summary>
    internal sealed class WinScpSftpSession : ISftpSession
    {
        private readonly object _session;
        private readonly Type _sessionType;
        private readonly object _transferOptions;
        private readonly Type _transferOptionsType;
        private readonly string _password;

        /// <summary>Wraps an already opened WinSCP session.</summary>
        private WinScpSftpSession(object session, Type transferOptionsType, object transferOptions, string password)
        {
            _session = session;
            _sessionType = session.GetType();
            _transferOptionsType = transferOptionsType;
            _transferOptions = transferOptions;
            _password = password;
        }

        /// <summary>Creates SessionOptions with a mandatory host key fingerprint, opens the session and prepares transfer options.</summary>
        internal static WinScpSftpSession Open(Assembly assembly, string executablePath, bool relaxVersionCheck, SftpConnectionData connection)
        {
            var optionsType = assembly.GetType("WinSCP.SessionOptions", true);
            var protocolType = assembly.GetType("WinSCP.Protocol", true);
            var sessionType = assembly.GetType("WinSCP.Session", true);
            var options = Activator.CreateInstance(optionsType);
            SetProperty(options, "Protocol", Enum.Parse(protocolType, "Sftp"));
            SetProperty(options, "HostName", connection.HostName);
            SetProperty(options, "PortNumber", connection.PortNumber);
            SetProperty(options, "UserName", connection.UserName);
            SetProperty(options, "Password", connection.Password);
            SetProperty(options, "SshHostKeyFingerprint", connection.HostKeyFingerprint);
            SetProperty(options, "Timeout", TimeSpan.FromSeconds(connection.TimeoutSeconds));
            var session = Activator.CreateInstance(sessionType);
            SetProperty(session, "ExecutablePath", executablePath);
            if (relaxVersionCheck)
            {
                SetProperty(session, "DisableVersionCheck", true);
            }
            try
            {
                Invoke(session, "Open", options);
            }
            catch (Exception)
            {
                ((IDisposable)session).Dispose();
                throw;
            }
            var transferOptionsType = assembly.GetType("WinSCP.TransferOptions", true);
            var transferOptions = Activator.CreateInstance(transferOptionsType);
            SetProperty(transferOptions, "TransferMode", Enum.Parse(assembly.GetType("WinSCP.TransferMode", true), "Binary"));
            SetProperty(transferOptions, "PreserveTimestamp", false);
            DisableResume(assembly, transferOptionsType, transferOptions);
            return new WinScpSftpSession(session, transferOptionsType, transferOptions, connection.Password);
        }

        /// <summary>Lists the folder through WinSCP; throws when it cannot be opened.</summary>
        public void ListDirectory(string remotePath)
        {
            Call("ListDirectory", remotePath);
        }

        /// <summary>Returns whether the remote path exists.</summary>
        public bool PathExists(string remotePath)
        {
            return (bool)Call("FileExists", remotePath);
        }

        /// <summary>Creates a remote folder.</summary>
        public void CreateDirectory(string remotePath)
        {
            Call("CreateDirectory", remotePath);
        }

        /// <summary>Uploads one file to an exact remote file name and checks the transfer result.</summary>
        public void UploadFile(string localPath, string remotePath)
        {
            var method = _sessionType.GetMethod("PutFiles", new[] { typeof(string), typeof(string), typeof(bool), _transferOptionsType });
            if (method == null)
            {
                throw new RemoteContentPublishException(PublishStageAlias.Unknown, "Установленная версия WinSCP .NET не поддерживает PutFiles.");
            }
            // WinSCP misparses local paths with forward slashes (it dropped the folder and looked for C:\<file>), so use the native form.
            var nativeLocalPath = Path.GetFullPath(localPath);
            var result = Run(() => method.Invoke(_session, new object[] { nativeLocalPath, remotePath, false, _transferOptions }));
            Run(() => result.GetType().GetMethod("Check").Invoke(result, null));
        }

        /// <summary>Renames a remote file through WinSCP MoveFile.</summary>
        public void MoveFile(string sourcePath, string targetPath)
        {
            if (_sessionType.GetMethod("MoveFile", new[] { typeof(string), typeof(string) }) == null)
            {
                throw new RemoteContentPublishException(PublishStageAlias.Unknown, "Установленная версия WinSCP .NET не поддерживает MoveFile; обновите WinSCP.");
            }
            Call("MoveFile", sourcePath, targetPath);
        }

        /// <summary>Deletes one remote file and checks the removal result.</summary>
        public void DeleteFile(string remotePath)
        {
            var result = Call("RemoveFiles", remotePath);
            Run(() => result.GetType().GetMethod("Check").Invoke(result, null));
        }

        /// <summary>Closes the WinSCP session.</summary>
        public void Dispose()
        {
            ((IDisposable)_session).Dispose();
        }

        /// <summary>Invokes a Session method by name, unwrapping reflection exceptions into redacted publication errors.</summary>
        private object Call(string methodName, params object[] argumentsArray)
        {
            return Run(() => Invoke(_session, methodName, argumentsArray));
        }

        /// <summary>Runs a reflection call and converts failures into a secret-free exception.</summary>
        private object Run(Func<object> action)
        {
            try
            {
                return action();
            }
            catch (TargetInvocationException exception)
            {
                var inner = exception.InnerException ?? exception;
                throw new RemoteContentPublishException(PublishStageAlias.Unknown, SecretRedactor.Redact(inner.Message, _password));
            }
        }

        /// <summary>Invokes a public instance method found by name and argument count.</summary>
        private static object Invoke(object target, string methodName, params object[] argumentsArray)
        {
            foreach (var method in target.GetType().GetMethods())
            {
                if (method.Name == methodName && method.GetParameters().Length == argumentsArray.Length)
                {
                    return method.Invoke(target, argumentsArray);
                }
            }
            throw new MissingMethodException(target.GetType().Name, methodName);
        }

        /// <summary>Sets a public instance property by name.</summary>
        private static void SetProperty(object target, string propertyName, object value)
        {
            target.GetType().GetProperty(propertyName).SetValue(target, value);
        }

        /// <summary>Turns off WinSCP resume (.filepart) files because the publisher uses its own temporary names; ignored when unsupported.</summary>
        private static void DisableResume(Assembly assembly, Type transferOptionsType, object transferOptions)
        {
            try
            {
                var resume = transferOptionsType.GetProperty("ResumeSupport").GetValue(transferOptions);
                var stateType = assembly.GetType("WinSCP.TransferResumeSupportState", true);
                resume.GetType().GetProperty("State").SetValue(resume, Enum.Parse(stateType, "Off"));
            }
            catch (Exception)
            {
                // Older assemblies keep the default; the transfer still goes to the unique temporary name.
            }
        }
    }
}

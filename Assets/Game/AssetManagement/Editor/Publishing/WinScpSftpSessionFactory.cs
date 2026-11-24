using System;
using System.IO;
using System.Reflection;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Opens SFTP sessions through the WinSCP .NET assembly of a local WinSCP installation, loaded by reflection so that no
    /// binaries are added to the project. The password travels to WinSCP over its console pipe, not on a command line.
    /// </summary>
    public sealed class WinScpSftpSessionFactory : ISftpSessionFactory
    {
        /// <summary>Loads WinSCPnet.dll, opens a session with strict host key checking and returns it. Failures carry redacted messages.</summary>
        public ISftpSession Open(SftpConnectionData connection)
        {
            var installation = WinScpInstallationLocator.Locate(connection.WinScpFolder);
            try
            {
                var assembly = Assembly.LoadFrom(installation.AssemblyPath);
                // The runtime reuses an already loaded WinSCPnet of the same identity, so confirm which file was really loaded.
                var loadedFromChosenFile = string.Equals(Path.GetFullPath(assembly.Location), Path.GetFullPath(installation.AssemblyPath), StringComparison.OrdinalIgnoreCase);
                // Only when no dll/exe pair of equal versions exists is the WinSCP version check relaxed; the exe is still set explicitly.
                var relaxVersionCheck = !installation.VersionsMatch || !loadedFromChosenFile;
                return WinScpSftpSession.Open(assembly, installation.ExecutablePath, relaxVersionCheck, connection);
            }
            catch (RemoteContentPublishException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var inner = exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
                throw new RemoteContentPublishException(PublishStageAlias.Connection,
                    "Не удалось открыть SFTP-сессию. Проверьте сервер, порт, логин, пароль и доверенный отпечаток SSH-ключа. " +
                    SecretRedactor.Redact(inner.Message, connection.Password));
            }
        }

    }
}

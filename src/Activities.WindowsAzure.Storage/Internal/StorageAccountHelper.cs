using System.Configuration;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace Hasseware.Activities.WindowsAzure
{
    internal sealed class StorageAccountHelper
    {
        internal const string DefaultStorageConfigurationName = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";

        private static string GetConfigString(string configName)
        {
            // Retrieve from centralized setting storage when running in the cloud.
            // Retrieve from App.config / Web.config when running on-premises.
            return RoleEnvironment.IsAvailable ?
                RoleEnvironment.GetConfigurationSettingValue(configName) :
                ConfigurationManager.AppSettings.Get(configName);
        }

        internal static CloudStorageAccount GetCloudStorageAccount(string configurationName)
        {
            return CloudStorageAccount.Parse(GetConfigString(configurationName));
        }
    }
}

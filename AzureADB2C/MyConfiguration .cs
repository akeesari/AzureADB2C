using Microsoft.Extensions.Configuration;

namespace AzureADB2C
{
    public class MyConfiguration : IMyConfiguration
    {

        IConfigurationRoot _configurationRoot;
        public MyConfiguration(IConfigurationRoot configurationRoot)
        {
            _configurationRoot = configurationRoot;
        }

        public string DBConnection => _configurationRoot["ConnectionStrings:DBconnection"];
        public string Tenant => _configurationRoot["Azureb2cSettings:Tenant"];
        public string ClientID => _configurationRoot["Azureb2cSettings:ClientID"];
        public string ClientSecret => _configurationRoot["Azureb2cSettings:ClientSecret"];
        public string Instance => _configurationRoot["Azureb2cSettings:Instance"];
        public string Endpoint => _configurationRoot["Azureb2cSettings:Endpoint"];
    }


    public interface IMyConfiguration
    {
        string DBConnection { get; }
        string Tenant { get; }
        string ClientID { get; }
        string ClientSecret { get; }
        string Instance { get; }
        string Endpoint { get; }
    }
}

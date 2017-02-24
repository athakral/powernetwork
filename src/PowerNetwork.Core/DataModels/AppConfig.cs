namespace PowerNetwork.Core.DataModels
{
    public class AppConfig
    {
        public string CognitoUserPoolId { get; set; }
        public string CognitoClientId { get; set; }

        public string ConnectionString { get; set; }

        public string DefaultLanguage { get; set; }
    }

}
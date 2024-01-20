namespace SmartCarWebApp.Shared;

#nullable disable annotations

public class Settings
{
    public string CallbackUri { get; set; }
    public string SmartCarClientID { get; set; }
    public string SmartCarClientSecret { get; set; }
    public Email Email { get; set; }
}

public class Email
{
    public string UseSendGridEmailService { get; set; }
    public string UseAzureEmailService { get; set; }
    public string SendToAddress { get; set; }
    public string SendToName { get; set; }
    public string FromAddress { get; set; }
    public string FromName { get; set; }
    public string Subject { get; set; }
    public string SendGridKey { get; set; }
    public string AzureEmailConnectionString { get; set; }
}

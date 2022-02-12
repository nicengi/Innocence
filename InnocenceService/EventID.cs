namespace InnocenceService
{
    public enum EventID
    {
        Error = 16,
        Warning = 32,
        Information = 64,

        OnStart = 1000,
        OnStop,
        OnShutdown,
        OnCustomCommand,

        EncryptStart = 1100,
        Encrypt,
    }
}

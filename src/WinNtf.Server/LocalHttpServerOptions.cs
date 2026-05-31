namespace WinNtf.Server;

public sealed record LocalHttpServerOptions(int Port = 9876, string Host = "127.0.0.1")
{
    public string Url => $"http://{Host}:{Port}";
}

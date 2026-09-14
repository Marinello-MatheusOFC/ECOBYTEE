namespace EcoByte.Web.Firebase;

public class FirebaseConfig
{
    public bool Enabled { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public bool UseEmulator { get; set; }
    public string EmulatorHost { get; set; } = "localhost:8080";
    public string ApiKey { get; set; } = string.Empty;
    public string AuthEmulatorHost { get; set; } = "localhost:9099";
}

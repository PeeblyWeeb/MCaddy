namespace MCaddy.Network;

public enum ConnectionState
{
    Handshake = 0,
    Status = 1,
    Login = 2,
    Transfer = 3,
    Configuration = 4,
    Play = 5
}
namespace MCaddy.Network.Packets;

/// <summary>
/// A human-friendly name for every packet.
///
/// This enum should <b>NOT</b> be used to get the name of a packet by its ID, as it contains duplicate values.
/// </summary>
public enum PacketType
{
    ServerboundHandshake = 0,
    ServerboundStatusRequest = 0,
    ServerboundLoginStart = 0,
    
    ClientboundStatusResponse = 0,
    ClientboundPingResponse = 1,
    
    ServerboundPingRequest = 1,
    ServerboundEncryptionResponse = 1,
    
    ClientboundEncryptionRequest = 1,
    
    ClientboundLoginSuccess = 2,
    
    ClientboundSetCompression = 3,
    ClientboundFinishConfiguration = 3,
}
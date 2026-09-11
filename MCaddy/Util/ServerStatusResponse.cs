namespace MCaddy.Util;

public class ServerStatusResponse
{
    public required VersionResponse Version { get; set; }
    public PlayersResponse? Players { get; set; }
    public DescriptionResponse? Description { get; set; }

    public string? Favicon { get; set; }
    public required bool EnforcesSecureChat { get; set; }

    public class VersionResponse
    {
        public required string Name { get; set; }
        public required int Protocol { get; set; }
    }

    public class PlayersResponse
    {
        public required int Max { get; set; }
        public required int Online { get; set; }
    }

    public class DescriptionResponse
    {
        public required string Text { get; set; }
    }
}
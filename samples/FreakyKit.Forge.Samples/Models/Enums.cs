namespace FreakyKit.Forge.Samples;

// Source enum
public enum PersonStatus
{
    Active,
    Inactive,
    Suspended
}

// Destination enum (same member names, different type)
public enum PersonStatusDto
{
    Active,
    Inactive,
    Suspended
}

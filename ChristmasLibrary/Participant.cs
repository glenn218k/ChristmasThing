namespace ChristmasLibrary;

public record Participant
{
    public static Participant Empty = new() { Name = string.Empty, Id = Guid.Empty, SignificantOtherId = null };

    public string Name { get; init; }
    public Guid Id { get; init; }
    public Guid? SignificantOtherId { get; init; }
}
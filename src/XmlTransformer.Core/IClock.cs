namespace XmlTransformer.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

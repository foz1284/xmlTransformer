namespace XmlTransformer.Core;

public sealed class TransformException : Exception
{
    public TransformException(string message)
        : base(message)
    {
    }

    public TransformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

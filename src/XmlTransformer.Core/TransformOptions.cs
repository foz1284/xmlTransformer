namespace XmlTransformer.Core;

public sealed class TransformOptions
{
    public string SenderPort { get; init; } = "XMLTRANSFORM";
    public string SenderPartnerType { get; init; } = "LS";
    public string SenderPartner { get; init; } = "TCENT";
    public string ReceiverPort { get; init; } = "SAPPORT";
    public string ReceiverPartnerType { get; init; } = "LS";
    public string ReceiverPartner { get; init; } = "SAPCLNT100";
    public string MaterialType { get; init; } = "FERT";
    public string IndustrySector { get; init; } = "M";
    public string MessageFunction { get; init; } = "005";
    public string Language { get; init; } = "E";
    public string LanguageIso { get; init; } = "EN";
    public string DocNumber { get; init; } = "0000000000000001";
    public DateTimeOffset? CreatedAt { get; init; }
}

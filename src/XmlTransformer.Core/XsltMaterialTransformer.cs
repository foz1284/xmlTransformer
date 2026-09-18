using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XmlTransformer.Core;

public sealed class XsltMaterialTransformer
{
    public const string DefaultStylesheetFileName = "TeamcenterToMatmas05.xslt";

    private readonly XslCompiledTransform _transform;
    private readonly IClock _clock;

    public XsltMaterialTransformer(string stylesheetPath, IClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(stylesheetPath))
        {
            throw new ArgumentException("Stylesheet path is required.", nameof(stylesheetPath));
        }

        if (!File.Exists(stylesheetPath))
        {
            throw new TransformException($"Stylesheet not found: {stylesheetPath}");
        }

        _clock = clock ?? SystemClock.Instance;
        _transform = new XslCompiledTransform();
        _transform.Load(stylesheetPath, XsltSettings.Default, new XmlUrlResolver());
    }

    public static string DefaultStylesheetPath =>
        Path.Combine(AppContext.BaseDirectory, "transforms", DefaultStylesheetFileName);

    public string Transform(string inputXml, TransformOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(inputXml))
        {
            throw new TransformException("Input XML is empty.");
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(inputXml, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new TransformException("Input is not valid XML.", ex);
        }

        return Transform(document, options);
    }

    public string Transform(XDocument document, TransformOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new TransformOptions();

        TeamcenterMaterialValidator.ResolveMaterialNumber(document);

        var createdAt = options.CreatedAt ?? _clock.UtcNow;
        var arguments = CreateArguments(options, createdAt);

        var settings = _transform.OutputSettings?.Clone() ?? new XmlWriterSettings();
        settings.Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        settings.CloseOutput = false;

        var writer = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(writer, settings))
        using (var reader = document.CreateReader())
        {
            _transform.Transform(reader, arguments, xmlWriter);
        }

        return writer.ToString();
    }

    public void TransformFile(string inputPath, string outputPath, TransformOptions? options = null)
    {
        if (!File.Exists(inputPath))
        {
            throw new TransformException($"Input file not found: {inputPath}");
        }

        var result = Transform(File.ReadAllText(inputPath), options);
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, result, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static XsltArgumentList CreateArguments(TransformOptions options, DateTimeOffset createdAt)
    {
        var utc = createdAt.ToUniversalTime();
        var arguments = new XsltArgumentList();
        arguments.AddParam("senderPort", string.Empty, options.SenderPort);
        arguments.AddParam("senderPartnerType", string.Empty, options.SenderPartnerType);
        arguments.AddParam("senderPartner", string.Empty, options.SenderPartner);
        arguments.AddParam("receiverPort", string.Empty, options.ReceiverPort);
        arguments.AddParam("receiverPartnerType", string.Empty, options.ReceiverPartnerType);
        arguments.AddParam("receiverPartner", string.Empty, options.ReceiverPartner);
        arguments.AddParam("materialTypeDefault", string.Empty, options.MaterialType);
        arguments.AddParam("industrySectorDefault", string.Empty, options.IndustrySector);
        arguments.AddParam("messageFunction", string.Empty, options.MessageFunction);
        arguments.AddParam("language", string.Empty, options.Language);
        arguments.AddParam("languageIso", string.Empty, options.LanguageIso);
        arguments.AddParam("docNumber", string.Empty, options.DocNumber);
        arguments.AddParam("creationDate", string.Empty, utc.ToString("yyyyMMdd"));
        arguments.AddParam("creationTime", string.Empty, utc.ToString("HHmmss"));
        return arguments;
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}

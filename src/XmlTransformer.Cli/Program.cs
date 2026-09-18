using System.CommandLine;
using XmlTransformer.Core;

namespace XmlTransformer.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        var inputArg = new Argument<FileInfo>("input")
        {
            Description = "Teamcenter material master XML file"
        };

        var outputOption = new Option<FileInfo?>("--output", "-o")
        {
            Description = "MATMAS05 output path. Defaults to <input-name>.matmas.xml next to the source."
        };

        var senderPartnerOption = new Option<string>("--sender-partner")
        {
            Description = "IDoc sender partner number (SNDPRN)",
            DefaultValueFactory = _ => "TCENT"
        };

        var receiverPartnerOption = new Option<string>("--receiver-partner")
        {
            Description = "IDoc receiver partner number (RCVPRN)",
            DefaultValueFactory = _ => "SAPCLNT100"
        };

        var materialTypeOption = new Option<string>("--material-type")
        {
            Description = "Default SAP material type (MTART) when object_type is unknown",
            DefaultValueFactory = _ => "FERT"
        };

        var industrySectorOption = new Option<string>("--industry-sector")
        {
            Description = "Default SAP industry sector (MBRSH) when not present in the source",
            DefaultValueFactory = _ => "M"
        };

        var stylesheetOption = new Option<FileInfo?>("--stylesheet")
        {
            Description = "XSLT stylesheet path. Defaults to transforms/TeamcenterToMatmas05.xslt next to the executable."
        };

        var convert = new Command("convert", "Convert Teamcenter material XML to SAP MATMAS05 IDoc XML")
        {
            inputArg,
            outputOption,
            senderPartnerOption,
            receiverPartnerOption,
            materialTypeOption,
            industrySectorOption,
            stylesheetOption
        };

        convert.SetAction(parseResult =>
        {
            var input = parseResult.GetValue(inputArg)!;
            var output = parseResult.GetValue(outputOption);
            var stylesheet = parseResult.GetValue(stylesheetOption);

            return ConvertFile(
                input,
                output,
                stylesheet,
                parseResult.GetValue(senderPartnerOption)!,
                parseResult.GetValue(receiverPartnerOption)!,
                parseResult.GetValue(materialTypeOption)!,
                parseResult.GetValue(industrySectorOption)!);
        });

        var root = new RootCommand("Converts Teamcenter material master XML into SAP MATMAS05 IDoc XML")
        {
            convert
        };

        return root.Parse(args).Invoke();
    }

    internal static int ConvertFile(
        FileInfo input,
        FileInfo? output,
        FileInfo? stylesheet,
        string senderPartner,
        string receiverPartner,
        string materialType,
        string industrySector)
    {
        try
        {
            if (!input.Exists)
            {
                Console.Error.WriteLine($"Input file not found: {input.FullName}");
                return 1;
            }

            var stylesheetPath = stylesheet?.FullName ?? XsltMaterialTransformer.DefaultStylesheetPath;
            var outputPath = output?.FullName ?? DefaultOutputPath(input.FullName);
            var transformer = new XsltMaterialTransformer(stylesheetPath);
            var options = new TransformOptions
            {
                SenderPartner = senderPartner,
                ReceiverPartner = receiverPartner,
                MaterialType = materialType,
                IndustrySector = industrySector
            };

            transformer.TransformFile(input.FullName, outputPath, options);
            Console.WriteLine(outputPath);
            return 0;
        }
        catch (TransformException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    internal static string DefaultOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{name}.matmas.xml");
    }
}

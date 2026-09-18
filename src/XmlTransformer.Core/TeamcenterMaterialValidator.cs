using System.Xml.Linq;

namespace XmlTransformer.Core;

public static class TeamcenterMaterialValidator
{
    public static readonly XNamespace PlmXml = "http://www.plmxml.org/Schemas/PLMXMLSchema";

    public static string ResolveMaterialNumber(XDocument document)
    {
        if (document.Root is null)
        {
            throw new TransformException("Input XML is empty.");
        }

        var material = document.Descendants(PlmXml + "Product").FirstOrDefault()
            ?? document.Descendants(PlmXml + "Item").FirstOrDefault()
            ?? document.Descendants().FirstOrDefault(e => e.Name.LocalName is "Product" or "Item");

        if (material is null)
        {
            throw new TransformException("Input XML does not contain a Teamcenter Product or Item.");
        }

        var materialNumber = FirstNonEmpty(
            (string?)material.Attribute("productId"),
            (string?)material.Attribute("itemId"));

        if (materialNumber is null)
        {
            throw new TransformException("Material number is missing: Product/@productId or Item/@itemId is required.");
        }

        return materialNumber;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }
        }

        return null;
    }
}

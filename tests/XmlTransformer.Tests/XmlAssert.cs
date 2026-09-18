using System.Xml.Linq;

namespace XmlTransformer.Tests;

internal static class XmlAssert
{
    public static void EqualNormalized(string expectedXml, string actualXml)
    {
        var expected = Normalize(XDocument.Parse(expectedXml));
        var actual = Normalize(XDocument.Parse(actualXml));

        if (XNode.DeepEquals(expected, actual))
        {
            return;
        }

        Assert.Equal(expected.ToString(), actual.ToString());
    }

    public static XElement RequireElement(string xml, string localName)
    {
        return XDocument.Parse(xml).Descendants().First(e => e.Name.LocalName == localName);
    }

    public static string? ChildValue(XElement parent, string localName)
    {
        return parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;
    }

    private static XDocument Normalize(XDocument document)
    {
        foreach (var text in document.DescendantNodes().OfType<XText>().ToList())
        {
            if (string.IsNullOrWhiteSpace(text.Value))
            {
                text.Remove();
            }
            else
            {
                text.Value = text.Value.Trim();
            }
        }

        return document;
    }
}

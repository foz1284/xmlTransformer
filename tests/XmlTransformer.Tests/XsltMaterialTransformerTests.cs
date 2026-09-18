using XmlTransformer.Core;

namespace XmlTransformer.Tests;

public sealed class XsltMaterialTransformerTests
{
    private static readonly DateTimeOffset FrozenTime =
        new(2026, 9, 18, 8, 13, 0, TimeSpan.Zero);

    private static XsltMaterialTransformer CreateTransformer() =>
        new(TestPaths.Stylesheet, new FixedClock(FrozenTime));

    [Fact]
    public void Transform_HappyPath_MatchesGoldenFile()
    {
        var input = File.ReadAllText(TestPaths.Fixture("happy-path-input.xml"));
        var expected = File.ReadAllText(TestPaths.Fixture("happy-path-expected.xml"));

        var actual = CreateTransformer().Transform(input);

        XmlAssert.EqualNormalized(expected, actual);
    }

    [Theory]
    [InlineData("Item", "FERT")]
    [InlineData("Part", "FERT")]
    [InlineData("Material", "ROH")]
    [InlineData("UnknownType", "HALB")]
    public void Transform_MapsObjectTypeToMaterialType(string objectType, string expectedMtart)
    {
        var xml = TeamcenterXml(productId: "MAT-1", objectType: objectType, name: "Widget");
        var options = new TransformOptions
        {
            MaterialType = "HALB",
            CreatedAt = FrozenTime
        };

        var actual = CreateTransformer().Transform(xml, options);
        var e1maram = XmlAssert.RequireElement(actual, "E1MARAM");

        Assert.Equal(expectedMtart, XmlAssert.ChildValue(e1maram, "MTART"));
    }

    [Theory]
    [InlineData("EA", "PCE")]
    [InlineData("each", "PCE")]
    [InlineData("kg", "KGM")]
    [InlineData("L", "L")]
    public void Transform_MapsUnitOfMeasure(string sourceUom, string expectedMeins)
    {
        var xml = TeamcenterXml(productId: "MAT-2", uom: sourceUom, name: "Widget");

        var actual = CreateTransformer().Transform(xml, new TransformOptions { CreatedAt = FrozenTime });
        var e1maram = XmlAssert.RequireElement(actual, "E1MARAM");

        Assert.Equal(expectedMeins, XmlAssert.ChildValue(e1maram, "MEINS"));
    }

    [Fact]
    public void Transform_TruncatesDescriptionToFortyCharacters()
    {
        var longName = new string('A', 55);
        var xml = TeamcenterXml(productId: "MAT-3", name: longName);

        var actual = CreateTransformer().Transform(xml, new TransformOptions { CreatedAt = FrozenTime });
        var e1maktm = XmlAssert.RequireElement(actual, "E1MAKTM");

        Assert.Equal(new string('A', 40), XmlAssert.ChildValue(e1maktm, "MAKTX"));
    }

    [Fact]
    public void Transform_OmitsMaterialGroupWhenAbsent()
    {
        var xml = TeamcenterXml(productId: "MAT-4", name: "No Group", includeMaterialGroup: false);

        var actual = CreateTransformer().Transform(xml, new TransformOptions { CreatedAt = FrozenTime });
        var e1maram = XmlAssert.RequireElement(actual, "E1MARAM");

        Assert.Null(e1maram.Elements().FirstOrDefault(e => e.Name.LocalName == "MATKL"));
    }

    [Fact]
    public void Transform_UsesItemIdWhenProductIdIsAbsent()
    {
        var xml = """
                  <PLMXML xmlns="http://www.plmxml.org/Schemas/PLMXMLSchema">
                    <Item id="item1" name="Bushing" itemId="ITEM-99">
                      <UserData>
                        <UserValue title="object_type" value="Part"/>
                        <UserValue title="uom_tag" value="EA"/>
                      </UserData>
                    </Item>
                  </PLMXML>
                  """;

        var actual = CreateTransformer().Transform(xml, new TransformOptions { CreatedAt = FrozenTime });
        var e1maram = XmlAssert.RequireElement(actual, "E1MARAM");

        Assert.Equal("ITEM-99", XmlAssert.ChildValue(e1maram, "MATNR"));
        Assert.Equal("FERT", XmlAssert.ChildValue(e1maram, "MTART"));
    }

    [Fact]
    public void Transform_MissingMaterialNumber_Throws()
    {
        var xml = """
                  <PLMXML xmlns="http://www.plmxml.org/Schemas/PLMXMLSchema">
                    <Product id="prod1" name="No Number"/>
                  </PLMXML>
                  """;

        var ex = Assert.Throws<TransformException>(() => CreateTransformer().Transform(xml));
        Assert.Contains("Material number is missing", ex.Message);
    }

    [Fact]
    public void Transform_MalformedXml_Throws()
    {
        var ex = Assert.Throws<TransformException>(() => CreateTransformer().Transform("<not-closed>"));
        Assert.Equal("Input is not valid XML.", ex.Message);
    }

    [Fact]
    public void Transform_EmptyInput_Throws()
    {
        var ex = Assert.Throws<TransformException>(() => CreateTransformer().Transform("   "));
        Assert.Equal("Input XML is empty.", ex.Message);
    }

    private static string TeamcenterXml(
        string productId,
        string name = "Widget",
        string objectType = "Item",
        string? uom = "EA",
        bool includeMaterialGroup = true)
    {
        var uomValue = uom is null
            ? string.Empty
            : $"""<UserValue title="uom_tag" value="{uom}"/>""";
        var matkl = includeMaterialGroup
            ? """<UserValue title="material_group" value="001"/>"""
            : string.Empty;

        return $"""
                <PLMXML xmlns="http://www.plmxml.org/Schemas/PLMXMLSchema">
                  <Product id="prod1" name="{name}" productId="{productId}">
                    <UserData>
                      <UserValue title="object_type" value="{objectType}"/>
                      {uomValue}
                      {matkl}
                    </UserData>
                  </Product>
                </PLMXML>
                """;
    }
}

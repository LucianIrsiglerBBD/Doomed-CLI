using System.Text.Json;
using System.Text.Json.Nodes;

namespace DoomedCLI.Utility.Parsers.Tests;

public class JSONHandlerTests
{
    [Fact]
    public void JSONHandler_FormatJSONToString_Should_Return_Error_For_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            JSONHandler.FormatJSONToString(null);
        });

    }

    [Fact]
    public void JSONHandler_FormatJSONToString_Should_Return_Empty_String()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            JSONHandler.FormatJSONToString("");
        });
    }

    [Fact]
    public void JSONHandler_FormatStringToJSONObject_Should_Return_Error_For_Null()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            JSONHandler.FormatStringToJSONObject(null);
        });

    }

    [Fact]
    public void JSONHandler__FormatStringToJSONObject_Should_Return_Empty_String()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            JSONHandler.FormatStringToJSONObject("");
        });
    }

    [Fact]
    public void JSONHandler_FormatJSONToString_Should_Parse_Correctly()
    {
        var json_string = "{\"id\":1,\"title\":\"Fjallraven - Foldsack No. 1 Backpack, Fits 15 Laptops\",\"price\":109.95,\"description\":\"Your perfect pack for everyday use and walks in the forest. Stash your laptop (up to 15 inches) in the padded sleeve, your everyday\",\"category\":\"men's clothing\",\"image\":\"https://fakestoreapi.com/img/81fPKd-2AYL._AC_SL1500_t.png\",\"rating\":{\"rate\":3.9,\"count\":120}}";

        var output = JSONHandler.FormatJSONToString(json_string);

        var expectedNode = JsonNode.Parse(json_string);
        var actualNode = JsonNode.Parse(output);

        Assert.Equal(expectedNode?.ToJsonString(), actualNode?.ToJsonString());
    }

    [Fact]
    public void JSONHandler_FormatStringToJSONObject_Should_Parse_Correctly()
    {
        var json_string = "{\"id\":1,\"title\":\"Fjallraven - Foldsack No. 1 Backpack, Fits 15 Laptops\",\"price\":109.95,\"description\":\"Your perfect pack for everyday use and walks in the forest. Stash your laptop (up to 15 inches) in the padded sleeve, your everyday\",\"category\":\"men's clothing\",\"image\":\"https://fakestoreapi.com/img/81fPKd-2AYL._AC_SL1500_t.png\",\"rating\":{\"rate\":3.9,\"count\":120}}";

        var output = JSONHandler.FormatStringToJSONObject(json_string);
        Assert.NotNull(output);
        Assert.Equal(1, output["id"]!.GetValue<int>());
        Assert.Equal("Fjallraven - Foldsack No. 1 Backpack, Fits 15 Laptops".Trim(), output["title"].GetValue<string>().Trim());
        Assert.Equal(3.9, output["rating"]["rate"].GetValue<double>());
    }

}
using InspectionProcessor.Services;

namespace InspectionProcessor.Tests.Services;

public sealed class InspectionEmailRendererTests
{
    [Fact]
    public void GivenInspectionDataWithHtmlSensitiveValues_WhenRenderingHtml_ThenItEncodesUserVisibleFields()
    {
        var sut = new InspectionEmailRenderer();
        var inspection = new GetInspectionResponse(
            "session-123",
            "user-456",
            "<Inspection>",
            new Dictionary<string, string> { ["equipment"] = "<Furnace>" },
            [new InspectionFileReference("photo <1>.jpg", "session-123", "image/jpeg")]);
        var user = new UserModel("user-456", "user@example.com", "<Jane>", "Doe & Co");

        var html = sut.RenderHtml(inspection, user);

        Assert.Contains("&lt;Inspection&gt;", html);
        Assert.Contains("&lt;Furnace&gt;", html);
        Assert.Contains("&lt;Jane&gt;", html);
        Assert.Contains("Doe &amp; Co", html);
        Assert.Contains("photo &lt;1&gt;.jpg", html);
    }

    [Fact]
    public void GivenMissingOptionalValues_WhenRenderingHtml_ThenItShowsNonePlaceholders()
    {
        var sut = new InspectionEmailRenderer();
        var inspection = new GetInspectionResponse(null, null, null, null, null);

        var html = sut.RenderHtml(inspection, user: null);

        Assert.Contains(">None<", html);
    }
}

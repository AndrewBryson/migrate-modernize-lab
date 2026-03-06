namespace ContosoUniversity.Tests;

[TestFixture]
public class NotificationTests : TestBase
{
    [Test]
    public async Task NotificationDashboard_Loads()
    {
        await Page.GotoAsync("/Notifications");
        await Expect(Page.Locator("body")).ToContainTextAsync("Notification");
    }

    [Test]
    public async Task NotificationApi_ReturnsJson()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/Notifications/GetNotifications");
        Assert.That(response.Ok, Is.True);
        var body = await response.TextAsync();
        // Should return valid JSON (object with success property)
        Assert.That(body, Does.StartWith("{"));
        Assert.That(body, Does.Contain("success"));
    }
}

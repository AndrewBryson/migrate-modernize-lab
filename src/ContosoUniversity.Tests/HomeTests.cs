namespace ContosoUniversity.Tests;

[TestFixture]
public class HomeTests : TestBase
{
    [Test]
    public async Task HomePage_ShowsWelcomeContent()
    {
        await Page.GotoAsync("/");
        await Expect(Page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("Contoso University"));
        await Expect(Page.Locator("text=Welcome to Contoso University")).ToBeVisibleAsync();
    }

    [Test]
    public async Task HomePage_HasNavigationLinks()
    {
        await Page.GotoAsync("/");

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Students" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Courses" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Instructors" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Departments" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task AboutPage_ShowsEnrollmentStatistics()
    {
        await Page.GotoAsync("/Home/About");
        await Expect(Page.Locator("h2", new() { HasText = "Student Body Statistics" })).ToBeVisibleAsync();
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_StudentLinkWorks()
    {
        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Link, new() { Name = "Students" }).First.ClickAsync();
        await Page.WaitForURLAsync("**/Students**");
        await Expect(Page.Locator("h2")).ToBeVisibleAsync();
    }
}

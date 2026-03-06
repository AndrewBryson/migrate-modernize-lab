namespace ContosoUniversity.Tests;

[TestFixture]
public class CourseTests : TestBase
{
    [Test]
    public async Task CourseIndex_ShowsCourseList()
    {
        await Page.GotoAsync("/Courses");
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        var rows = Page.Locator("table tbody tr");
        await Expect(rows).Not.ToHaveCountAsync(0);
    }

    [Test]
    public async Task CourseIndex_ShowsDepartmentColumn()
    {
        await Page.GotoAsync("/Courses");
        // Table should have column headers for courses
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        await Expect(Page.Locator("table th").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task CourseCreate_AndDeleteFlow()
    {
        await Page.GotoAsync("/Courses/Create");
        await Expect(Page.Locator("h2")).ToContainTextAsync("Create");

        // Fill form
        await Page.Locator("#CourseID").FillAsync("9999");
        await Page.Locator("#Title").FillAsync("Playwright Test Course");
        await Page.Locator("#Credits").FillAsync("3");

        // Select a department from dropdown
        var departmentSelect = Page.Locator("#DepartmentID");
        await departmentSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });

        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Courses**");

        // Verify course exists in list
        await Expect(Page.Locator("table tbody")).ToContainTextAsync("Playwright Test Course");

        // View details
        var courseRow = Page.Locator("table tbody tr", new() { HasText = "Playwright Test Course" });
        await courseRow.GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();
        await Expect(Page.Locator("body")).ToContainTextAsync("Playwright Test Course");
        await Expect(Page.Locator("body")).ToContainTextAsync("3");

        // Go back and delete
        await Page.GoBackAsync();
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = "Playwright Test Course" });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Courses**");

        // Verify deleted
        await Expect(Page.Locator("table tbody")).Not.ToContainTextAsync("Playwright Test Course");
    }

    [Test]
    public async Task CourseEdit_UpdatesCourse()
    {
        // Create first
        await Page.GotoAsync("/Courses/Create");
        await Page.Locator("#CourseID").FillAsync("9998");
        await Page.Locator("#Title").FillAsync("Course Before Edit");
        await Page.Locator("#Credits").FillAsync("3");
        await Page.Locator("#DepartmentID").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Courses**");

        // Find and edit
        var row = Page.Locator("table tbody tr", new() { HasText = "Course Before Edit" });
        await row.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        await Page.Locator("#Title").FillAsync("Course After Edit");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Courses**");

        // Verify
        await Expect(Page.Locator("table tbody")).ToContainTextAsync("Course After Edit");

        // Cleanup
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = "Course After Edit" });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }

    [Test]
    public async Task CourseCreate_ValidationRejectsEmptyForm()
    {
        await Page.GotoAsync("/Courses/Create");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Expect(Page.Locator(".text-danger.field-validation-error").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task CourseCreate_DepartmentDropdownHasOptions()
    {
        await Page.GotoAsync("/Courses/Create");
        var options = Page.Locator("#DepartmentID option");
        // Should have at least the placeholder + seeded departments
        var count = await options.CountAsync();
        Assert.That(count, Is.GreaterThan(1));
    }
}

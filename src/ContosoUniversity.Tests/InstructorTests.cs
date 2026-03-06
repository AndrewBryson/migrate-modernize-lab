namespace ContosoUniversity.Tests;

[TestFixture]
public class InstructorTests : TestBase
{
    [Test]
    public async Task InstructorIndex_ShowsInstructorList()
    {
        await Page.GotoAsync("/Instructors");
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        var rows = Page.Locator("table tbody tr");
        await Expect(rows).Not.ToHaveCountAsync(0);
    }

    [Test]
    public async Task InstructorIndex_SelectShowsCourses()
    {
        await Page.GotoAsync("/Instructors");
        // Click Select on the first instructor
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Select" }).ClickAsync();

        // Wait for the page to load with instructor selection
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // A second table with courses should appear
        var tables = Page.Locator("table");
        var tableCount = await tables.CountAsync();
        Assert.That(tableCount, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task InstructorCreate_AndDeleteFlow()
    {
        await Page.GotoAsync("/Instructors/Create");
        await Expect(Page.Locator("h2")).ToContainTextAsync("Create");

        var uniqueName = $"PwInst_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("TestInstr");
        await Page.Locator("#HireDate").FillAsync("2023-06-01");

        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        // Verify in list
        await Expect(Page.Locator("table tbody")).ToContainTextAsync(uniqueName);

        // Details
        var row = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await row.GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();
        await Expect(Page.Locator("body")).ToContainTextAsync(uniqueName);

        // Go back and delete
        await Page.GoBackAsync();
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        // Verify deleted
        await Expect(Page.Locator("table tbody")).Not.ToContainTextAsync(uniqueName);
    }

    [Test]
    public async Task InstructorEdit_UpdatesInstructor()
    {
        // Create
        await Page.GotoAsync("/Instructors/Create");
        var uniqueName = $"EditInst_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("BeforeEdit");
        await Page.Locator("#HireDate").FillAsync("2023-06-01");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        // Edit
        var row = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await row.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        await Page.Locator("#FirstMidName").FillAsync("AfterEdit");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        await Expect(Page.Locator("table tbody")).ToContainTextAsync("AfterEdit");

        // Cleanup
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }

    [Test]
    public async Task InstructorCreate_WithOfficeAssignment()
    {
        await Page.GotoAsync("/Instructors/Create");

        var uniqueName = $"OffInst_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("OfficeTest");
        await Page.Locator("#HireDate").FillAsync("2023-06-01");

        // Fill office location if field exists
        var officeField = Page.Locator("[name='OfficeAssignment.Location']");
        if (await officeField.IsVisibleAsync())
        {
            await officeField.FillAsync("Room 101");
        }

        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        // Cleanup
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }

    [Test]
    public async Task InstructorCreate_WithCourseAssignment()
    {
        await Page.GotoAsync("/Instructors/Create");

        var uniqueName = $"CrsInst_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("CourseTest");
        await Page.Locator("#HireDate").FillAsync("2023-06-01");

        // Check the first course checkbox if available
        var checkboxes = Page.Locator("input[name='selectedCourses']");
        if (await checkboxes.CountAsync() > 0)
        {
            await checkboxes.First.CheckAsync();
        }

        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Instructors**");

        // Cleanup
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }
}

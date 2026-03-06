namespace ContosoUniversity.Tests;

[TestFixture]
public class StudentTests : TestBase
{
    [Test]
    public async Task StudentIndex_ShowsStudentList()
    {
        await Page.GotoAsync("/Students");
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        // Seed data should have students
        var rows = Page.Locator("table tbody tr");
        await Expect(rows).Not.ToHaveCountAsync(0);
    }

    [Test]
    public async Task StudentIndex_SearchFiltersResults()
    {
        await Page.GotoAsync("/Students");
        await Page.Locator("input[name='SearchString']").FillAsync("Alexander");
        await Page.Locator("input[type='submit']").ClickAsync();

        var rows = Page.Locator("table tbody tr");
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(Page.Locator("table tbody")).ToContainTextAsync("Alexander");
    }

    [Test]
    public async Task StudentIndex_SortByName()
    {
        await Page.GotoAsync("/Students");
        // Click Last Name header to sort
        await Page.GetByRole(AriaRole.Link, new() { Name = "Last Name" }).ClickAsync();
        await Page.WaitForURLAsync("**/Students**sortOrder**");
        // Page should still have students
        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task StudentIndex_PaginationExists()
    {
        await Page.GotoAsync("/Students");
        // Pagination buttons should be present
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Next" })
            .Or(Page.Locator("a:has-text('Next')"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task StudentCreate_AndDeleteFlow()
    {
        // Navigate to Create
        await Page.GotoAsync("/Students/Create");
        await Expect(Page.Locator("h2")).ToContainTextAsync("Create");

        // Fill form
        var uniqueName = $"PlaywrightTest_{Guid.NewGuid().ToString()[..8]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("TestFirst");
        await Page.Locator("#EnrollmentDate").FillAsync("2024-01-15");

        // Submit
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Students**");

        // Verify created - search for the student
        await Page.Locator("input[name='SearchString']").FillAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Expect(Page.Locator("table tbody")).ToContainTextAsync(uniqueName);

        // Click Details
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();
        await Expect(Page.Locator("body")).ToContainTextAsync(uniqueName);
        await Expect(Page.Locator("body")).ToContainTextAsync("TestFirst");

        // Go back, find and delete
        await Page.GoBackAsync();
        await Page.Locator("input[name='SearchString']").FillAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();

        // Confirm delete
        await Expect(Page.Locator("body")).ToContainTextAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Students**");

        // Verify deleted
        await Page.Locator("input[name='SearchString']").FillAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task StudentEdit_UpdatesStudent()
    {
        // Create a student first
        await Page.GotoAsync("/Students/Create");
        var uniqueName = $"EditTest_{Guid.NewGuid().ToString()[..8]}";
        await Page.Locator("#LastName").FillAsync(uniqueName);
        await Page.Locator("#FirstMidName").FillAsync("BeforeEdit");
        await Page.Locator("#EnrollmentDate").FillAsync("2024-01-15");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Students**");

        // Find and edit
        await Page.Locator("input[name='SearchString']").FillAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        // Update first name
        await Page.Locator("#FirstMidName").FillAsync("AfterEdit");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Students**");

        // Verify
        await Page.Locator("input[name='SearchString']").FillAsync(uniqueName);
        await Page.Locator("input[type='submit']").ClickAsync();
        await Expect(Page.Locator("table tbody")).ToContainTextAsync("AfterEdit");

        // Cleanup: delete the student
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }

    [Test]
    public async Task StudentCreate_ValidationRejectsEmptyForm()
    {
        await Page.GotoAsync("/Students/Create");
        await Page.Locator("input[type='submit']").ClickAsync();

        // Should stay on the create page with validation errors
        await Expect(Page.Locator(".text-danger.field-validation-error").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task StudentDetails_ShowsEnrollments()
    {
        await Page.GotoAsync("/Students");
        // Click first student's Details link
        await Page.Locator("table tbody tr").First
            .GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();

        await Expect(Page.Locator("h2")).ToContainTextAsync("Details");
        // Details page should show enrollments section
        await Expect(Page.Locator("body")).ToContainTextAsync("Enrollments");
    }
}

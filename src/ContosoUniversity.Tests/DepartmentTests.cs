namespace ContosoUniversity.Tests;

[TestFixture]
public class DepartmentTests : TestBase
{
    [Test]
    public async Task DepartmentIndex_ShowsDepartmentList()
    {
        await Page.GotoAsync("/Departments");
        await Expect(Page.Locator("table")).ToBeVisibleAsync();
        var rows = Page.Locator("table tbody tr");
        await Expect(rows).Not.ToHaveCountAsync(0);
    }

    [Test]
    public async Task DepartmentIndex_ShowsBudgetAndAdministrator()
    {
        await Page.GotoAsync("/Departments");
        await Expect(Page.Locator("table thead")).ToContainTextAsync("Budget");
        await Expect(Page.Locator("table thead")).ToContainTextAsync("Full Name");
    }

    [Test]
    public async Task DepartmentCreate_AndDeleteFlow()
    {
        await Page.GotoAsync("/Departments/Create");
        await Expect(Page.Locator("h2")).ToContainTextAsync("Create");

        var uniqueName = $"PwDept_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#Name").FillAsync(uniqueName);
        await Page.Locator("#Budget").FillAsync("100000");
        await Page.Locator("#StartDate").FillAsync("2024-01-01");

        // Select an instructor as administrator
        var adminSelect = Page.Locator("#InstructorID");
        await adminSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });

        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Departments**");

        // Verify in list
        await Expect(Page.Locator("table tbody")).ToContainTextAsync(uniqueName);

        // Details
        var row = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await row.GetByRole(AriaRole.Link, new() { Name = "Details" }).ClickAsync();
        await Expect(Page.Locator("body")).ToContainTextAsync(uniqueName);
        await Expect(Page.Locator("body")).ToContainTextAsync("100,000");

        // Go back and delete
        await Page.GoBackAsync();
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Departments**");

        // Verify deleted
        await Expect(Page.Locator("table tbody")).Not.ToContainTextAsync(uniqueName);
    }

    [Test]
    public async Task DepartmentEdit_UpdatesDepartment()
    {
        // Create
        await Page.GotoAsync("/Departments/Create");
        var uniqueName = $"EditDept_{Guid.NewGuid().ToString()[..6]}";
        await Page.Locator("#Name").FillAsync(uniqueName);
        await Page.Locator("#Budget").FillAsync("50000");
        await Page.Locator("#StartDate").FillAsync("2024-01-01");
        await Page.Locator("#InstructorID").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Departments**");

        // Edit
        var row = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await row.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();

        await Page.Locator("#Budget").FillAsync("75000");
        await Page.Locator("input[type='submit']").ClickAsync();
        await Page.WaitForURLAsync("**/Departments**");

        // Verify
        await Expect(Page.Locator("table tbody tr", new() { HasText = uniqueName }))
            .ToContainTextAsync("75,000");

        // Cleanup
        var deleteRow = Page.Locator("table tbody tr", new() { HasText = uniqueName });
        await deleteRow.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
        await Page.Locator("input[type='submit']").ClickAsync();
    }

    [Test]
    public async Task DepartmentCreate_InstructorDropdownHasOptions()
    {
        await Page.GotoAsync("/Departments/Create");
        var options = Page.Locator("#InstructorID option");
        var count = await options.CountAsync();
        Assert.That(count, Is.GreaterThan(1));
    }
}

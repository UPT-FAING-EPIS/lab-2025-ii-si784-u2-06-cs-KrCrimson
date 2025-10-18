using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace UPTSiteTests;

[TestClass]
public class UPTSiteTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            RecordVideoDir = "videos",
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        };
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        await Context.Tracing.StartAsync(new()
        {
            Title = $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await Context.Tracing.StopAsync(new()
        {
            Path = Path.Combine(
                Environment.CurrentDirectory,
                "playwright-traces",
                $"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}.zip"
            )
        });
        // await Context.CloseAsync();
    }

    [TestMethod]
    public async Task HasTitle()
    {
        await Page.GotoAsync("https://www.upt.edu.pe");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Universidad"));
    }

    [TestMethod]
    public async Task GetSchoolDirectorName()
    {
        // Arrange
        string schoolDirectorName = "Ing. Martha Judith Paredes Vignola";
        await Page.GotoAsync("https://www.upt.edu.pe");

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Pre-Grado" }).HoverAsync(); //ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Escuela Profesional de Ingeniería de Sistemas" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Escuela Profesional de" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Plana Docente" }).ClickAsync();

        // Assert
        await Expect(Page.GetByText("Ing. Martha Judith Paredes")).ToContainTextAsync(schoolDirectorName);
    } 

    [TestMethod]
    public async Task SearchStudentInDirectoryPage()
    {
        // Arrange
        string studentName = "ARCE BRACAMONTE, SEBASTIAN RODRIGO";
        string studentSearch = studentName.Split(" ")[0];
        await Page.GotoAsync("https://www.upt.edu.pe");

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Pre-Grado" }).HoverAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Escuela Profesional de Ingeniería de Sistemas" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Estudiantes" }).ClickAsync();

        var frame = Page.FrameLocator("iframe");
        await frame.GetByRole(AriaRole.Link, new() { Name = "CICLO - IX", Exact = true }).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).FillAsync(studentSearch);
        await frame.GetByRole(AriaRole.Button, new() { Name = "Buscar" }).ClickAsync();

        // Espera a que la tabla se actualice y contenga el nombre buscado
        await Expect(frame.GetByRole(AriaRole.Table)).ToContainTextAsync(studentName);
    } 

    [TestMethod]
    public async Task SearchNonExistentStudentInDirectoryPage()
    {
        // Arrange
        string studentName = "CHATA CHOQUE, BRANT ANTONY";
        await Page.GotoAsync("https://www.upt.edu.pe");

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Pre-Grado" }).HoverAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Escuela Profesional de Ingeniería de Sistemas" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Estudiantes" }).ClickAsync();

        var frame = Page.FrameLocator("iframe");
        await frame.GetByRole(AriaRole.Link, new() { Name = "CICLO - IX", Exact = true }).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).FillAsync(studentName);
        await frame.GetByRole(AriaRole.Button, new() { Name = "Buscar" }).ClickAsync();

        // Assert: la tabla no debe contener el nombre buscado
        await Expect(frame.GetByRole(AriaRole.Table)).Not.ToContainTextAsync(studentName);
    }

    [TestMethod]
    public async Task SearchEmptyShowsAllStudentsInDirectoryPage()
    {
        // Arrange
        string knownStudent = "	AYMA CHOQUE, ERICK YOEL";
        await Page.GotoAsync("https://www.upt.edu.pe");

        // Act
        await Page.GetByRole(AriaRole.Button, new() { Name = "×" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Pre-Grado" }).HoverAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Escuela Profesional de Ingeniería de Sistemas" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Estudiantes" }).ClickAsync();

        var frame = Page.FrameLocator("iframe");
        await frame.GetByRole(AriaRole.Link, new() { Name = "CICLO - IX", Exact = true }).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).ClickAsync();
        await frame.GetByRole(AriaRole.Textbox).FillAsync("");
        await frame.GetByRole(AriaRole.Button, new() { Name = "Buscar" }).ClickAsync();

        // Assert: la tabla debe contener un estudiante conocido
        await Expect(frame.GetByRole(AriaRole.Table)).ToContainTextAsync(knownStudent);
    }
}
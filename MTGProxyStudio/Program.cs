// HTTP-Kommunikation mit der Scryfall-API, JSON-Verarbeitung und PDF-Erzeugung.
// Die Anwendung verwendet Windows Forms fuer die grafische Oberflaeche.
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MTGProxyStudio;

/// <summary>Startpunkt der Windows-Anwendung.</summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

/// <summary>Hauptfenster mit Karten-Downloader und PDF-Druckbogengenerator.</summary>
internal sealed class MainForm : Form
{
    // Ein gemeinsam verwendeter HTTP-Client fuer Scryfall-Anfragen und Bilddownloads.
    private readonly HttpClient http = new();
    // Eingabefelder und Optionen fuer den Download-Reiter.
    private readonly TextBox deckPath = new() { Dock = DockStyle.Fill };
    private readonly TextBox outputFolder = new() { Dock = DockStyle.Fill };
    private readonly ComboBox language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox selectionMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly CheckBox fallback = new() { Text = "Anderen Druck in gewählter Sprache verwenden", Checked = true, AutoSize = true };
    private readonly CheckBox excludeSpecial = new() { Text = "Showcase, Borderless und Sonderrahmen ausblenden", AutoSize = true };
    private readonly CheckBox includeTokens = new() { Text = "Offizielle Tokens und Embleme ergänzen", Checked = true, AutoSize = true };
    private readonly NumericUpDown tokenCount = new() { Minimum = 1, Maximum = 10, Value = 2, Width = 70 };
    private readonly ComboBox tokenLanguageMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 330 };
    private readonly CheckBox includeBackFaces = new() { Text = "Bei zweiseitigen Karten Vorder- und Rückseite herunterladen", Checked = true, AutoSize = true };
    private readonly ProgressBar downloadProgress = new() { Dock = DockStyle.Fill };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button downloadButton = new() { Text = "Kartenbilder herunterladen", AutoSize = true };

    // Eingabefelder und Optionen fuer den PDF-Reiter.
    private readonly TextBox imageFolder = new() { Dock = DockStyle.Fill };
    private readonly TextBox pdfPath = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown spacingMm = new() { DecimalPlaces = 1, Increment = 0.5M, Minimum = 0, Maximum = 10, Value = 3, Width = 70 };
    private readonly NumericUpDown safeMarginMm = new() { DecimalPlaces = 1, Increment = 0.5M, Minimum = 0, Maximum = 20, Value = 5, Width = 70 };
    private readonly CheckBox cropMarks = new() { Text = "Schnittmarken", Checked = true, AutoSize = true };
    private readonly CheckBox cardNumbers = new() { Text = "Karten nummerieren", Checked = true, AutoSize = true };
    private readonly CheckBox pageNumbers = new() { Text = "Seitenzahlen", Checked = true, AutoSize = true };
    private readonly CheckBox includeSubfolders = new() { Text = "Unterordner einbeziehen (Karten und Tokens)", Checked = true, AutoSize = true };
    private readonly ProgressBar pdfProgress = new() { Dock = DockStyle.Fill };
    private readonly Button pdfButton = new() { Text = "PDF erstellen", AutoSize = true };

    // Zuordnung der sichtbaren Sprachbezeichnungen zu Scryfall-Sprachcodes.
    private readonly Dictionary<string, string> languages = new()
    {
        ["Deutsch"] = "de", ["Englisch"] = "en", ["Französisch"] = "fr", ["Italienisch"] = "it",
        ["Spanisch"] = "es", ["Portugiesisch"] = "pt", ["Japanisch"] = "ja", ["Koreanisch"] = "ko",
        ["Russisch"] = "ru", ["Chinesisch vereinfacht"] = "zhs", ["Chinesisch traditionell"] = "zht"
    };

    /// <summary>Initialisiert Fenster, API-Header, Auswahloptionen und Reiter.</summary>
    public MainForm()
    {
        Text = "MTG Proxy Studio";
        Width = 980;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(860, 650);

        http.DefaultRequestHeaders.UserAgent.ParseAdd("MTGProxyStudio/1.0");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        foreach (var item in languages.Keys) language.Items.Add(item);
        language.SelectedIndex = 0;
        selectionMode.Items.AddRange(new object[]
        {
            "Moxfield-Edition übernehmen",
            "Nur bei Abweichungen nachfragen",
            "Für jede Karte auswählen",
            "Automatisch neuesten regulären Druck wählen"
        });
        selectionMode.SelectedIndex = 1;
        tokenLanguageMode.Items.AddRange(new object[]
        {
            "Gewählte Sprache bevorzugen, Englisch als Fallback",
            "Immer Englisch verwenden",
            "Bei mehreren passenden Tokens nachfragen",
            "Jeden Token manuell auswählen"
        });
        tokenLanguageMode.SelectedIndex = 0;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildDownloaderTab());
        tabs.TabPages.Add(BuildPdfTab());
        Controls.Add(tabs);
    }

    /// <summary>Erstellt die Bedienoberflaeche fuer Deckimport und Bilddownload.</summary>
    private TabPage BuildDownloaderTab()
    {
        var page = new TabPage("Downloader");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 3, RowCount = 12 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddRow(grid, 0, "Moxfield-Exportdatei", deckPath, MakeButton("Auswählen", PickDeck));
        AddRow(grid, 1, "Ausgabeordner", outputFolder, MakeButton("Auswählen", PickOutputFolder));
        AddRow(grid, 2, "Kartensprache", language, new Panel());


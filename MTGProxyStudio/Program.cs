using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MTGProxyStudio;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly HttpClient http = new();
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

    private readonly Dictionary<string, string> languages = new()
    {
        ["Deutsch"] = "de", ["Englisch"] = "en", ["Französisch"] = "fr", ["Italienisch"] = "it",
        ["Spanisch"] = "es", ["Portugiesisch"] = "pt", ["Japanisch"] = "ja", ["Koreanisch"] = "ko",
        ["Russisch"] = "ru", ["Chinesisch vereinfacht"] = "zhs", ["Chinesisch traditionell"] = "zht"
    };

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
        AddRow(grid, 3, "Motiv-/Editionsauswahl", selectionMode, new Panel());
        grid.Controls.Add(fallback, 1, 4); grid.SetColumnSpan(fallback, 2);
        grid.Controls.Add(excludeSpecial, 1, 5); grid.SetColumnSpan(excludeSpecial, 2);
        grid.Controls.Add(includeBackFaces, 1, 6); grid.SetColumnSpan(includeBackFaces, 2);

        var tokenPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        tokenPanel.Controls.Add(includeTokens);
        tokenPanel.Controls.Add(new Label { Text = "Exemplare je Token:", AutoSize = true, Margin = new Padding(20, 6, 3, 0) });
        tokenPanel.Controls.Add(tokenCount);
        grid.Controls.Add(tokenPanel, 1, 7); grid.SetColumnSpan(tokenPanel, 2);
        AddRow(grid, 8, "Tokensprache", tokenLanguageMode, new Panel());
        grid.Controls.Add(downloadProgress, 0, 9); grid.SetColumnSpan(downloadProgress, 3);
        grid.Controls.Add(log, 0, 10); grid.SetColumnSpan(log, 3);
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        btnPanel.Controls.Add(downloadButton);
        grid.Controls.Add(btnPanel, 0, 11); grid.SetColumnSpan(btnPanel, 3);
        downloadButton.Click += async (_, _) => await DownloadAsync();
        page.Controls.Add(grid);
        return page;
    }

    private TabPage BuildPdfTab()
    {
        var page = new TabPage("Druckbogen");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 3, RowCount = 10 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        for (int i = 0; i < 9; i++) grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddRow(grid, 0, "Ordner mit Kartenbildern", imageFolder, MakeButton("Auswählen", PickImageFolder));
        AddRow(grid, 1, "PDF-Ausgabedatei", pdfPath, MakeButton("Auswählen", PickPdfPath));
        AddRow(grid, 2, "Abstand zwischen Karten (mm)", spacingMm, new Panel());
        AddRow(grid, 3, "Sicherheitsrand (mm)", safeMarginMm, new Panel());
        grid.Controls.Add(cropMarks, 1, 4); grid.SetColumnSpan(cropMarks, 2);
        grid.Controls.Add(cardNumbers, 1, 5); grid.SetColumnSpan(cardNumbers, 2);
        grid.Controls.Add(pageNumbers, 1, 6); grid.SetColumnSpan(pageNumbers, 2);
        grid.Controls.Add(includeSubfolders, 1, 7); grid.SetColumnSpan(includeSubfolders, 2);
        grid.Controls.Add(pdfProgress, 0, 8); grid.SetColumnSpan(pdfProgress, 3);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.Controls.Add(new Label
        {
            Text = "A4, 3x3 Raster, 63x88 mm. Das Kartenfeld sowie unvollständige Reihen werden mittig platziert.\r\nBeim Drucken in Acrobat 'Tatsächliche Größe' bzw. 100 % wählen.",
            AutoSize = true,
            Dock = DockStyle.Top
        }, 0, 0);
        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        btnPanel.Controls.Add(pdfButton);
        bottom.Controls.Add(btnPanel, 0, 1);
        grid.Controls.Add(bottom, 0, 9); grid.SetColumnSpan(bottom, 3);
        pdfButton.Click += async (_, _) => await CreatePdfAsync();
        page.Controls.Add(grid);
        return page;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control center, Control right)
    {
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) }, 0, row);
        center.Margin = new Padding(3, 4, 6, 4);
        grid.Controls.Add(center, 1, row);
        right.Margin = new Padding(3, 3, 3, 3);
        grid.Controls.Add(right, 2, row);
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var b = new Button { Text = text, Dock = DockStyle.Fill };
        b.Click += click;
        return b;
    }

    private void PickDeck(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Decklisten|*.txt;*.dek;*.csv|Alle Dateien|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        deckPath.Text = dlg.FileName;
        if (string.IsNullOrWhiteSpace(outputFolder.Text)) outputFolder.Text = Path.Combine(Path.GetDirectoryName(dlg.FileName)!, Path.GetFileNameWithoutExtension(dlg.FileName) + "_Bilder");
    }

    private void PickOutputFolder(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK) outputFolder.Text = dlg.SelectedPath;
    }

    private void PickImageFolder(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        imageFolder.Text = dlg.SelectedPath;
        pdfPath.Text = Path.Combine(dlg.SelectedPath, "Druckbogen_A4_3x3.pdf");
    }

    private void PickPdfPath(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog { Filter = "PDF|*.pdf", DefaultExt = "pdf" };
        if (dlg.ShowDialog() == DialogResult.OK) pdfPath.Text = dlg.FileName;
    }

    private async Task DownloadAsync()
    {
        if (!File.Exists(deckPath.Text)) { MessageBox.Show("Bitte eine gültige Moxfield-Exportdatei auswählen."); return; }
        if (string.IsNullOrWhiteSpace(outputFolder.Text)) { MessageBox.Show("Bitte einen Ausgabeordner auswählen."); return; }

        downloadButton.Enabled = false;
        log.Clear();
        try
        {
            var entries = ParseDeck(deckPath.Text);
            if (entries.Count == 0) throw new InvalidOperationException("Keine Karten erkannt. Beispiel: 1 Sol Ring (CMM) 396");

            Directory.CreateDirectory(outputFolder.Text);
            var cardsDir = Path.Combine(outputFolder.Text, "Karten");
            var tokensDir = Path.Combine(outputFolder.Text, "Tokens");
            Directory.CreateDirectory(cardsDir);
            Directory.CreateDirectory(tokensDir);

            downloadProgress.Minimum = 0;
            downloadProgress.Maximum = entries.Sum(x => x.Quantity);
            downloadProgress.Value = 0;
            var lang = languages[(string)language.SelectedItem!];
            var manifest = new List<ManifestRow>();
            var tokenManifest = new List<TokenManifestRow>();
            var tokenReferences = new Dictionary<string, TokenRequest>(StringComparer.OrdinalIgnoreCase);
            var index = 1;

            foreach (var entry in entries)
            {
                var result = await ResolveCardAsync(entry, lang);
                if (result.Card is null)
                {
                    AppendLog("FEHLT: " + entry.Name);
                    continue;
                }

                var card = result.Card;
                if (selectionMode.SelectedIndex == 2 || (selectionMode.SelectedIndex == 1 && !result.Exact))
                {
                    var prints = await GetLocalizedPrintsAsync(card, lang, excludeSpecial.Checked);
                    var chosen = PrintSelectionDialog.Choose(this, prints, entry.SetCode, entry.CollectorNumber);
                    if (chosen is null) throw new OperationCanceledException("Motivauswahl abgebrochen.");
                    card = chosen;
                }
                else if (selectionMode.SelectedIndex == 3)
                {
                    var prints = await GetLocalizedPrintsAsync(card, lang, true);
                    card = prints.FirstOrDefault() ?? card;
                }

                for (int copy = 1; copy <= entry.Quantity; copy++)
                {
                    var faces = GetFaces(card);
                    for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
                    {
                        if (!includeBackFaces.Checked && faceIndex > 0) break;
                        var face = faces[faceIndex];
                        var suffix = faces.Count > 1 ? (faceIndex == 0 ? "_Vorderseite" : faceIndex == 1 ? "_Rueckseite" : $"_Seite{faceIndex + 1}") : "";
                        var file = Path.Combine(cardsDir, $"{index:000}_{SafeFileName(face.Name)}{suffix}.png");
                        await DownloadFileAsync(face.PngUrl, file);
                    }

                    manifest.Add(new ManifestRow(entry.Name, entry.SetCode, entry.CollectorNumber, card.Name, card.Lang, card.Set, card.CollectorNumber, result.Exact));
                    AppendLog($"{index:000}: {card.PrintedName ?? card.Name}");
                    index++;
                    downloadProgress.Value = Math.Min(downloadProgress.Maximum, downloadProgress.Value + 1);
                }

                if (includeTokens.Checked && card.AllParts is not null)
                {
                    foreach (var part in card.AllParts.Where(p => p.Component == "token" && !string.IsNullOrWhiteSpace(p.Uri)))
                    {
                        var reference = await GetCardByUriAsync(part.Uri!);
                        if (reference is null) continue;
                        var key = BuildTokenKey(reference);
                        if (!tokenReferences.ContainsKey(key))
                            tokenReferences[key] = new TokenRequest(reference, card.Set, card.Name);
                    }
                }
            }

            if (includeTokens.Checked && tokenReferences.Count > 0)
            {
                AppendLog("--- Tokens und Embleme ---");
                int tokenIndex = 1;
                foreach (var request in tokenReferences.Values)
                {
                    var resolution = await ResolveTokenAsync(request.Reference, request.ParentSetCode, lang);
                    var token = resolution.Card;
                    if (token is null)
                    {
                        AppendLog($"TOKEN FEHLT: {request.Reference.Name}");
                        tokenManifest.Add(new TokenManifestRow(request.Reference.Name, request.SourceCardName, lang, "", "", "", "nicht gefunden", false));
                        continue;
                    }

                    var faces = GetFaces(token);
                    for (int copy = 1; copy <= tokenCount.Value; copy++)
                    {
                        for (int faceIndex = 0; faceIndex < faces.Count; faceIndex++)
                        {
                            if (!includeBackFaces.Checked && faceIndex > 0) break;
                            var face = faces[faceIndex];
                            var suffix = faces.Count > 1 ? (faceIndex == 0 ? "_Vorderseite" : faceIndex == 1 ? "_Rueckseite" : $"_Seite{faceIndex + 1}") : "";
                            var file = Path.Combine(tokensDir, $"T{tokenIndex:000}_{copy}x_{SafeFileName(face.Name)}{suffix}.png");
                            await DownloadFileAsync(face.PngUrl, file);
                        }
                    }

                    var fallbackText = resolution.UsedEnglishFallback ? " (englischer Fallback)" : "";
                    AppendLog($"Token: {token.PrintedName ?? token.Name} x{tokenCount.Value}{fallbackText}");
                    tokenManifest.Add(new TokenManifestRow(request.Reference.Name, request.SourceCardName, lang, token.PrintedName ?? token.Name, token.Lang, token.Set, resolution.MatchReason, resolution.UsedEnglishFallback));
                    tokenIndex++;
                }
            }

            await File.WriteAllTextAsync(Path.Combine(outputFolder.Text, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
            var csv = new StringBuilder("OriginalName;RequestedSet;RequestedCollector;DownloadName;Language;UsedSet;UsedCollector;Exact\r\n");
            foreach (var row in manifest) csv.AppendLine(string.Join(';', row.Values().Select(CsvEscape)));
            await File.WriteAllTextAsync(Path.Combine(outputFolder.Text, "manifest.csv"), csv.ToString(), Encoding.UTF8);

            await File.WriteAllTextAsync(Path.Combine(outputFolder.Text, "token_manifest.json"), JsonSerializer.Serialize(tokenManifest, JsonOptions));
            var tokenCsv = new StringBuilder("ReferenceName;SourceCard;RequestedLanguage;DownloadName;UsedLanguage;UsedSet;MatchReason;EnglishFallback\r\n");
            foreach (var row in tokenManifest) tokenCsv.AppendLine(string.Join(';', row.Values().Select(CsvEscape)));
            await File.WriteAllTextAsync(Path.Combine(outputFolder.Text, "token_manifest.csv"), tokenCsv.ToString(), Encoding.UTF8);

            MessageBox.Show("Download abgeschlossen.", "MTG Proxy Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException ex) { MessageBox.Show(ex.Message); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { downloadButton.Enabled = true; }
    }


    private async Task<TokenResolution> ResolveTokenAsync(ScryCard reference, string parentSetCode, string targetLang)
    {
        var mode = tokenLanguageMode.SelectedIndex;
        var desiredLang = mode == 1 ? "en" : targetLang;
        var candidates = await FindTokenCandidatesAsync(reference, parentSetCode, desiredLang);

        if (candidates.Count == 0 && desiredLang != "en")
        {
            var englishCandidates = await FindTokenCandidatesAsync(reference, parentSetCode, "en");
            var english = englishCandidates.FirstOrDefault() ?? reference;
            return new TokenResolution(english, true, "keine Übersetzung vorhanden; englisches Original verwendet");
        }

        if (candidates.Count == 0)
            return new TokenResolution(reference, reference.Lang != targetLang, "verknüpftes Original verwendet");

        var ranked = candidates
            .Select(c => new { Card = c, Score = ScoreTokenCandidate(reference, c, parentSetCode) })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Card.ReleasedAt)
            .Select(x => x.Card)
            .ToList();

        bool ask = mode == 3 || (mode == 2 && ranked.Count > 1);
        if (ask)
        {
            var chosen = PrintSelectionDialog.Choose(this, ranked, "t" + parentSetCode, reference.CollectorNumber);
            if (chosen is null) throw new OperationCanceledException("Token-Auswahl abgebrochen.");
            return new TokenResolution(chosen, chosen.Lang != targetLang, "manuell ausgewählt");
        }

        var selected = ranked[0];
        return new TokenResolution(selected, selected.Lang != targetLang, ranked.Count > 1 ? "beste Übereinstimmung automatisch gewählt" : "eindeutige Übereinstimmung");
    }

    private async Task<List<ScryCard>> FindTokenCandidatesAsync(ScryCard reference, string parentSetCode, string lang)
    {
        var candidates = new List<ScryCard>();

        if (!string.IsNullOrWhiteSpace(reference.OracleId))
            candidates.AddRange(await SearchAsync($"oracleid:{reference.OracleId} lang:{lang} game:paper"));

        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(reference.Name))
        {
            candidates.AddRange(await SearchAsync($"!\"{EscapeSearch(reference.Name)}\" lang:{lang} game:paper"));
            if (candidates.Count == 0)
                candidates.AddRange(await SearchAsync($"name:\"{EscapeSearch(reference.Name)}\" lang:{lang} game:paper"));
        }

        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(reference.TypeLine))
        {
            var layoutQuery = TokenLayoutQuery(reference.Layout);
            var powerQuery = string.IsNullOrWhiteSpace(reference.Power) ? "" : $" pow={reference.Power}";
            var toughnessQuery = string.IsNullOrWhiteSpace(reference.Toughness) ? "" : $" tou={reference.Toughness}";
            candidates.AddRange(await SearchAsync($"lang:{lang} game:paper {layoutQuery}{powerQuery}{toughnessQuery}"));
        }

        return candidates
            .Where(IsTokenLike)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();
    }

    private static int ScoreTokenCandidate(ScryCard reference, ScryCard candidate, string parentSetCode)
    {
        int score = 0;
        if (string.Equals(reference.Name, candidate.Name, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (!string.IsNullOrWhiteSpace(reference.OracleId) && reference.OracleId == candidate.OracleId) score += 120;
        if (Normalize(reference.TypeLine) == Normalize(candidate.TypeLine)) score += 25;
        if (Normalize(reference.OracleText) == Normalize(candidate.OracleText)) score += 30;
        if (reference.Power == candidate.Power) score += 10;
        if (reference.Toughness == candidate.Toughness) score += 10;
        if (NormalizeColors(reference.Colors) == NormalizeColors(candidate.Colors)) score += 15;
        if (reference.Layout == candidate.Layout) score += 10;
        if (candidate.Set.Equals("t" + parentSetCode, StringComparison.OrdinalIgnoreCase)) score += 35;
        if (candidate.Set.StartsWith("t", StringComparison.OrdinalIgnoreCase)) score += 5;
        return score;
    }

    private static bool IsTokenLike(ScryCard card) => card.Layout is "token" or "double_faced_token" or "emblem" or "reversible_card";
    private static string TokenLayoutQuery(string? layout) => layout switch
    {
        "double_faced_token" => "layout:double_faced_token",
        "emblem" => "layout:emblem",
        "reversible_card" => "layout:reversible_card",
        _ => "layout:token"
    };
    private static string EscapeSearch(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string Normalize(string? value) => Regex.Replace(value ?? "", @"\s+", " ").Trim().ToLowerInvariant();
    private static string NormalizeColors(List<string>? colors) => colors is null ? "" : string.Join("", colors.OrderBy(x => x));
    private static string BuildTokenKey(ScryCard card) => string.Join("|", new[]
    {
        Normalize(card.Name), Normalize(card.TypeLine), Normalize(card.OracleText), card.Power ?? "", card.Toughness ?? "", NormalizeColors(card.Colors), card.Layout ?? ""
    });

    private async Task<(ScryCard? Card, bool Exact)> ResolveCardAsync(DeckEntry entry, string lang)
    {
        ScryCard? card = null;
        bool exact = false;
        if (!string.IsNullOrWhiteSpace(entry.SetCode) && !string.IsNullOrWhiteSpace(entry.CollectorNumber))
        {
            card = await GetCardAsync($"https://api.scryfall.com/cards/{entry.SetCode}/{Uri.EscapeDataString(entry.CollectorNumber)}/{lang}");
            exact = card is not null;
            if (card is null)
            {
                var baseCard = await GetCardAsync($"https://api.scryfall.com/cards/{entry.SetCode}/{Uri.EscapeDataString(entry.CollectorNumber)}");
                card = fallback.Checked ? await LocalizeCardAsync(baseCard, lang) : baseCard;
            }
        }
        if (card is null)
        {
            var baseCard = await GetCardAsync("https://api.scryfall.com/cards/named?fuzzy=" + Uri.EscapeDataString(entry.Name));
            card = fallback.Checked ? await LocalizeCardAsync(baseCard, lang) : baseCard;
        }
        return (card, exact);
    }

    private async Task<ScryCard?> LocalizeCardAsync(ScryCard? card, string lang)
    {
        if (card is null || card.Lang == lang || string.IsNullOrWhiteSpace(card.OracleId)) return card;
        var list = await SearchAsync($"oracleid:{card.OracleId} lang:{lang} game:paper");
        return list.FirstOrDefault() ?? card;
    }

    private async Task<List<ScryCard>> GetLocalizedPrintsAsync(ScryCard card, string lang, bool hideSpecial)
    {
        if (string.IsNullOrWhiteSpace(card.OracleId)) return new List<ScryCard> { card };
        var query = $"oracleid:{card.OracleId} lang:{lang} game:paper";
        if (hideSpecial) query += " -frame:showcase -border:borderless";
        var list = await SearchAsync(query);
        return list.Count > 0 ? list : new List<ScryCard> { card };
    }

    private async Task<List<ScryCard>> SearchAsync(string query)
    {
        var results = new List<ScryCard>();
        string? url = "https://api.scryfall.com/cards/search?q=" + Uri.EscapeDataString(query) + "&order=released&dir=desc&unique=prints";
        while (!string.IsNullOrWhiteSpace(url))
        {
            var page = await GetJsonAsync<ScryList>(url);
            if (page is null) break;
            results.AddRange(page.Data);
            url = page.HasMore ? page.NextPage : null;
        }
        return results;
    }

    private async Task<ScryCard?> GetCardByUriAsync(string uri) => await GetCardAsync(uri);

    private async Task<ScryCard?> GetCardAsync(string url)
    {
        try { return await GetJsonAsync<ScryCard>(url); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
    }

    private async Task<T?> GetJsonAsync<T>(string url)
    {
        await Task.Delay(90);
        using var response = await http.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return default;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
    }

    private async Task DownloadFileAsync(string url, string path)
    {
        using var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(path);
        await input.CopyToAsync(output);
    }

    private async Task CreatePdfAsync()
    {
        if (!Directory.Exists(imageFolder.Text)) { MessageBox.Show("Bitte einen gültigen Bilderordner auswählen."); return; }
        if (string.IsNullOrWhiteSpace(pdfPath.Text)) { MessageBox.Show("Bitte eine PDF-Ausgabedatei auswählen."); return; }
        pdfButton.Enabled = false;
        try
        {
            var option = includeSubfolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(imageFolder.Text, "*.*", option)
                .Where(p => new[] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0) throw new InvalidOperationException("Keine PNG- oder JPG-Dateien gefunden.");

            await Task.Run(() => BuildPdf(files));
            MessageBox.Show("PDF wurde erstellt.", "MTG Proxy Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { pdfButton.Enabled = true; }
    }

    private void BuildPdf(List<string> files)
    {
        const double mm = 72.0 / 25.4;
        const double pageW = 210 * mm;
        const double pageH = 297 * mm;
        const double cardW = 63 * mm;
        const double cardH = 88 * mm;
        var gap = (double)spacingMm.Value * mm;
        var safe = (double)safeMarginMm.Value * mm;
        var gridW = 3 * cardW + 2 * gap;
        var gridH = 3 * cardH + 2 * gap;
        if (gridW > pageW - 2 * safe || gridH > pageH - 2 * safe) throw new InvalidOperationException("Raster passt mit diesen Einstellungen nicht auf A4.");

        var document = new PdfDocument();
        document.Info.Title = "MTG Proxy Druckbogen";
        var totalPages = (int)Math.Ceiling(files.Count / 9.0);
        Invoke(() => { pdfProgress.Minimum = 0; pdfProgress.Maximum = files.Count; pdfProgress.Value = 0; });
        int counter = 0;

        for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(pageW);
            page.Height = XUnit.FromPoint(pageH);
            using var gfx = XGraphics.FromPdfPage(page);
            var subset = files.Skip(pageIndex * 9).Take(9).ToList();
            int rows = (int)Math.Ceiling(subset.Count / 3.0);
            var usedH = rows * cardH + Math.Max(0, rows - 1) * gap;
            var startY = (pageH - usedH) / 2;

            for (int row = 0; row < rows; row++)
            {
                var rowFiles = subset.Skip(row * 3).Take(3).ToList();
                int cols = rowFiles.Count;
                var usedW = cols * cardW + Math.Max(0, cols - 1) * gap;
                var startX = (pageW - usedW) / 2;
                for (int col = 0; col < cols; col++)
                {
                    var x = startX + col * (cardW + gap);
                    var y = startY + row * (cardH + gap);
                    using var image = XImage.FromFile(rowFiles[col]);
                    gfx.DrawImage(image, x, y, cardW, cardH);
                    counter++;
                    if (cropMarks.Checked) DrawCropMarks(gfx, x, y, cardW, cardH, mm);
                    if (cardNumbers.Checked)
                    {
                        var font = new XFont("Arial", 6, XFontStyleEx.Regular);
                        gfx.DrawRectangle(XBrushes.White, x + 1.2 * mm, y + cardH - 4.5 * mm, 9 * mm, 3.2 * mm);
                        gfx.DrawString(counter.ToString("000"), font, XBrushes.Black, new XRect(x + 1.2 * mm, y + cardH - 4.4 * mm, 9 * mm, 3 * mm), XStringFormats.Center);
                    }
                    Invoke(() => pdfProgress.Value = Math.Min(pdfProgress.Maximum, counter));
                }
            }

            if (pageNumbers.Checked)
            {
                var font = new XFont("Arial", 8, XFontStyleEx.Regular);
                gfx.DrawString($"Seite {pageIndex + 1} von {totalPages}", font, XBrushes.Black, new XRect(0, pageH - 8 * mm, pageW, 5 * mm), XStringFormats.Center);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath.Text))!);
        document.Save(pdfPath.Text);
    }

    private static void DrawCropMarks(XGraphics gfx, double x, double y, double w, double h, double mm)
    {
        var pen = new XPen(XColors.Black, 0.25);
        var len = 4 * mm; var gap = 1 * mm;
        gfx.DrawLine(pen, x - gap - len, y, x - gap, y); gfx.DrawLine(pen, x, y - gap - len, x, y - gap);
        gfx.DrawLine(pen, x + w + gap, y, x + w + gap + len, y); gfx.DrawLine(pen, x + w, y - gap - len, x + w, y - gap);
        gfx.DrawLine(pen, x - gap - len, y + h, x - gap, y + h); gfx.DrawLine(pen, x, y + h + gap, x, y + h + gap + len);
        gfx.DrawLine(pen, x + w + gap, y + h, x + w + gap + len, y + h); gfx.DrawLine(pen, x + w, y + h + gap, x + w, y + h + gap + len);
    }

    private static List<DeckEntry> ParseDeck(string path)
    {
        var list = new List<DeckEntry>();
        var regex = new Regex(@"^\s*(\d+)\s+(.+?)(?:\s+\(([A-Za-z0-9]+)\)\s+([A-Za-z0-9★*+\-]+))?\s*$");
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//")) continue;
            if (new[] { "deck", "commander", "sideboard", "maybeboard", "considering" }.Contains(line.ToLowerInvariant())) continue;
            var match = regex.Match(line);
            if (!match.Success) continue;
            list.Add(new DeckEntry(int.Parse(match.Groups[1].Value), match.Groups[2].Value.Trim(), match.Groups[3].Value.ToLowerInvariant(), match.Groups[4].Value));
        }
        return list;
    }

    private static List<CardFaceImage> GetFaces(ScryCard card)
    {
        var result = new List<CardFaceImage>();
        if (card.ImageUris?.Png is not null) result.Add(new CardFaceImage(card.PrintedName ?? card.Name, card.ImageUris.Png));
        else if (card.CardFaces is not null)
        {
            foreach (var face in card.CardFaces)
                if (face.ImageUris?.Png is not null) result.Add(new CardFaceImage(face.PrintedName ?? face.Name, face.ImageUris.Png));
        }
        if (result.Count == 0) throw new InvalidOperationException($"Kein PNG-Bild für {card.Name} verfügbar.");
        return result;
    }

    private static string SafeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        value = Regex.Replace(value, @"\s+", " ").Trim();
        return value.Length > 140 ? value[..140] : value;
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(text)); return; }
        log.AppendText(text + Environment.NewLine);
        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }

    private static string CsvEscape(string? value)
    {
        value ??= "";
        return '"' + value.Replace("\"", "\"\"") + '"';
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

internal sealed class PrintSelectionDialog : Form
{
    private readonly ListBox list = new() { Dock = DockStyle.Left, Width = 520 };
    private readonly PictureBox preview = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
    private readonly List<ScryCard> cards;
    private readonly HttpClient http = new();

    private PrintSelectionDialog(List<ScryCard> cards, string setCode, string collector)
    {
        this.cards = cards;
        Text = "Motiv/Edition auswählen";
        Width = 980; Height = 650; StartPosition = FormStartPosition.CenterParent;
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MTGProxyStudio/1.0");

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "Auswahl übernehmen", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        Controls.Add(preview); Controls.Add(list); Controls.Add(buttons);
        AcceptButton = ok; CancelButton = cancel;

        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            var flags = new List<string>();
            if (c.BorderColor == "borderless") flags.Add("Borderless");
            if (c.FrameEffects?.Contains("showcase") == true) flags.Add("Showcase");
            if (c.FullArt) flags.Add("Full Art");
            var extra = flags.Count == 0 ? "" : " [" + string.Join(", ", flags) + "]";
            list.Items.Add($"{c.PrintedName ?? c.Name} - {c.SetName} ({c.Set.ToUpperInvariant()}) #{c.CollectorNumber}, {c.ReleasedAt}{extra}");
            if (c.Set == setCode && c.CollectorNumber == collector) list.SelectedIndex = i;
        }
        if (list.SelectedIndex < 0 && list.Items.Count > 0) list.SelectedIndex = 0;
        list.SelectedIndexChanged += async (_, _) => await UpdatePreviewAsync();
        Shown += async (_, _) => await UpdatePreviewAsync();
    }

    private async Task UpdatePreviewAsync()
    {
        if (list.SelectedIndex < 0) return;
        var card = cards[list.SelectedIndex];
        var url = card.ImageUris?.Normal ?? card.ImageUris?.Large ?? card.ImageUris?.Png ?? card.CardFaces?.FirstOrDefault()?.ImageUris?.Normal;
        if (url is null) return;
        try
        {
            var bytes = await http.GetByteArrayAsync(url);
            using var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            var old = preview.Image;
            preview.Image = new Bitmap(img);
            old?.Dispose();
        }
        catch { }
    }

    public static ScryCard? Choose(IWin32Window owner, List<ScryCard> cards, string setCode, string collector)
    {
        if (cards.Count == 0) return null;
        if (cards.Count == 1) return cards[0];
        using var dlg = new PrintSelectionDialog(cards, setCode, collector);
        return dlg.ShowDialog(owner) == DialogResult.OK && dlg.list.SelectedIndex >= 0 ? cards[dlg.list.SelectedIndex] : null;
    }
}

internal record DeckEntry(int Quantity, string Name, string SetCode, string CollectorNumber);
internal record CardFaceImage(string Name, string PngUrl);
internal record TokenRequest(ScryCard Reference, string ParentSetCode, string SourceCardName);
internal record TokenResolution(ScryCard? Card, bool UsedEnglishFallback, string MatchReason);
internal record ManifestRow(string OriginalName, string RequestedSet, string RequestedCollector, string DownloadName, string Language, string UsedSet, string UsedCollector, bool Exact)
{
    public IEnumerable<string?> Values() => new string?[] { OriginalName, RequestedSet, RequestedCollector, DownloadName, Language, UsedSet, UsedCollector, Exact.ToString() };
}
internal record TokenManifestRow(string ReferenceName, string SourceCard, string RequestedLanguage, string DownloadName, string UsedLanguage, string UsedSet, string MatchReason, bool EnglishFallback)
{
    public IEnumerable<string?> Values() => new string?[] { ReferenceName, SourceCard, RequestedLanguage, DownloadName, UsedLanguage, UsedSet, MatchReason, EnglishFallback.ToString() };
}

internal sealed class ScryList
{
    [JsonPropertyName("data")] public List<ScryCard> Data { get; set; } = new();
    [JsonPropertyName("has_more")] public bool HasMore { get; set; }
    [JsonPropertyName("next_page")] public string? NextPage { get; set; }
}

internal sealed class ScryCard
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("oracle_id")] public string? OracleId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("printed_name")] public string? PrintedName { get; set; }
    [JsonPropertyName("lang")] public string Lang { get; set; } = "";
    [JsonPropertyName("set")] public string Set { get; set; } = "";
    [JsonPropertyName("set_name")] public string SetName { get; set; } = "";
    [JsonPropertyName("collector_number")] public string CollectorNumber { get; set; } = "";
    [JsonPropertyName("released_at")] public string? ReleasedAt { get; set; }
    [JsonPropertyName("border_color")] public string? BorderColor { get; set; }
    [JsonPropertyName("frame_effects")] public List<string>? FrameEffects { get; set; }
    [JsonPropertyName("full_art")] public bool FullArt { get; set; }
    [JsonPropertyName("layout")] public string? Layout { get; set; }
    [JsonPropertyName("type_line")] public string? TypeLine { get; set; }
    [JsonPropertyName("oracle_text")] public string? OracleText { get; set; }
    [JsonPropertyName("power")] public string? Power { get; set; }
    [JsonPropertyName("toughness")] public string? Toughness { get; set; }
    [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
    [JsonPropertyName("image_uris")] public ImageUris? ImageUris { get; set; }
    [JsonPropertyName("card_faces")] public List<ScryFace>? CardFaces { get; set; }
    [JsonPropertyName("all_parts")] public List<ScryPart>? AllParts { get; set; }
}

internal sealed class ScryFace
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("printed_name")] public string? PrintedName { get; set; }
    [JsonPropertyName("image_uris")] public ImageUris? ImageUris { get; set; }
}

internal sealed class ImageUris
{
    [JsonPropertyName("png")] public string? Png { get; set; }
    [JsonPropertyName("normal")] public string? Normal { get; set; }
    [JsonPropertyName("large")] public string? Large { get; set; }
}

internal sealed class ScryPart
{
    [JsonPropertyName("component")] public string? Component { get; set; }
    [JsonPropertyName("uri")] public string? Uri { get; set; }
}

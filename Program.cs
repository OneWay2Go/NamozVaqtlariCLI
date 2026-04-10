using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;
using PuppeteerSharp;

namespace NamozTaqvimCli;

// Dastur holatlari
public enum AppState
{
    ShowingTaqvim,
    ChangingRegion,
    Exiting
}

public class Region
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("differ_minute")] public string? DifferMinute { get; set; }
}

public class AppConfig
{
    public int RegionId { get; set; }
    public string RegionName { get; set; }
    public int DifferMinute { get; set; }
}

class Program
{
    static string configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "namoz-vaqtlari"
    );
    
    private static string ConfigFile = Path.Combine(configPath, "namoz-config.json");
    private static string RegionsFile => Path.Combine(AppContext.BaseDirectory, "regions.json");

    static async Task Main(string[] args)
    {
        if (!Directory.Exists(configPath))
        {
            Directory.CreateDirectory(configPath);
        }
        
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Initial Config
        AppConfig config = LoadConfig();
        AppState currentState = (config == null || args.Contains("--config"))
            ? AppState.ChangingRegion
            : AppState.ShowingTaqvim;

        // STATE MACHINE LOOP
        while (currentState != AppState.Exiting)
        {
            currentState = currentState switch
            {
                AppState.ShowingTaqvim => await HandleShowingTaqvim(config),
                AppState.ChangingRegion => HandleChangingRegion(ref config),
                _ => AppState.Exiting
            };
        }

        AnsiConsole.MarkupLine("[yellow]Dastur tugatildi. Xayr![/]");
    }

    // --- STATE HANDLERS ---

    private static async Task<AppState> HandleShowingTaqvim(AppConfig config)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Islom.uz").Color(Color.Green));

        await FetchAndDisplayPrayerTimes(config);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[yellow]Amalni tanlang:[/]")
                .AddChoices("Yangilash", "Hududni o'zgartirish", "Chiqish"));

        return choice switch
        {
            "Hududni o'zgartirish" => AppState.ChangingRegion,
            "Chiqish" => AppState.Exiting,
            _ => AppState.ShowingTaqvim // "Yangilash" holati
        };
    }

    private static AppState HandleChangingRegion(ref AppConfig config)
    {
        config = PromptUserForRegion();
        SaveConfig(config);
        AnsiConsole.MarkupLine("[green]✓ Hudud muvaffaqiyatli saqlandi![/]");
        return AppState.ShowingTaqvim;
    }

    // --- HELPER METHODS ---

    static AppConfig PromptUserForRegion()
    {
        string path = Path.Combine(AppContext.BaseDirectory, RegionsFile);
        if (!File.Exists(path)) path = RegionsFile;

        if (!File.Exists(path)) throw new FileNotFoundException("regions.json topilmadi!");

        var json = File.ReadAllText(path);
        var regions = JsonSerializer.Deserialize<List<Region>>(json);
        var sorted = regions.OrderBy(r => r.Name).ToList();

        var selectedName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]Hududni tanlang:[/]")
                .PageSize(10)
                .AddChoices(sorted.Select(r => r.Name).ToArray()));

        var selected = sorted.First(r => r.Name == selectedName);
        int diff = 0;
        if (!string.IsNullOrEmpty(selected.DifferMinute))
        {
            int.TryParse(selected.DifferMinute.Replace("+", "").Trim(), out diff);
        }

        return new AppConfig { RegionId = selected.Id, RegionName = selected.Name, DifferMinute = diff };
    }

    static async Task FetchAndDisplayPrayerTimes(AppConfig config)
    {
        var prayerNames = new[] { "Bomdod", "Quyosh", "Peshin", "Asr", "Shom", "Xufton" };
        var times = new List<TimeSpan>();

        await AnsiConsole.Status().StartAsync("[yellow]Yuklanmoqda...[/]", async ctx =>
        {
            try
            {
                var browserFetcher = new BrowserFetcher();
                // This downloads the correct browser for whatever OS you are on (Windows/Linux/macOS)
                var revisionInfo = await browserFetcher.DownloadAsync();
                await using var b = await Puppeteer.LaunchAsync(new LaunchOptions 
                { 
                    Headless = true,
                    ExecutablePath = revisionInfo.GetExecutablePath(),
                    // Linux uchun muhim argumentlar:
                    Args = new[] 
                    { 
                        "--no-sandbox", 
                        "--disable-setuid-sandbox", 
                        "--disable-dev-shm-usage" 
                    } 
                });
                await using var p = await b.NewPageAsync();
                await p.GoToAsync("https://islom.uz/taqvim", WaitUntilNavigation.Networkidle2);
                string html = await p.GetContentAsync();

                var patterns = new[]
                {
                    @"Бомдод.*?(\d{2}:\d{2})", @"Қуёш.*?(\d{2}:\d{2})", @"Пешин.*?(\d{2}:\d{2})",
                    @"Аср.*?(\d{2}:\d{2})", @"Шом.*?(\d{2}:\d{2})", @"Хуфтон.*?(\d{2}:\d{2})"
                };
                foreach (var pat in patterns)
                {
                    var m = Regex.Match(html, pat, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (m.Success)
                        times.Add(TimeSpan.Parse(m.Groups[1].Value).Add(TimeSpan.FromMinutes(config.DifferMinute)));
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex);
            }
        });

        if (times.Count < 6) return;

        var now = DateTime.Now.TimeOfDay;
        int next = times.FindIndex(t => t > now);

        var table = new Table().Border(TableBorder.Rounded).Title($"[green]{config.RegionName} vaqti[/]");
        table.AddColumns("[bold]Namoz[/]", "[bold]Vaqt[/]", "[bold]Holat[/]");

        for (int i = 0; i < 6; i++)
        {
            bool isNext = (i == next);
            bool isCurrent = (i == next - 1 || (next == -1 && i == 5));
            string color = isCurrent ? "green" : (isNext || (next != -1 && i > next) ? "blue" : "grey");
            string status = isCurrent ? "Hozir" : (isNext || (next != -1 && i > next) ? "Kutilmoqda" : "O'tdi");

            table.AddRow($"[{color}]{prayerNames[i]}[/]", $"[{color}]{times[i]:hh\\:mm}[/]", $"[{color}]{status}[/]");
        }

        AnsiConsole.Write(table);

        if (next != -1)
        {
            var left = times[next] - now;
            AnsiConsole.MarkupLine(
                $"\n[yellow]Navbatdagi:[/] [white]{prayerNames[next]}[/] [grey]({times[next]:hh\\:mm})[/]");
            AnsiConsole.MarkupLine($"[yellow]Vaqt qoldi:[/] [bold cyan]{(int)left.TotalHours}s {left.Minutes}m[/]");
        }
    }

    static AppConfig LoadConfig() => File.Exists(ConfigFile)
        ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile))
        : null;

    static void SaveConfig(AppConfig c) => File.WriteAllText(ConfigFile,
        JsonSerializer.Serialize(c, new JsonSerializerOptions { WriteIndented = true }));
}
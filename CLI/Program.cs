using System.CommandLine;
using System.Text;

#region הגדרת האופציות (Options) עם Aliases

var languageOption = new Option<string>(new[] { "--language", "-l" }, "List of languages or 'all'") { IsRequired = true };
var outputOption = new Option<FileInfo>(new[] { "--output", "-o" }, "Output file path and name");
var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Include source file path as a comment");
var sortOption = new Option<string>(new[] { "--sort", "-s" }, () => "name", "Sort by 'name' or 'type'");
var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Remove empty lines from source");
var authorOption = new Option<string>(new[] { "--author", "-a" }, "Name of the author");

#endregion

#region פקודת bundle

var bundleCommand = new Command("bundle", "Bundle code files into a single file");
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(authorOption);

bundleCommand.SetHandler((string lang, FileInfo output, bool note, string sort, bool removeLines, string author) =>
{
    try
    {
        if (output == null)
        {
            Console.WriteLine("Error: Output file is required. Use -o <path>");
            return;
        }

        var currentDir = Directory.GetCurrentDirectory();
        var excludedDirs = new[] { "bin", "debug", "obj", ".git", "node_modules" };

        // שליפת קבצים וסינון תיקיות
        var allFiles = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
            .Where(f => !excludedDirs.Any(d => f.Contains($"{Path.DirectorySeparatorChar}{d}{Path.DirectorySeparatorChar}")));

        // סינון שפות
        if (lang.ToLower() != "all")
        {
            var exts = lang.Split(',').Select(x => "." + x.Trim().ToLower());
            allFiles = allFiles.Where(f => exts.Contains(Path.GetExtension(f).ToLower()));
        }

        // מיון (לפי שם או סוג)
        var sortedFiles = sort.ToLower() == "type"
            ? allFiles.OrderBy(f => Path.GetExtension(f)).ThenBy(f => Path.GetFileName(f))
            : allFiles.OrderBy(f => Path.GetFileName(f));

        using var writer = new StreamWriter(output.FullName);

        if (!string.IsNullOrEmpty(author))
            writer.WriteLine($"// Author: {author}\n");

        foreach (var file in sortedFiles)
        {
            if (file == output.FullName) continue; // מניעת לולאה אם קובץ הפלט באותה תיקייה

            if (note)
            {
                writer.WriteLine($"// Source: {Path.GetRelativePath(currentDir, file)}");
            }

            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (removeLines && string.IsNullOrWhiteSpace(line)) continue;
                writer.WriteLine(line);
            }
            writer.WriteLine();
        }
        Console.WriteLine($"Successfully created bundle: {output.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

#endregion

#region פקודת create-rsp

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");

createRspCommand.SetHandler(() =>
{
    Console.WriteLine("--- Create Response File ---");

    Console.Write("Languages (extensions separated by comma, or 'all'): ");
    var lang = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(lang)) { Console.Write("Required! Languages: "); lang = Console.ReadLine(); }

    Console.Write("Output file path: ");
    var output = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(output)) { Console.Write("Required! Output path: "); output = Console.ReadLine(); }

    Console.Write("Include notes? (y/n): ");
    var note = Console.ReadLine()?.ToLower() == "y" ? "-n" : "";

    Console.Write("Sort by (name/type): ");
    var sort = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(sort)) sort = "name";

    Console.Write("Remove empty lines? (y/n): ");
    var rel = Console.ReadLine()?.ToLower() == "y" ? "-r" : "";

    Console.Write("Author name (optional): ");
    var author = Console.ReadLine();
    var authorArg = !string.IsNullOrEmpty(author) ? $"-a \"{author}\"" : "";

    // יצירת הפקודה המלאה
    var fullCommand = $"bundle -l {lang} -o \"{output}\" {note} -s {sort} {rel} {authorArg}";

    try
    {
        File.WriteAllText("bundle_config.rsp", fullCommand);
        Console.WriteLine("\nSuccess: 'bundle_config.rsp' created.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});

#endregion

var rootCommand = new RootCommand("CLI for bundling code");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
await rootCommand.InvokeAsync(args);
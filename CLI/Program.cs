using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileMergerCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // הגדרת פקודת השורש (Root Command)
            var rootCommand = new RootCommand("כלי לאיחוד כל קבצי הטקסט בתיקייה לקובץ אחד");

            // הגדרת אופציה אופציונלית לשם קובץ הפלט (דיפולט: combined.txt)
            var outputOption = new Option<string>(
                name: "--output",
                description: "שם קובץ היעד",
                getDefaultValue: () => "combined_files.txt");

            rootCommand.AddOption(outputOption);

            // הגדרת הפעולה שתתבצע בעת הרצת הפקודה
            rootCommand.SetHandler((string outputName) =>
            {
                MergeFiles(outputName);
            }, outputOption);

            // הרצת הפקודה עם הארגומנטים שהתקבלו
            return await rootCommand.InvokeAsync(args);
        }

        static void MergeFiles(string outputFileName)
        {
            // קבלת הנתיב הנוכחי שבו המשתמש נמצא
            string currentDirectory = Directory.GetCurrentDirectory();
            string outputPath = Path.Combine(currentDirectory, outputFileName);

            try
            {
                string[] files = Directory.GetFiles(currentDirectory);
                int count = 0;

                using (var writer = new StreamWriter(outputPath, false, Encoding.UTF8))
                {
                    foreach (var filePath in files)
                    {
                        string fileName = Path.GetFileName(filePath);

                        // מניעת איחוד קובץ הפלט לתוך עצמו או קבצי מערכת
                        if (fileName == outputFileName || fileName.EndsWith(".exe") || fileName.EndsWith(".dll") || fileName.EndsWith(".pdb"))
                        {
                            continue;
                        }

                        Console.WriteLine($"מאחד את: {fileName}...");

                        writer.WriteLine($"// --- START OF FILE: {fileName} ---");
                        writer.WriteLine(File.ReadAllText(filePath));
                        writer.WriteLine($"// --- END OF FILE: {fileName} ---");
                        writer.WriteLine();

                        count++;
                    }
                }

                Console.WriteLine($"\nהסתיים בהצלחה! {count} קבצים אוחדו לתוך '{outputFileName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"שגיאה קריטית: {ex.Message}");
            }
        }
    }
}
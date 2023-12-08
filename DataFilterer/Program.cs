using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFilterer;

public static class Program
{
    private static readonly DirectoryInfo dataPath = new($"{Directory.GetCurrentDirectory()}/../../../../data");
    private static readonly string editorLoc = @"C:\Program Files\Notepad++\notepad++.exe";
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static void Main(string[] args)
    {
        //ensure console output can display Korean
        Console.OutputEncoding = Encoding.UTF8;

        //run async method
        Task.Run(MergeFiles).Wait();
    }

    public static async Task MergeFiles()
    {
        //acquire list of file paths
        var files = dataPath.GetFiles();

        //temp storage for articles
        ConcurrentBag<Article> articles = [];

        //do for all articles
        await Parallel.ForEachAsync(files, async (filePath, token) =>
        {
            //open article data file
            using FileStream fs = new(filePath.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //deserialize it
            ArticlePair? data = await JsonSerializer.DeserializeAsync<ArticlePair>(fs, cancellationToken: token);

            //save in article bag if it succeeds
            if (data != null)
            {
                //add original human written article
                articles.Add(new Article(false, data.Original));

                //add paraphrased machine written article
                articles.Add(new Article(true, data.Paraphrased));
            }
        });

        //open filestream
        using FileStream fs = new(Path.Join(dataPath.FullName, "..", "data.json"), FileMode.Create, FileAccess.ReadWrite, FileShare.Read);

        //write combined article data as file
        await JsonSerializer.SerializeAsync(fs, articles, jsonOptions);
    }

    /// <summary>
    /// Loop through all data files
    /// </summary>
    public static async Task CheckFiles()
    {
        //acquire enumerable of files, sorted by time in descending order
        var files = dataPath.GetFiles();

        //auto remove special cases
        await Parallel.ForEachAsync(files, async (filePath, token) =>
        {
            //open filestream
            using FileStream fs = new(filePath.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //deserialize json file
            var data = await JsonSerializer.DeserializeAsync<ArticlePair>(fs, cancellationToken: token) ?? new();

            //special auto delete cases
            if (data.Paraphrased.Contains("translat", StringComparison.OrdinalIgnoreCase) ||
                data.Paraphrased.Contains("paraphrase", StringComparison.OrdinalIgnoreCase) ||
                data.Paraphrased.Contains("in korean", StringComparison.OrdinalIgnoreCase) ||
                data.Original.Length * 0.75 > data.Paraphrased.Length ||
                data.Paraphrased.Length > data.Original.Length * 1.5)
            {
                //clear console first
                Console.Clear();

                //close filestream
                fs.Close();

                //delete file
                filePath.Delete();

                //decrement number of articles
            }
        });

        //refresh filelist
        var fileList = (from f in dataPath.GetFiles()
                        orderby f.Length descending
                        select f).ToList();

        //temp variables
        //number of files
        int numData = fileList.Count;

        //progress string
        string progress;

        for (int i = 0; i < numData; i++)
        {
            var filePath = fileList[i];

            //open filestream
            using FileStream fs = new(filePath.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            //deserialize json file
            var data = await JsonSerializer.DeserializeAsync<ArticlePair>(fs) ?? new();

            //output filename
            Console.Write($"FILENAME: {filePath.Name}");

            //generate progress string
            progress = $"{i} / {numData} {i * 100.0f / numData:0.###}%";

            //output progres string right aligned
            Console.CursorLeft = Console.BufferWidth - progress.Length;
            Console.WriteLine(progress);

            //output paraphrased data
            Console.WriteLine(data.Paraphrased);

            //acquire console key input
            switch (Console.ReadKey().Key)
            {
                //delete file on Y
                case ConsoleKey.Y:
                case ConsoleKey.Delete:

                    //clear console first
                    Console.Clear();

                    //output file deleted
                    Console.WriteLine($"DELETED {filePath.Name}\n");

                    //remove file from filelist
                    fileList.RemoveAt(i);

                    //close filestream
                    fs.Close();

                    //delete file
                    filePath.Delete();

                    //decrement number of articles
                    numData--;
                    break;

                //edit file
                case ConsoleKey.E:
                case ConsoleKey.Insert:

                    //open file with given editor location
                    Process.Start(editorLoc, $"\"{filePath.FullName}\"");
                    Console.Clear();
                    break;

                //go back
                case ConsoleKey.LeftArrow:

                    Console.Clear();
                    i -= 2;
                    break;

                //go forward fast
                case ConsoleKey.RightArrow:

                    Console.Clear();
                    i += 9;
                    break;

                //go forward faster
                case ConsoleKey.PageDown:

                    Console.Clear();
                    i += 99;
                    break;

                //just clear console by default
                default:
                    Console.Clear();
                    break;
            }
        }
    }
}

public class ArticlePair
{
    public string ID { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string Paraphrased { get; set; } = string.Empty;
}

public class Article
{
    [JsonPropertyName("label")]
    public int IsAI { get; set; }

    [JsonPropertyName("text")]
    public string Body { get; set; }

    public Article(bool isAI, string body)
    {
        IsAI = isAI ? 1 : 0;
        Body = body;
    }
}
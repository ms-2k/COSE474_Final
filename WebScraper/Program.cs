using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text;
using System.Text.RegularExpressions;

namespace WebScraper;

public partial class Program
{
    //llm api server uri
    private static readonly Uri LlmServer = new("http://localhost:5000/v1/completions");

    //current project directory
    private static readonly DirectoryInfo projectPath = new($"{Directory.GetCurrentDirectory()}/../../..");

    //article fetcher object to use
    private static readonly ArticleFetcher articles = new(projectPath);

    public static void Main(string[] args)
    {
        //setup console
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Console.OutputEncoding = Encoding.Unicode;
        Console.CancelKeyPress += new ConsoleCancelEventHandler(KeyboardInterruptHander);

        //create logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Join(projectPath.FullName, "logs", "log.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Task.Run(ParaphraserLoop).Wait();
    }

    /// <summary>
    /// Continuously acquires random articles and sends a paraphrase request to LLM API
    /// </summary>
    public static async Task ParaphraserLoop()
    {
        //initialize article fetcher
        await articles.InitializeAsync();

        ArticlePair article;

        //loop forever (until interrupt)
        while (true)
        {
            //attempt to acquire paraphrased article
            try
            {
                //acquire random article
                article = new(await articles.AcquireRandomArticle());

                //skip if article body is empty
                if (article.Original.Length < 5)
                    throw new InvalidResponseException("Invalid article length!");

                //parse article json, extract article body
                dynamic json = JObject.Parse(

                    //send API request to LLM
                    await LlmApiRequest(
                        $"Paraphrase the following article in Korean in a journalist style. No paraphrased sentence should exactly match the given text. Do not translate the article.\\n\\nText:\\n{article.Original.Replace("\n", "\\n")}\\n\\nParaphrased article in Korean:\\n",
                        (int)(article.Original.Length * 1.1)
                    )
                );

                //acquire paraphrased article (not enough computing power to request multiple choices :c)
                article.Paraphrased = json.choices[0].text;
                article.Paraphrased = article.Paraphrased.Trim();

                //skip if response is empty
                if (article.Paraphrased.Length < 5)
                    throw new InvalidResponseException("Empty LLM response!");

                //filter out LLM responses that translate the article (happens every now and then for some reason)
                if (Alphabets().Matches(article.Paraphrased).Count > article.Paraphrased.Length / 3.0)
                    throw new InvalidResponseException("Bad LLM response");

                Log.Information($"Paraphrased: {article.ID}");
                await DumpArticle(article);
            }

            //log exception if it happens
            catch (Exception ex)
            {
                Log.Information($"Exception:\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// Save article as JSON
    /// </summary>
    /// <param name="article">Article pair containing the original, and paraphrased articles</param>
    /// <returns></returns>
    private static async Task DumpArticle(ArticlePair article)
    {
        //open filestream and create new json file
        using FileStream fs = new(
            Path.Join(projectPath.FullName, "..", "data", $"{DateTime.Now:yyyyMMddHHss.fffffff}.json"),
            FileMode.Create, FileAccess.ReadWrite, FileShare.Read
        );

        //create streamwriter over filestream
        using StreamWriter writer = new(fs, Encoding.UTF8);

        //serialize articlepair record and write to file
        await writer.WriteAsync(JsonConvert.SerializeObject(article, Formatting.Indented));
    }

    /// <summary>
    /// Called on keyboard interrupt (ctrl+c)
    /// </summary>
    protected static void KeyboardInterruptHander(object? sender, ConsoleCancelEventArgs args)
    {
        articles.SaveLast();
    }

    //English alphabet
    [GeneratedRegex(@"[a-zA-Z]")]
    private static partial Regex Alphabets();
}

/// <summary>
/// Article pair object for storage
/// </summary>
public class ArticlePair
{
    public string ID { get; set; }
    public string Original { get; set; }
    public string Paraphrased { get; set; }

    public ArticlePair((string original, string id) articleTuple)
    {
        ID = articleTuple.id;
        Original = articleTuple.original;
        Paraphrased = string.Empty;
    }
}

/// <summary>
/// Invalid API response exception
/// </summary>
public class InvalidResponseException : Exception
{
    public InvalidResponseException() { }
    public InvalidResponseException(string message) : base(message) { }
    public InvalidResponseException(string message, Exception inner) : base(message, inner) { }
}
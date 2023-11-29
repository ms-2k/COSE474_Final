using Newtonsoft.Json.Linq;
using System.Text;

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

        //begin running paraphraser loop
        //Task.Run(ParaphraserLoop).Wait();

        Task.Run(async () =>
        {
            //initialize article fetcher
            await articles.InitializeAsync();

            //temp variable for original article
            string article = await articles.AcquireRandomArticle();

            Console.Write("original:\n" + article);

            dynamic json = JObject.Parse(
                await LlmApiRequest(
                    $"Paraphrase the following article in Korean in a journalist style. No paraphrased sentence should exactly match the given text. Do not translate the article.\\n\\nText:\\n{article.Replace("\n", "\\n")}\\n\\nParaphrased article in Korean:\\n",
                    (int)(article.Length * 1.1)
                )
            );

            Console.WriteLine("\n\nparaphrased:\n" + json.choices[0].text);
        }).Wait();
    }

    /// <summary>
    /// Continuously acquires random articles and sends a paraphrase request to LLM API
    /// </summary>
    public static async Task ParaphraserLoop()
    {
        //initialize article fetcher
        await articles.InitializeAsync();

        //temp variable for original article
        string article;

        //loop forever (until interrupt)
        while (true)
        {
            //acquire random article
            article = await articles.AcquireRandomArticle();

            await LlmApiRequest(GeneratePrompt(article + "\nParaphrase the above article in Korean."));
        }

    }

    /// <summary>
    /// Called on keyboard interrupt (ctrl+c)
    /// </summary>
    protected static void KeyboardInterruptHander(object? sender, ConsoleCancelEventArgs args)
    {
        articles.SaveLast();
    }
}
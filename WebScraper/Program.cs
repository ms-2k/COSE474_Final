﻿using Newtonsoft.Json;
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

        Task.Run(ParaphraserLoop).Wait();
    }

    private sealed record ArticlePair
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
    /// Continuously acquires random articles and sends a paraphrase request to LLM API
    /// </summary>
    public static async Task ParaphraserLoop()
    {
        //initialize article fetcher
        await articles.InitializeAsync();

        using FileStream logFile = new(
            Path.Join(projectPath.FullName, "log.txt"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read
        );
        using StreamWriter logger = new(logFile);

        ArticlePair article;

        //loop forever (until interrupt)
        while (true)
        {
            //acquire random article
            article = new(await articles.AcquireRandomArticle());

            //attempt to acquire paraphrased article
            try
            {
                //log current time
                await logger.WriteAsync($"[{DateTime.Now:u}] ");

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
                article.Paraphrased = article.Paraphrased.Trim().Replace("\n", "\\n");
            }

            //log exception if it happens
            catch (Exception ex)
            {
                await logger.WriteLineAsync(ex.Message);
            }

            //save article as json
            finally
            {
                await logger.WriteLineAsync(article.ID);
                await DumpArticle(article);
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
}
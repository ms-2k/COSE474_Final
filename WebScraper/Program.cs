using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

namespace WebScraper;

public partial class Program
{
    //llm api server uri
    private static readonly Uri LlmServer = new("http://localhost:5000/v1/completions");

    //request header type to use
    private static readonly MediaTypeHeaderValue headerType = new("application/json");

    //current project directory
    private static readonly DirectoryInfo projectPath = new($"{Directory.GetCurrentDirectory()}/../../..");

    //cache http client
    private static readonly HttpClient client = new();

    public static void Main(string[] args)
    {

        Console.OutputEncoding = Encoding.UTF8;

        Task.Run(async () =>
        {
            ArticleFetcher articles = new(projectPath);
            await articles.InitializeAsync();

            Console.Write(await articles.AcquireRandomArticle());
        }).Wait();
        /*
        Task.Run(async () =>
        {
            var doc = await new HtmlWeb().LoadFromWebAsync(url);

            string str = StripHtml(doc.DocumentNode.SelectSingleNode($"//*[@id=\"dic_area\"]"));

            using HttpClient client = new();
            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = LlmServer,
                Content = new StringContent(
                    GeneratePrompt("Generate a cake recipe:", 500),
                    headerType
                )
            };

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var llmOutput = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

            Console.WriteLine(llmOutput.ToString());
        }).Wait();
        */
    }

    /// <summary>
    /// Performs API call to LLM server with given parameters
    /// </summary>
    /// <param name="prompt">LLM instruction prompt.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="temperature">Generation temperature</param>
    /// <param name="topP">Generation top_p.</param>
    /// <returns></returns>
    private static async Task<JsonElement> LlmApiRequest(Uri ApiLoc, string prompt, int maxTokens = 0, float temperature = 0.7f, float topP = 0.8f)
    {
        //create http request message with OpenAPI compatible prompt as body to LLM server
        HttpRequestMessage request = new()
        {
            Method = HttpMethod.Post,
            RequestUri = ApiLoc,
            Content = new StringContent(
                GeneratePrompt(prompt, maxTokens > 0? maxTokens : prompt.Length),
                headerType
            )
        };

        //acquire response
        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        //return acquired response
        return await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
    }

    /// <summary>
    /// Generates an OpenAI API compatible prompt from given text.
    /// </summary>
    /// <param name="text">LLM instruction prompt.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="temperature">Generation temperature</param>
    /// <param name="topP">Generation top_p.</param>
    /// <returns></returns>
    private static string GeneratePrompt(string text, int maxTokens = 0, float temperature = 0.7f, float topP = 0.8f)
    {
        return string.Concat(
            "{\n\t\"prompt\": \"",
            text,
            "\",\n\t\"max_tokens\": ",
            maxTokens > 0? maxTokens : text.Length,
            ",\n\t\"temperature\": ",
            temperature,
            ",\n\t\"top_p\": ",
            topP,
            ",\n\t\"seed\": ",
            DateTime.Now.Ticks,
            "\n}"
        );
    }
}
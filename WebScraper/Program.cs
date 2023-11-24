using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

namespace WebScraper;

public partial class Program
{
    private static readonly Uri LlmServer = new("http://localhost:5000/v1/completions");
    private static readonly MediaTypeHeaderValue headerType = new("application/json");

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        string url = $"https://n.news.naver.com/article/055/0001108114";
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
    }

    /// <summary>
    /// Generates an OpenAI API compatible prompt from given text.
    /// </summary>
    /// <param name="text">LLM instruction prompt.</param>
    /// <param name="maxTokens">Maximum number of tokens to generate.</param>
    /// <param name="temperature">Generation temperature</param>
    /// <param name="topP">Generation top_p.</param>
    /// <returns></returns>
    public static string GeneratePrompt(string text, int maxTokens = 0, float temperature = 0.7f, float topP = 0.8f)
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

    /// <summary>
    /// Strips HTML node of special elements and returns in plaintext
    /// </summary>
    /// <param name="parent">HTML node to start from</param>
    /// <returns>Text only body of <see cref="HtmlNode"/></returns>
    public static string StripHtml(HtmlNode parent)
    {
        //create temp string builder
        StringBuilder sb = new();

        //loop through children
        foreach (var node in parent.ChildNodes)
        {
            //append text if the node is just text
            if (node.NodeType == HtmlNodeType.Text)
            {
                sb.Append(node.InnerText.Trim());

                continue;
            }

            //append special characters and certain elements
            sb.Append(node.Name switch
            {
                "span" when node.HasClass("data-lang") => node.InnerText.Trim(),
                "strong" => node.InnerText.Trim(),
                "br" => '\n',
                _ => string.Empty
            });
        }

        //trim and return stripped document
        return sb.ToString().Replace("\n\n", "\n").Trim();
    }
}
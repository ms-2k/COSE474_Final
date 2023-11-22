using System.Net.Http.Headers;
using System.Text;
using HtmlAgilityPack;

namespace WebScraper;

public partial class Program
{
    private static readonly Uri LlmServer = new("http://localhost:18888/v1/chat/completions");

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
                Content = new StringContent(GeneratePrompt("todo"))
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string llmOutput = await response.Content.ReadAsStringAsync();

            Console.WriteLine(llmOutput);
        }).Wait();
    }

    public static string GeneratePrompt(string text)
    {
        return "todo";
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
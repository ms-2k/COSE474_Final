using System.Text;

namespace WebScraper;

public class Program
{
    static Program()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static void Main(string[] args)
    {
        string url = $"https://news.naver.com/main/ranking/popularDay.naver";
        string text = string.Empty;

        Task.Run(async () =>
        {
            text = await GetContent(url);

            string output = Path.Join(Directory.GetCurrentDirectory(), "test.txt");
            using StreamWriter writer = new(output, Encoding.UTF8, new FileStreamOptions()
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.OpenOrCreate,
                Share = FileShare.Read
            });

            await writer.WriteAsync(text);
        }).Wait();
    }

    private static async Task<string> GetContent(string url)
    {
        using HttpClient client = new();
        using HttpResponseMessage response = await client.GetAsync(url);
        
        using StreamReader sr = new(await response.Content.ReadAsStreamAsync(), Encoding.GetEncoding("euc-kr"), false);

        return await sr.ReadToEndAsync();
    }
}
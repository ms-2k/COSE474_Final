using System.Net.Http.Headers;

namespace WebScraper
{
    public partial class Program
    {
        //request header type to use
        public static readonly MediaTypeHeaderValue headerType = new("application/json");

        //cache http client
        public static readonly HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(300)
        };

        /// <summary>
        /// Performs API call to LLM server with given parameters
        /// </summary>
        /// <param name="prompt">LLM instruction prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Generation temperature</param>
        /// <param name="topP">Generation top_p.</param>
        /// <returns></returns>
        public static async Task<string> LlmApiRequest(string prompt, int maxTokens = 0, float temperature = 0.7f, float topP = 0.8f)
        {
            //create http request message with OpenAPI compatible prompt as body to LLM server
            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Post,
                RequestUri = LlmServer,
                Content = new StringContent(
                    GeneratePrompt(prompt, maxTokens),
                    headerType
                )
            };

            //acquire response
            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            //return acquired response
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Generates an OpenAI API compatible prompt from given text.
        /// </summary>
        /// <param name="text">LLM instruction prompt.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="temperature">Generation temperature</param>
        /// <param name="topP">Generation top_p.</param>
        /// <returns></returns>
        public static string GeneratePrompt(string text, int maxTokens = 0, float temperature = 0.7f, float topP = 0.8f, float presencePenalty = 0.6f)
        {
            return string.Concat(
                "{\n\t\"prompt\": \"",
                text,
                "\",\n\t\"max_tokens\": ",
                maxTokens > 0 ? maxTokens : text.Length,
                ",\n\t\"temperature\": ",
                temperature,
                ",\n\t\"top_p\": ",
                topP,
                ",\n\t\"stop\": [\"번역결과\", \"번역\", \"\\n\\n\\n\"]",
                ",\n\t\"presence_penalty\": ",
                presencePenalty,
                ",\n\t\"seed\": ",
                DateTime.Now.Ticks,
                "\n}"
            );
        }
    }
}

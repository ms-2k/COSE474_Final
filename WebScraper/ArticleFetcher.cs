using HtmlAgilityPack;
using System.Text;

namespace WebScraper
{
    public class ArticleFetcher
    {
        //dictionary of press id and last article id
        private readonly Dictionary<int, int> articleKeys;
        private readonly List<int> oids;

        //path to article list root directory
        private readonly DirectoryInfo projectPath;

        //primary constructor
        public ArticleFetcher(DirectoryInfo projectPath)
        {
            this.projectPath = projectPath;
            articleKeys = new();
            oids = new();
        }

        /// <summary>
        /// Acquire a random article
        /// </summary>
        /// <returns><see cref="string"></see> representation of an article body</returns>
        public async Task<string> AcquireRandomArticle()
        {
            //acquire random oid
            var oid = oids[new Random().Next(0, oids.Count)];
            int aid = articleKeys[oid]--;

            //acquire article
            var doc = await new HtmlWeb().LoadFromWebAsync(AcquireArticleURL(oid, aid));

            //acquire just the article body
            return StripHtml(doc.DocumentNode.SelectSingleNode($"//*[@id=\"dic_area\"]"));
        }

        /// <summary>
        /// Initialize article table
        /// </summary>
        public async Task InitializeAsync()
        {
            //acquire file stream and read as string
            using FileStream fs = new(projectPath.FullName + "\\articles.csv", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader sr = new(fs);

            //temp variable to store each line
            string? line;

            //read line by line until end of file
            while ((line = await sr.ReadLineAsync()) != null)
            {
                //split line by commas
                var pieces = line.Split(',');
                (int, int, int) vals;

                try
                {
                    //parse them into integers
                    vals = (int.Parse(pieces[0]), int.Parse(pieces[1]), 0);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Parse error for " + line);
                    continue;
                }

                //add to article keys
                articleKeys.Add(vals.Item1, vals.Item2);
            }

            oids.AddRange(articleKeys.Keys);
        }

        /// <summary>
        /// Create article URL from press ID and article ID
        /// </summary>
        /// <param name="oid">Press ID</param>
        /// <param name="aid">Article ID</param>
        /// <returns>String represetation of article URL</returns>
        private static string AcquireArticleURL(int oid, int aid)
        {
            return $"https://n.news.naver.com/article/{oid:D3}/{aid:D10}";
        }

        /// <summary>
        /// Strips HTML node of special elements and returns in plaintext
        /// </summary>
        /// <param name="parent">HTML node to start from</param>
        /// <returns>Text only body of <see cref="HtmlNode"/></returns>
        private static string StripHtml(HtmlNode parent)
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
}

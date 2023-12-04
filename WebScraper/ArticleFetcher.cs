using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;

namespace WebScraper
{
    public partial class ArticleFetcher
    {
        //dictionary of press id and last article id
        private readonly Dictionary<int, int> articleKeys;
        private readonly List<int> oids;

        //path to article list root directory
        private readonly DirectoryInfo projectPath;

        //matches special HT
        [GeneratedRegex(@"\&\w{2,10}\;")]
        private static partial Regex SpecialCharMatch();

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
        public async Task<(string Article, string Url)> AcquireRandomArticle()
        {
            //acquire random oid
            var oid = oids[new Random().Next(0, oids.Count)];
            int aid = articleKeys[oid]--;

            //acquire article url
            string articleUrl = AcquireArticleURL(oid, aid);

            //acquire article
            var doc = await new HtmlWeb()
            {
                AutoDetectEncoding = false,
                OverrideEncoding = Encoding.GetEncoding(949)
            }.LoadFromWebAsync(articleUrl);

            //acquire just the article body
            return (StripHtml(doc.DocumentNode.SelectSingleNode($"//*[@id=\"dic_area\"]")), articleUrl);
        }

        /// <summary>
        /// Initialize article table
        /// </summary>
        public async Task InitializeAsync()
        {
            //clear stored data (if any)
            articleKeys.Clear();
            oids.Clear();

            //acquire article list directory
            var articleDir = new DirectoryInfo(Path.Join(projectPath.FullName, "articles"));

            //acquire last accessed article list
            var articleList = (from f in articleDir.GetFiles()
                               orderby f.LastWriteTime descending
                               select f).First();

            //acquire file stream and read as string
            using FileStream fs = new(
                Path.Join(articleList.FullName),
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite
            );
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
        /// Saves current article IDs.
        /// </summary>
        /// <param name="savePath">full path to where last article ID data should be stored</param>
        /// <returns></returns>
        public void SaveLast()
        {
            //create new file 
            using FileStream fs = new(
                Path.Join(projectPath.FullName, "articles", string.Join('.', DateTime.Now.ToString("yyyyMMddHHss.fffffff"), "csv"))
                , FileMode.Create, FileAccess.Write, FileShare.Read
            );

            //create stream writer over filestream
            using StreamWriter sw = new(fs);

            //write each press id and article id pairs to file
            foreach (var oid in oids)
                sw.WriteLine($"{oid:D3}, {articleKeys[oid]:D10}");
        }

        /// <summary>
        /// Saves current article IDs asynchronously.
        /// </summary>
        /// <param name="savePath">full path to where last article ID data should be stored</param>
        /// <returns></returns>
        public async Task SaveLastAsync()
        {
            //create new file 
            using FileStream fs = new(
                Path.Join(projectPath.FullName, "articles", string.Join('.', DateTime.Now.ToString("yyyyMMddHHss.fffffff"), "csv"))
                ,FileMode.Create, FileAccess.Write, FileShare.Read
            );

            //create stream writer over filestream
            using StreamWriter sw = new(fs);

            //write each press id and article id pairs to file
            foreach (var oid in oids)
                await sw.WriteLineAsync($"{oid:D3}, {articleKeys[oid]:D10}");
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
            return SpecialCharMatch().Replace(
                sb.ToString().Replace("\n\n", "\n").Trim(),
                string.Empty
            );
        }
    }
}

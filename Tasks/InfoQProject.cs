using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Tasks
{
    using Lib;
    public class InfoQProject : Project
    {
        protected override void StartTask()
        {
            var list = GetList(this.Url);

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var url = item.OriginUrl;
                var content = Request(ref url);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    item.HtmlContent = RegexMatch(content, @"<div class=""text_info"">\s*(?<content>[\s\S]*?)\s*<div class=""random_links"">", "content");
                    Import2Database("Article", item);

                    SetProgress(new int[] { i }, new int[] { list.Count });
                }
            }
        }


        private List<Article> GetList(string url)
        {
            var result = new List<Article>();
            var content = Request(ref url);

            if (!string.IsNullOrWhiteSpace(content))
            {
                var pattern = @"<h2>\s*<a\s*href=""(?<url>.*?)""\s*.*?>\s*(?<title>.*?)\s*</a>";
                foreach (var item in RegexMatches(content, pattern))
                {
                    var model = new Article(this.Url);

                    model.Title = item["title"];
                    model.OriginUrl = FillUrl(item["url"]);

                    result.Add(model);
                }
            }

            return result;
        }

        public override string Url
        {
            get { return "http://www.infoq.com/news/"; }
        }

        public override string Name
        {
            get { return "InfoQ"; }
        }

        #region Models

        class Article
        {
            public string Title { get; set; }
            public string OriginUrl { get; set; }
            public string OriginSite { get; set; }
            public string HtmlContent { get; set; }
            public DateTime CreatedDate { get; set; }

            public Article(string site)
            {
                this.OriginSite = site;
                this.CreatedDate = DateTime.Now;
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Tasks
{
    using Lib;
    public class ItHomeProject : Project
    {
        protected override void StartTask()
        {
            var list = GetList(this.Url);

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                // check whether the item exists.
                try
                {
                    if (ExistsInDatabase("Article", string.Format("Title='{0}'", item.Title.Replace("'", "''"))))
                    {
                        continue;
                    }
                }
                catch
                {
                    // nothing to do if the table does not be created
                }

                var url = item.OriginUrl;
                var content = Request(ref url, "gb2312");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    item.HtmlContent = RegexMatch(content, @"id=""paragraph"">(?<content>[\s\S]*?)</div>\s*<div class=""con-recom"">", "content");
                    item.HtmlContent = RegexReplace(item.HtmlContent, new Dictionary<string, string>() {
                        {@"<p class=""content_copyright"">[\s\S]*?</p>", "" },
                        {@"(?<prefix><img[^>]*)src=""[^""]*?"" data-original=""(?<url>.*?)""(?<suffix>.*?>)", @"${prefix}src=""${url}""${suffix}" },
                    });
                    item.HtmlContent = CleanUpAndFixHtml(item.HtmlContent);

                    if (string.IsNullOrWhiteSpace(item.HtmlContent)) continue;

                    var pubtimeText = RegexMatch(content, @"pubtime.*?>(?<dt>.*?)</span>", "dt");
                    var pubtime = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(pubtimeText) && DateTime.TryParse(pubtimeText, out pubtime))
                    {
                        item.PublishTime = pubtime;
                    }
                    else
                    {
                        item.PublishTime = pubtime;
                    }

                    SaveHtml(item.HtmlContent, GetContentFilesPath(
                        item.PublishTime.ToString("yyyy/MM/dd"),
                        Guid.NewGuid().ToString() + ".html"));

                    Import2Database("Article", item);

                    SetProgress(new int[] { i }, new int[] { list.Count });
                }
            }
        }


        private List<Article> GetList(string url)
        {
            var result = new List<Article>();
            var content = Request(ref url, "gb2312");

            if (!string.IsNullOrWhiteSpace(content))
            {
                var pattern = @"<li(>| .*?>)<span class=""date"">.*?</span><span class=""title""><a target=""_blank"" href=""(?<url>.*?)"">(?<title>.*?)</a></span></li>";
                foreach (var item in RegexMatches(content, pattern))
                {
                    var model = new Article(this.Url);
                    model.Origin = this.Name;
                    model.Title = GetTextInHtml(item["title"]);
                    model.OriginUrl = FillUrl(item["url"]);

                    if (model.Title.Contains("软媒")
                        || model.Title.Contains("魔方")
                    )
                    {
                        continue;
                    }
                    result.Add(model);
                }
            }

            return result;
        }

        public override string Url
        {
            get { return "http://ithome.com/"; }
        }

        public override string Name
        {
            get { return "ItHome"; }
        }

        protected override List<DbCounter> DbCounters
        {
            get
            {
                return new List<DbCounter>() {
                      new DbCounter("Article", "RowId"),
                };
            }
        }

        #region Models

        [Serializable]
        public class Article
        {
            public string Title { get; set; }
            public string OriginUrl { get; set; }
            public string OriginSite { get; set; }
            public string HtmlContent { get; set; }
            public DateTime PublishTime { get; set; }
            public string Origin { get; set; }

            public Article(string site)
            {
                this.OriginSite = site;
            }
        }

        #endregion
    }
}

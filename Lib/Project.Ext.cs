// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 9/5/2014 15:21:03
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using HtmlAgilityPack;

namespace TaskUI.Lib
{
    using Net.Bndy;
    using Net.Bndy.IO;
    using Net.Bndy.Web;
    public abstract partial class Project
    {
        /// <summary>
        /// Marks the skip.
        /// </summary>
        /// <param name="description">The description.</param>
        protected void MarkSkip(string description)
        {
            OnActionChanged(new ActionChangedEventArgs(
                this,
                description,
                ActionType.Skipped));
        }
        /// <summary>
        /// Tries to request the specified URL via GET method, or POST method if the postData is not null.
        ///		No exceptions thrown.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="postData">The post data.</param>
        /// <returns>The html source</returns>
        protected string TryRequest(ref string url, string encoding = null, Dictionary<string, string> postData = null, [CallerMemberName] string methodName = "")
        {
            string result = null;
            Exception lastError = null;
            int errors = 0;
            while (errors < 5)
            {
                try
                {
                    result = Request(ref url, encoding, postData, methodName);
                    break;
                }
                catch (AppException ex)
                {
                    OnError(ex.InnerException, new { Url = url }, ActionTypeOnError.Interrupted);
                    throw ex;
                }
                catch (Exception ex)
                {
                    errors++;
                    lastError = ex;
                    Thread.Sleep(5000);
                }
            }

            if (result == null)
                OnError(lastError, new { Url = url }, ActionTypeOnError.Skipped, methodName);

            return result;
        }
        /// <summary>
        /// Requests the specified URL via GET method, or POST method if the postData is not null.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="postData">The post data.</param>
        /// <returns>The html source</returns>
        protected string Request(ref string url, string encoding = null, Dictionary<string, string> postData = null, [CallerMemberName] string methodName = "")
        {
            string result = null;
            int err = 0;
            again:
            try
            {
                OnActionChanged(new ActionChangedEventArgs(
                    this,
                    string.Format("Requesting {0}...",
                        url),
                    ActionType.Request,
                    methodName)
                );
                if (postData == null)
                    result = WebHelper.Get(ref url, encoding ?? this.Encoding, this.ProxyHost, this.ProxyPort);
                else
                    result = WebHelper.Post(url, postData, encoding ?? this.Encoding, this.ProxyHost, this.ProxyPort);

                StartInfo.Requests++;
            }
            catch (WebException ex)
            {
                err++;
                if (ex.Response == null)
                {
                    // network error
                    if (err > 5)
                    {
                        OnError(ex, new { Url = url }, ActionTypeOnError.Retry, methodName);
                    }
                    goto again;
                }
                else
                {
                    // such as 404 error
                    throw ex;
                }
            }

            return result;
        }
        /// <summary>
        /// Analyzes the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="action">The action.</param>
        protected void Analyze(string url, Action action)
        {
            OnActionChanged(new ActionChangedEventArgs(
                this,
                string.Format("Analyzing {0}...", url),
                ActionType.Analyze)
            );
            if (action != null)
                action();
        }
        /// <summary>
        /// Tries to download the file by the specified URL. No exceptions thrown.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="saveAs">
        /// The local path. 
        /// You don't need to specify the file name, if the url includes the file name.
        /// </param>
        /// <returns>The file full name, if no errors occurred.</returns>
        protected string TryDownload(string url, string saveAs, [CallerMemberName] string methodName = "")
        {
            var count = 0;
            string result = null;
            Exception err = null;

            while (count < 5)
            {
                try
                {
                    result = Download(url, saveAs, methodName);
                    break;
                }
                catch (Exception ex)
                {
                    count++;
                    err = ex;
                }
            }

            if (result == null)
                OnError(err, new { Url = url }, ActionTypeOnError.Skipped, methodName);

            return result;
        }
        /// <summary>
        /// Downloads the file by the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="saveAs">
        /// The local path. 
        /// You don't need to specify the file name, if the url includes the file name.
        /// </param>
        /// <returns>The file full name, if no errors occurred.</returns>
        protected string Download(string url, string saveAs, [CallerMemberName] string methodName = "")
        {
            string result = null;

            again:
            try
            {
                OnActionChanged(
                    new ActionChangedEventArgs(
                        this,
                        string.Format("Downloading {0} to {1}...",
                            url,
                            saveAs
                        ),
                    ActionType.Download,
                    methodName)
                    );

                result = WebHelper.Download(url, saveAs, this.ProxyHost, this.ProxyPort);

                if (!string.IsNullOrWhiteSpace(result))
                    StartInfo.Downloads++;
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    // network error
                    OnError(ex, new { Url = url }, ActionTypeOnError.Retry, methodName);
                    goto again;
                }
                else
                {
                    // such as 404 error
                    throw ex;
                }
            }

            return result;
        }
        /// <summary>
        /// Tries to download the images in html (img tag) and replace with the local link.
        /// </summary>
        /// <param name="html">The html contains img tags.</param>
        /// <param name="url">The url of html</param>
        /// <param name="imagesLocation">The local directory for downloading.</param>
        /// <param name="localLinkPrefix">The prefix of the file name for local link.</param>
        /// <param name="methodName"></param>
        protected void TryDownloadImagesInHtml(ref string html, string url, string imagesLocation, string localLinkPrefix = null, [CallerMemberName] string methodName = "")
        {
            var doc = new HtmlDocument();
            var root = doc.DocumentNode;
            var imageTags = root.Descendants("img");
            if (imageTags != null)
            {
                foreach (HtmlNode tag in imageTags)
                {
                    if (tag.Attributes["src"] != null)
                    {
                        var imageUrl = tag.Attributes["src"].Value;
                        imageUrl = CombineUrl(url, imageUrl);

                        var file = TryDownload(imageUrl, imagesLocation, methodName);
                        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                        {
                            var localLink = Path.GetFileName(file);
                            if (!string.IsNullOrWhiteSpace(localLinkPrefix))
                                localLink = string.Format("{0}{1}", localLinkPrefix, localLink);

                            tag.Attributes["src"].Value = localLink;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Saves the HTML source.
        /// </summary>
        /// <param name="html">The HTML text.</param>
        /// <param name="fileName">The file full name</param>
        protected void SaveHtml(string html, string fileName, [CallerMemberName] string methodName = "")
        {
            OnActionChanged(new ActionChangedEventArgs(
                this,
                string.Format("Saving html to {0}...", fileName),
                ActionType.Save2Disk,
                methodName)
            );
            html.SaveAs(fileName);
            StartInfo.HtmlSourceCount++;
        }
        /// <summary>
        /// Searches the specified input string and returns the matched value.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="group">The group.</param>
        /// <returns>System.String.</returns>
        protected string RegexMatch(string input, string pattern, string group = null)
        {
            if (group == null)
            {
                return Regex.Match(input, pattern, RegexOptions.IgnoreCase).Value;
            }
            else
            {
                var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (m != null)
                {
                    return m.Groups[group].Value;
                }
            }
            return "";
        }
        /// <summary>
        /// Searches the specified input string for all occurrences of a specified regular expression.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>List<Dictionary<string, string>></returns>
        protected List<Dictionary<string, string>> RegexMatches(string input, string pattern)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            Regex re = new Regex(pattern, RegexOptions.IgnoreCase);
            foreach (Match m in re.Matches(input))
            {
                var d = new Dictionary<string, string>();

                foreach (var group in re.GetGroupNames())
                {
                    d[group] = m.Groups[group].Value;
                }
                result.Add(d);
            }

            return result;
        }
        /// <summary>
        /// Regexes the replace.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="replacements">
        /// The replacements.
        ///		The key is the pattern for matching
        ///		The value is the replacements.
        /// </param>
        /// <returns>System.String.</returns>
        protected string RegexReplace(string input, Dictionary<string, string> replacements)
        {
            foreach (KeyValuePair<string, string> kv in replacements)
            {
                input = Regex.Replace(input, kv.Key, kv.Value);
            }
            return input;
        }
        /// <summary>
        /// Combines the URLs.
        /// </summary>
        /// <param name="url1">The url1.</param>
        /// <param name="url2">The url2.</param>
        /// <returns>System.String.</returns>
        protected string CombineUrl(string url1, string url2)
        {
            return new Uri(new Uri(url1), url2).ToString();
        }
        /// <summary>
        /// Fills the URL.
        /// </summary>
        /// <param name="relativeUrl">The relative URL.</param>
        /// <returns>System.String.</returns>
        protected string FillUrl(string relativeUrl)
        {
            return new Uri(new Uri(this.Url), relativeUrl).ToString();
        }
        /// <summary>
        /// Gets the relative path.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>System.String.</returns>
        protected string GetRelativePath(params string[] paths)
        {
            return Path.Combine(paths);
        }
        /// <summary>
        /// Gets the path without root.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        protected string GetPathWithoutRoot(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = path.Replace(GetContentFilesPath() + "\\", "\\");
                path = path.Replace(GetHtmlSourcePath() + "\\", "\\");
            }

            return path;
        }
        private string GetScriptFile(SqlScriptType type)
        {
            var fileName = "";
            //switch (type)
            //{
            //	case SqlScriptType.Delete:
            //		fileName = "delete.sql";
            //		break;
            //	case SqlScriptType.Insert:
            //		fileName = "insert.sql";
            //		break;
            //	case SqlScriptType.Update:
            //		fileName = "update.sql";
            //		break;
            //}
            var dir = Path.Combine(this.DataVersionDir, "Scripts");
            if (Directory.Exists(dir))
            {
                var lastFile = new DirectoryInfo(dir).GetFiles()
                    .OrderByDescending(__ => __.LastWriteTime).FirstOrDefault();
                if (lastFile != null)
                {
                    // 2M size per file
                    if (lastFile.Length > 1024 * 1024 * 2)
                        fileName = (int.Parse(RegexMatch(lastFile.Name, @"\d+")) + 1) + ".sql";
                    else
                        return lastFile.FullName;
                }
                else
                {
                    fileName = "1.sql";
                }
            }
            else
            {
                Directory.CreateDirectory(dir);
                fileName = "1.sql";
            }

            var file = Path.Combine(dir, fileName);

            return file;
        }
        /// <summary>
        /// Gets the HTML source path.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>System.String.</returns>
        protected string GetHtmlSourcePath(params string[] paths)
        {
            var d = Path.Combine(this.DataVersionDir, "Html Source");
            if (paths != null)
            {
                d = Path.Combine(d, Path.Combine(paths));
            }
            return d;
        }
        /// <summary>
        /// Gets the content files path.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>System.String.</returns>
        protected string GetContentFilesPath(params string[] paths)
        {
            var d = Path.Combine(this.DataVersionDir, "Content Files");
            if (paths != null)
            {
                d = Path.Combine(d, Path.Combine(paths));
            }
            return d;
        }
        /// <summary>
        /// Gets the log file fule name by the log type.
        /// </summary>
        /// <param name="logType">Type of the log.</param>
        /// <returns>System.String.</returns>
        private string GetLogFile(MessageRank logType = MessageRank.Normal)
        {
            switch (logType)
            {
                case MessageRank.Warning:
                case MessageRank.Error:
                    return Path.Combine(this.DataVersionDir, "err.log");

                case MessageRank.Custom:
                    return Path.Combine(this.DataVersionDir, string.Format("{0}.log", this.Name));

                default:
                    return Path.Combine(this.DataVersionDir, "info.log");
            }
        }
        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        protected void Log(string message, object parameters = null)
        {
            var file = GetLogFile(MessageRank.Custom);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("{0} ----> {1}", DateTime.Now, message);
            sb.AppendLine();
            if (parameters != null)
            {
                foreach (var kv in parameters.ToDict())
                {
                    sb.AppendFormat("\t{0}\t\t{1}", kv.Key, kv.Value);
                    sb.AppendLine();
                }
            }

            File.AppendAllText(file, sb.ToString());
        }
        /// <summary>
        /// Removes the links.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <returns>System.String.</returns>
        protected string RemoveLinks(string html)
        {
            return Regex.Replace(html,
                @"<(a|A).*?>|</(a|A)>",
                "");
        }
        /// <summary>
        /// Fills the urls of HTML links.
        /// </summary>
        /// <param name="html">The HTML.</param>
        /// <param name="currentUrl">The current URL.</param>
        /// <param name="withoutAnchor">if set to <c>true</c> [without anchor starts with '#'].</param>
        /// <returns>System.String.</returns>
        protected string FillUrlsOfHtmlLinks(string html, string currentUrl, bool withoutAnchor = false)
        {
            var pattern = @"(HREF|href)=\s*(?<url>['""][\s\S]*?['""])";

            List<string> urls = new List<string>();
            foreach (Match m in Regex.Matches(html, pattern))
            {
                var url = m.Groups["url"].Value;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    if ((url.StartsWith("'#") || url.StartsWith(@"""#"))
                        && withoutAnchor)
                    {

                    }
                    else
                    {
                        urls.Add(url);
                    }
                }
            }

            foreach (var u in urls)
            {
                html = html.Replace(u, string.Format(@"""{0}""", CombineUrl(currentUrl,
                    u.Replace("'", "").Replace(@"""", "").Trim())));
            }

            return html;
        }
        /// <summary>
        /// Dumps the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        protected void Dump(object obj)
        {
            OnActionChanged(new ActionChangedEventArgs(this,
                string.Format("{0}", obj),
                ActionType.None));
        }
        /// <summary>
        /// Analyzes the web.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="patterns">The patterns.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="postData">The post data.</param>
        /// <returns>WebResult.</returns>
        protected WebResult AnalyzeWeb(ref string url, List<string> patterns,
            string encoding = null, Dictionary<string, string> postData = null)
        {
            try
            {
                var html = Request(ref url, encoding, postData);

                var result = new WebResult(url, html);

                if (!string.IsNullOrWhiteSpace(html))
                {
                    foreach (var pattern in patterns)
                    {
                        var reg = new Regex(pattern, RegexOptions.IgnoreCase);

                        foreach (Match m in reg.Matches(html))
                        {
                            var item = new WebResultItem(m.Value);

                            foreach (var group in reg.GetGroupNames())
                            {
                                item.Groups.Add(group, m.Groups[group].Value);
                            }

                            result.Items.Add(item);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                OnError(ex, new { Url = url });
                return null;
            }
        }
    }
}

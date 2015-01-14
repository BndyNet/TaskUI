using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	public class WebResult
	{
		public string Url { get; private set; }
		public string HtmlSource { get; private set; }
		public List<WebResultItem> Items { get; set; }
		public WebResult(string url, string html)
		{
			this.Url = url;
			this.HtmlSource = html;
			this.Items = new List<WebResultItem>();
		}
	}

	public class WebResultItem
	{
		public string Value { get; set; }
		public Dictionary<string, string> Groups { get; set; }

		public WebResultItem(string value)
		{
			this.Value = value;
			this.Groups = new Dictionary<string, string>();
		}
	}
}

// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 9/5/2014 17:40:55
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	public abstract class PageModel
	{
		//!+ Use fields instead of properties if the value does not need to be saved.
		public string _HtmlSource;
		public string _LocalRelativePath;
		public RecordType _DataActionType;

		public string _Url { get; set; }

		public PageModel(string url, string html)
		{
			_Url = url;
			_HtmlSource = html;
		}

		public PageModel() { }
	}
}

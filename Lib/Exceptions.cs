// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 2014-10-18 15:42:33
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

	public class AppException : Exception
	{
		public AppException(string msg, Exception ex)
			: base(msg, ex)
		{
			
		}
	}

	public class WebRequestFailedException : AppException
	{
		public string Url { get; set; }

		public override string Message
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(base.Message))
				{
					return base.Message;
				}
				else
				{
					if (!string.IsNullOrWhiteSpace(this.Url))
					{
						return string.Format("Request failed for {0}.", this.Url);
					}
					else
					{
						return "Request Failed";
					}
				}
			}
		}
		public WebRequestFailedException(string url = null, Exception ex = null)
			: base("", ex)
		{
			this.Url = url;
		}
	}
}

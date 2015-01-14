// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 8/7/2014 9:53:55
// ---------------------------------------------------------------------------------
// The project start information.
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	public class StartInfo
	{
		public DateTime StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public int AffectedRows { get; set; }
		public int HtmlSourceCount { get; set; }
		public int Requests { get; set; }
		public int Downloads { get; set; }
		public double Progress { get; set; }
		public int ErrorCount
		{
			get
			{
				return this.Errors.Count;
			}
		}
		public Exception LastError
		{
			get
			{
				return this.Errors.LastOrDefault();
			}
		}
		public List<Exception> Errors { get; private set; }

		public StartInfo()
		{
			this.StartTime = DateTime.Now;
			this.Errors = new List<Exception>();
		}

	}
}

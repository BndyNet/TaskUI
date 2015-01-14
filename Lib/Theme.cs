// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 8/14/2014 8:53:23
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
	public struct Theme
	{
		public string BackgroundColor;
		public string ForegroundColor;
		public Theme(string backgroundColor, string foregroundColor)
		{
			this.BackgroundColor = backgroundColor;
			this.ForegroundColor = foregroundColor;
		}
	}
}

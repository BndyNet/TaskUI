// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 8/12/2014 11:12:47
// ---------------------------------------------------------------------------------
// ErrorOccurredEventHandler and Event Args
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	public class ErrorOccurredEventArgs : EventArgs
	{
		public Project Project { get; private set; }
		public string Description { get; private set; }
		public Exception Error { get; private set; }
		public DateTime TimeOccurred { get; private set; }
		public object ErrorParameters { get; private set; }
		public ActionTypeOnError TodoType { get; private set; }

		public ErrorOccurredEventArgs(Project p,
			string description,
			Exception e,
			object errorParameters = null,
			ActionTypeOnError todo = ActionTypeOnError.Default)
		{
			this.Project = p;
			this.Error = e;
			this.ErrorParameters = errorParameters;
			this.TimeOccurred = DateTime.Now;
			this.TodoType = todo;

			if(string.IsNullOrWhiteSpace(description))
			{
				this.Description = this.Error.Message;
			}
		}

	}
	public delegate ActionTypeOnError ErrorOccurredEventHandler(ErrorOccurredEventArgs e);
}

// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 8/6/2014 15:40:50
// ---------------------------------------------------------------------------------
// Handlers, ActionType and Event Args about ActionChanged Event
// =================================================================================

using System;

namespace TaskUI.Lib
{
	public class ActionChangedEventArgs : EventArgs
	{
		public Project Project { get; set; }
		public DateTime ActionTime { get; set; }
		public string Description { get; set; }
		public ActionType ActionType { get; set; }
		public string Invoker { get; private set; }

		/// <summary>
		/// Returns a string includes Time and ProgressDescription.
		/// </summary>
		public string DescriptionWithTime
		{
			get
			{
				return string.Format("{0} ---> {2}{1}",
					this.ActionTime,
					this.Description,
					string.IsNullOrWhiteSpace(this.Invoker) 
						? ""
						: string.Format("[{0}]\t", this.Invoker)
					);
			}
		}

		public ActionChangedEventArgs(
			Project project,
			string progressDescription,
			ActionType actionType,
			string invoker = null
			)
		{
			this.Project = project;
			this.Description = progressDescription;
			this.ActionType = actionType;
			this.ActionTime = DateTime.Now;
			this.Invoker = invoker;
		}
	}

	public enum ActionType
	{
		None,
		Request,
		Download,
		Analyze,
		Save2Database,
		Save2Disk,
		UpdateDatabase,
		Skipped,
		Stopped,
		CopyFile,
		BackupDatabase,
	}

	public delegate void ActionChangedEventHandler(ActionChangedEventArgs args);
}

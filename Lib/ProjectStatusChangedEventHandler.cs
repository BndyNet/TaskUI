// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 8/9/2014 11:27:23
// ---------------------------------------------------------------------------------
// ProjectStatusChangedEventHandler and Event Args
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	public delegate void ProjectStatusChangedEventHandler(ProjectStatusChangedEventArgs e);
	public class ProjectStatusChangedEventArgs : EventArgs
	{
		public Project Project { get; set; }
		public ProjectStatus Status
		{

			get
			{
				return this.Project.Status;
			}
		}

		public ProjectStatusChangedEventArgs(Project project)
		{
			this.Project = project;
		}
	}
}

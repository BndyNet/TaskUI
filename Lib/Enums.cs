using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	[Flags]
	public enum ProjectStatus
	{
		Ready = 0,
		Started = 1 << 0,
		Canceled = 1 << 1,
		Done = 1 << 2,
		ErrorOccurred = 1 << 3,
		Paused = 1 << 4,
	}

	public enum ActionTypeOnError
	{
		Default,
		Skipped,
		Retry,
		Interrupted,
	}
}

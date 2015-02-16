// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 9/5/2014 8:57:03
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TaskUI
{
	public class ToggleWindowCommand : ICommand
	{
		public bool CanExecute(object parameter)
		{
			if (CanExecuteChanged != null)
				CanExecuteChanged(this, null);
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void Execute(object parameter)
		{
			var win = parameter as Window;
			if (win != null)
			{
				win.WindowState = WindowState.Normal;
				win.ShowInTaskbar = true;
				win.ShowActivated = true;
			}
		}
	}

}

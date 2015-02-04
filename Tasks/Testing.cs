// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 9/4/2014 10:11:32
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Tasks
{
	using Lib;
	public class Testing : Project
	{
		private bool _err = false;
		public Testing()
		{
			this.RequireCheckConfig = false;
		}

		protected override void StartTask()
		{
			var t1 = 1;
			var t2 = 10;
			var t3 = 10;
			for (int a = 0; a < t1; a++)
			{
				for (int b = 0; b < t2; b++)
				{
					for (int c = 0; c < t3; c++)
					{

						int awefawef = 3232;

						CheckIsCancellationOrPauseRequested();
						//if (!_err)
						//{
						//	OnError(new Exception("awefa"), null, todo:ActionTypeOnError.Interrupted);
						//	_err = true;
						//}

						//var url = "https://code.msdn.microsoft.com/vstudio";
						//var html =TryRequest(ref url);

						//Import2Database("Test", new { 
						//	dt = DateTime.Now,
						//});

                        System.Threading.Thread.Sleep(500);

                        int xxxx = 3232;

						var p = GetProgressValue(new int[] { a, b, c },
							new int[] { t1, t2, t3 });

						Dump(string.Format("{0}/{1}/{2}", a, b, c));

						SetProgress(p);
					}
				}
			}
		}

		public override string Url
		{
			get { return "http://www.com"; }
		}

		public override string Name
		{
			get { return "Test Project"; }
		}


		public override Theme? Theme
		{
			get
			{
				return new Theme("#35496A", "white");

			}
		}

		public override SwitcherCollection Switchers
		{
			get
			{
				return new SwitcherCollection(
					new Switcher("A", "AAAAA", false),
					new Switcher("C", "CCCCC", false, "A"),
					new Switcher("D", "DDDDD", true, "A"),
					new Switcher("B", "BBBBB", true)
					);
			}
		}
	}
}

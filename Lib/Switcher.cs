// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 8/30/2014 13:08:14
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskUI.Lib
{
	[Serializable]
	public class Switcher : INotifyPropertyChanged
	{
		private bool _value;
		public string Name { get; set; }
		public string Description { get; set; }
		public string DependTo { get; set; }
		public bool DefaultEnabled { get; set; }
		public bool Value
		{
			get
			{
				return _value;
			}
			set
			{
				_value = value;
				OnPropertyChanged();
			}
		}

		public Switcher() { }

		public Switcher(string name, string description, bool defaultValue, string dependTo = null)
		{
			this.Name = name;
			this.Description = description;
			this.Value = defaultValue;
			this.DependTo = dependTo;
			this.DefaultEnabled = true;
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}


	public class SwitcherCollection : List<Switcher>
	{
		private Dictionary<string, Switcher> _switchers;

		public Switcher this[string name]
		{
			get
			{
				return _switchers[name];
			}
		}

		public SwitcherCollection(params Switcher[] switchers)
		{
			_switchers = new Dictionary<string, Switcher>();
			foreach (var s in switchers)
			{
				_switchers.Add(s.Name, s);
				this.Add(s);
			}

			foreach (var s in _switchers.Values)
			{
				if (!string.IsNullOrWhiteSpace(s.DependTo))
				{
					if (_switchers.ContainsKey(s.DependTo))
					{
						s.DefaultEnabled = _switchers[s.DependTo].Value;
						if (!s.DefaultEnabled)
						{
							s.Value = false;
						}
					}
				}
			}
		}

		public new IEnumerator GetEnumerator()
		{
			return _switchers.Values.GetEnumerator();
		}
	}
}

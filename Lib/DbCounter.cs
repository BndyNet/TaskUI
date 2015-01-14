using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
	[Serializable]
	public class DbCounter : INotifyPropertyChanged
	{
		private List<string> Inserts { get; set; }
		private List<string> Updates { get; set; }
		private List<string> Deletes { get; set; }
		public string Table { get; set; }
		public string Identity { get; set; }

		public string Summary
		{
			get
			{
				return this.ToString();
			}
		}

		protected DbCounter()
		{

		}
		public DbCounter(string tableName, string identityColumn)
		{
			this.Table = tableName;
			this.Identity = identityColumn;

			this.Inserts = new List<string>();
			this.Updates = new List<string>();
			this.Deletes = new List<string>();
		}

		public void Count(object id, CountType type)
		{
			if (id == null) return;

			var sid = id.ToString();
			switch (type)
			{
				case CountType.Insert:
					if (!this.Inserts.Contains(sid))
					{
						this.Inserts.Add(sid);
					}
					break;

				case CountType.Update:
					if (!this.Inserts.Contains(sid) && !this.Updates.Contains(sid))
					{
						this.Updates.Add(sid);
					}
					break;

				case CountType.Delete:
					if (!this.Inserts.Contains(sid) && !this.Updates.Contains(sid))
					{
						this.Deletes.Add(sid);
					}
					break;
			}

			OnPropertyChanged("Summary");
		}

		public void Reset()
		{
			this.Inserts.Clear();
			this.Updates.Clear();
			this.Deletes.Clear();
		}

		public override string ToString()
		{
			return string.Format("{0}: Ins ({1}) - Upd ({2})", this.Table, this.Inserts.Count(), this.Updates.Count());
		}

		public enum CountType
		{
			Insert,
			Update,
			Delete,
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}

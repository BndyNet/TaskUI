// =================================================================================
// Copyright (c) 2014 CommerNet Co., Ltd.
// Created by Bendy at 8/5/2014 11:19:52
// ---------------------------------------------------------------------------------
// Abstract class about Project, all projects MUST implement this class for startup.
// =================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TaskUI.Lib
{
    using Net.Bndy;
    using Net.Bndy.Data.SqlServer;

    /// <summary>
    /// The derived classes can use the following members:
    ///		CheckIsCancellationRequested()
    ///		OnActionChanged(GetActionChangedEventArgsOf...
    ///		OnError(...)
    ///		CombineUrl(...)
    ///		PadUrl(...)
    ///		GetRelativePath(...)
    ///		GetHtmlSourcePath(...)
    ///		GetContentFilesPath(...)
    ///		DropUselessString(...)
    ///		PadUrlsOfHtmlLinks(...)
    ///		-----------------------------------------------
    ///		Request(...)
    ///		Analyze(...)
    ///		Download(...)
    ///		SaveHtml(...)
    ///		ExistsInDatabase(...)
    ///		GetScalarValueFromDatabase(...)
    ///		QueryFromDatabase(...)
    ///		Import2Database(...)
    /// </summary>
    [Serializable]
    public abstract partial class Project : INotifyPropertyChanged
    {
        #region Fields & Properties
        [XmlIgnore]
        public StartInfo StartInfo;

        private ProjectStatus _status;
        private MsSqlFactory _dbFactory;
        private string _dataVersion;
        private SwitcherCollection _switchersSource;
        private List<DbCounter> _dbCounters;
        private bool _inPause = false;

        private string _encoding = "utf-8";
        private string _dbServer;
        private string _dbName;
        private string _dbUser;
        private string _dbPassword;
        private string _workspace;
        private string _proxyHost;
        private string _proxyPort;
        private bool _enableDatabase = true;
        private bool _enableScriptsGenerate = true;
        private bool _enableBackupDb = false;
        private bool _requireCheckConfig = true;

        public string Encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
                OnPropertyChanged();
            }
        }
        public string DbServer
        {
            get
            {
                return _dbServer;
            }
            set
            {
                _dbServer = value;
                OnPropertyChanged();
            }
        }
        public string DbName
        {
            get
            {
                return _dbName;
            }
            set
            {
                _dbName = value;
                OnPropertyChanged();
            }
        }
        public string DbUser
        {
            get
            {
                return _dbUser;
            }
            set
            {
                _dbUser = value;
                OnPropertyChanged();
            }
        }
        public string DbPassword
        {
            get
            {
                return _dbPassword;
            }
            set
            {
                _dbPassword = value;
                OnPropertyChanged();
            }
        }
        public string Workspace
        {
            get
            {
                return _workspace;
            }
            set
            {
                _workspace = value;
                OnPropertyChanged();
            }
        }
        public string ProxyHost
        {
            get
            {
                return _proxyHost;
            }
            set
            {
                _proxyHost = value;
                OnPropertyChanged();
            }
        }
        public string ProxyPort
        {
            get
            {
                return _proxyPort;
            }
            set
            {
                _proxyPort = value;
                OnPropertyChanged();
            }
        }
        public bool EnableScriptGenerate
        {
            get
            {
                return _enableScriptsGenerate;
            }
            set
            {
                _enableScriptsGenerate = value;
                OnPropertyChanged();
            }
        }
        public bool EnableDatabase
        {
            get
            {
                return _enableDatabase;
            }
            set
            {
                _enableDatabase = value;
                OnPropertyChanged();
            }
        }
        public bool EnableBackupDb
        {
            get
            {
                return _enableBackupDb;
            }
            set
            {
                _enableBackupDb = value;
                OnPropertyChanged();
            }
        }
        public List<string> Versions { get; set; }
        [XmlIgnore]
        public ProjectStatus Status
        {
            get
            {
                return _status;
            }
            private set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public string DataVersion
        {
            get
            {
                return _dataVersion;
            }
            private set
            {
                _dataVersion = value;
            }
        }
        [XmlIgnore]
        public string DataVersionDir
        {
            get
            {
                var d = "v" + this.DataVersion + "_Updates";
                var dir = Path.Combine(this.Workspace, d);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                return dir;
            }
        }
        [XmlIgnore]
        public bool RequireCheckConfig
        {
            get
            {
                return _requireCheckConfig;
            }
            protected set
            {
                _requireCheckConfig = value;
            }
        }
        /// <summary>
        /// Gets the <see cref="System.Nullable{System.Boolean}"/> with the specified switcher name.
        /// </summary>
        /// <param name="switcherName">Name of the switcher.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected bool this[string switcherName]
        {
            get
            {
                if (this.SwitchersSource != null && this.SwitchersSource.Count > 0)
                    return SwitchersSource.Where(__ => __.Name.Equals(switcherName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault().Value;
                else
                    throw new NotImplementedException(
                        string.Format("No Switcher '{0}' Found", switcherName));
            }
        }
        [XmlIgnore]
        public virtual bool RequireHighPerformance
        {
            get
            {
                return true;
            }
        }
        [XmlIgnore]
        public SwitcherCollection SwitchersSource
        {
            get
            {
                if (_switchersSource == null)
                    _switchersSource = this.Switchers;
                return _switchersSource;
            }
        }
        [XmlIgnore]
        public List<DbCounter> DbCountersSource
        {
            get
            {
                if (_dbCounters == null)
                    _dbCounters = this.DbCounters;

                if (_dbCounters == null)
                    _dbCounters = new List<DbCounter>();

                return _dbCounters;
            }
        }
        [XmlIgnore]
        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.DbName)
                    || string.IsNullOrWhiteSpace(this.DbPassword)
                    || string.IsNullOrWhiteSpace(this.DbServer)
                    || string.IsNullOrWhiteSpace(this.DbUser)
                ) return null;

                return string.Format("Data Source={0}; Initial Catalog={1}; User ID={2}; Password={3}",
                    this.DbServer, this.DbName, this.DbUser, this.DbPassword);
            }
        }
        [XmlIgnore]
        public bool IsDatabaseReady
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.ConnectionString))
                {
                    return new MsSqlFactory(this.ConnectionString).CanOpen();
                }
                return false;
            }
        }
        [XmlIgnore]
        public bool AllReady
        {
            get
            {
                if (IsDatabaseReady && !string.IsNullOrWhiteSpace(this.Workspace))
                {
                    return true;
                }
                return false;
            }
        }
        protected string ConfigurationFile
        {
            get
            {
                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    return System.IO.Path.Combine(
                        ApplicationDeployment.CurrentDeployment.DataDirectory,
                        string.Format("Projects\\{0}.duconfig", this.Name)
                        );
                }
                else
                {
                    return System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        string.Format("Projects\\{0}.duconfig", this.Name)
                        );
                }
            }
        }

        #endregion Fields & Properties

        protected Project()
        {
            if (ActionChanged == null)
                ActionChanged = DefaultActionChanged;
        }
        public void Start()
        {
            if (this.RequireCheckConfig && !this.AllReady) return;

            this._cancellationTokenSource = new System.Threading.CancellationTokenSource();
            this.StartInfo = new Lib.StartInfo();
            this._dbFactory = new MsSqlFactory(this.ConnectionString);
            this.Status = ProjectStatus.Started;
            this.DataVersion = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            // version history
            if (this.Versions == null)
                this.Versions = new List<string>();
            this.Versions.Add(this.DataVersion);
            this.Save();

            OnStarted();

            Task t = new Task(() =>
            {
                try
                {
                    StartTask();
                    this.Status = ProjectStatus.Done;
                    OnDone();
                }
                catch (OperationCanceledException)
                {
                    this.Status = ProjectStatus.Canceled;
                    OnCanceled();
                }
                catch (AppException)
                {
                    this.Status = ProjectStatus.ErrorOccurred;
                }
#if !DEBUG
				catch (Exception e)
				{
					this.Status = ProjectStatus.ErrorOccurred;

					this.StartInfo.Errors.Add(e);

					OnError(e, null, todo: ActionTypeOnError.Interrupted);
				}
#endif
            }, _cancellationTokenSource.Token);

            var doCompleted = t.ContinueWith(__ =>
            {
                OnCompleted();
            });

            t.Start();
        }
        public void Cancel()
        {
            this.Status = ProjectStatus.Canceled;
            _inPause = false;

            OnCanceling();

            _cancellationTokenSource.Cancel();
        }

        public void Pause()
        {
            _inPause = true;
            this.Status = this.Status | ProjectStatus.Paused;

            if (Pausing != null)
            {
                Pausing();
            }

        }
        public void Resume()
        {
            _inPause = false;
            this.Status = ProjectStatus.Started;

            if (Resumed != null)
            {
                Resumed();
            }
        }
        public void Save()
        {
            FileInfo fi = new FileInfo(this.ConfigurationFile);
            if (!fi.Directory.Exists)
                fi.Directory.Create();
            if (fi.Exists)
                fi.Delete();

            using (System.IO.Stream s = System.IO.File.OpenWrite(this.ConfigurationFile))
            {
                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(this.GetType());
                serializer.Serialize(s, this);
            }
        }

        private void CountDbInserts(string tableName, object identityValue, object data = null)
        {
            foreach (var c in this.DbCountersSource)
            {
                if (c.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    if (identityValue != null)
                        c.Count(identityValue, DbCounter.CountType.Insert);
                    else
                    {
                        var d = data as Dictionary<string, object>;
                        if (d != null && d.ContainsKey(c.Identity))
                        {
                            c.Count(d[c.Identity], DbCounter.CountType.Insert);
                        }
                        else
                        {
                            foreach (var p in data.GetType().GetProperties())
                            {
                                if (p.Name.Equals(c.Identity, StringComparison.OrdinalIgnoreCase))
                                {
                                    c.Count(p.GetValue(data, null), DbCounter.CountType.Insert);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void CountDbUpdates(string tableName, string condition)
        {
            foreach (var c in this.DbCountersSource)
            {
                if (c.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    var rows = QueryFromDatabase(tableName, c.Identity, condition);
                    foreach (var row in rows)
                    {
                        c.Count(row[c.Identity], DbCounter.CountType.Update);
                    }
                }
            }
        }
        private void CountDbDeletes(string tableName, string condition)
        {
            foreach (var c in this.DbCountersSource)
            {
                if (c.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                {
                    var rows = QueryFromDatabase(tableName, c.Identity, condition);
                    foreach (var row in rows)
                    {
                        c.Count(row[c.Identity], DbCounter.CountType.Delete);
                    }
                }
            }
        }

        public string GetCountersSummary()
        {
            if (this.DbCountersSource.Count > 0)
            {
                var s = (from item in this.DbCountersSource
                         select item.Summary).ToArray();
                return string.Join("  ●  ", s);
            }

            return "No Counters";
        }

        public void ResetDbCounters()
        {
            foreach (var c in this.DbCountersSource)
            {
                c.Reset();
            }
        }

        #region About Events
        public event Action<double> ProgressChanged;
        public event Action Pausing;
        public event Action Paused;
        public event Action Resumed;
        public event ActionChangedEventHandler ActionChanged;
        public event ProjectStatusChangedEventHandler Started;
        public event ProjectStatusChangedEventHandler Canceling;
        public event ProjectStatusChangedEventHandler Canceled;
        public event ProjectStatusChangedEventHandler Done;
        public event ProjectStatusChangedEventHandler Completed;
        public event PropertyChangedEventHandler PropertyChanged;
        public event ErrorOccurredEventHandler ErrorOccurred;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void SetProgress(double value)
        {
            this.StartInfo.Progress = value;

            if (ProgressChanged != null)
            {
                ProgressChanged(value);
            }
        }
        protected void SetProgress(int[] values, int[] totals)
        {
            SetProgress(GetProgressValue(values, totals));
        }
        protected void SetProgress(List<Tuple<int, int>> valueTotalPair)
        {
            var values = valueTotalPair.Select(__ => __.Item1).ToArray();
            var totals = valueTotalPair.Select(__ => __.Item2).ToArray();

            SetProgress(values, totals);
        }
        public virtual void OnStarted()
        {
            ResetDbCounters();

            if (Started != null)
                Started(new ProjectStatusChangedEventArgs(this));

            File.AppendAllText(GetLogFile(), string.Format("===========================  Started at {0} ==========================={1}",
                DateTime.Now, Environment.NewLine));


            if (this.EnableBackupDb)
            {
                OnActionChanged(new ActionChangedEventArgs(this, "Backing up database...", ActionType.BackupDatabase));
                BackupDatabase();
            }
        }
        protected virtual void OnCanceling()
        {
            if (Canceling != null)
                Canceling(new ProjectStatusChangedEventArgs(this));
        }
        protected virtual void OnCanceled()
        {
            if (Canceled != null)
                Canceled(new ProjectStatusChangedEventArgs(this));

            File.AppendAllText(GetLogFile(), @"
--------------------- Canceled --------------------");
        }
        protected virtual void OnDone()
        {
            if (Done != null)
                Done(new ProjectStatusChangedEventArgs(this));
            this.Status = ProjectStatus.Done;

            File.AppendAllText(GetLogFile(), @"
--------------------- Done --------------------");
        }
        protected virtual void OnCompleted()
        {
            StartInfo.EndTime = DateTime.Now;

            if (this.Status != ProjectStatus.Canceled
                && this.Status != ProjectStatus.Done)
                this.Status = ProjectStatus.ErrorOccurred;

            if (Completed != null)
                Completed(new ProjectStatusChangedEventArgs(this));


            var totalTime = this.StartInfo.EndTime.Value.Subtract(this.StartInfo.StartTime);
            var flag = "";
            switch (this.Status)
            {
                case ProjectStatus.ErrorOccurred:
                    flag = "!";
                    break;
                case ProjectStatus.Canceled:
                    flag = "*";
                    break;
            }
            File.AppendAllText(GetLogFile(), string.Format(@"

{6}Completed at {1}		Total time: {0} 

================== {2} requests, {3} downloads, ~{4} affected rows, {5} errors ==================
",
                string.Format("{0} ({1} days)", totalTime.ToString(@"hh\:mm\:ss"), totalTime.ToString("%d")),
                this.StartInfo.EndTime,
                this.StartInfo.Requests,
                this.StartInfo.Downloads,
                this.StartInfo.AffectedRows,
                this.StartInfo.ErrorCount,
                flag
                ));
        }
        protected virtual void OnError(Exception e,
            object errorParameters = null,
            ActionTypeOnError todo = ActionTypeOnError.Default,
            [CallerMemberName] string methodName = null
            )
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} ---> Error M:{1} > [{2}]{3} ({4}){5}",
                DateTime.Now,
                methodName,
                e.GetType(),
                e.Message,
                errorParameters == null ? "" : errorParameters.UnionPropertiesWith(", "),
                Environment.NewLine);



            if (todo != ActionTypeOnError.Retry)
            {
                File.AppendAllText(GetLogFile(), sb.ToString());

                var ex = e;
                while (ex != null)
                {
                    var exStr = string.Format(@"
{0}
------------------------------------------------------------
{1}

",

                        ex.Message,
                        ex.StackTrace
                    );

                    sb.Append(exStr.Tab(1));
                    sb.Append(Environment.NewLine);

                    ex = ex.InnerException;
                }
                File.AppendAllText(GetLogFile(MessageRank.Error), sb.ToString());

                this.StartInfo.Errors.Add(e);
            }

            if (todo == ActionTypeOnError.Interrupted)
            {
                throw new AppException("Interrupted!", e);
            }

            if (ErrorOccurred != null)
            {
                var args = new ErrorOccurredEventArgs(
                            this, null, e, errorParameters, todo);

                todo = ErrorOccurred(args);
                switch (todo)
                {
                    case ActionTypeOnError.Default:
                        break;

                    case ActionTypeOnError.Interrupted:
                        File.AppendAllText(GetLogFile(),
                             string.Format("{0} ---> Interrupted!{1}{1}",
                                DateTime.Now, Environment.NewLine));
                        throw new AppException("Interrupted!", e);

                    case ActionTypeOnError.Retry:
                        break;

                    case ActionTypeOnError.Skipped:
                        File.AppendAllText(GetLogFile(),
                            string.Format("{0} ---> Skipped!{1}{1}",
                                DateTime.Now, Environment.NewLine));
                        break;
                }
            }
        }

        #endregion

        #region Virtual & Abstract Members
        public virtual Theme? Theme
        {
            get
            {
                return null;
            }
        }
        public virtual SwitcherCollection Switchers
        {
            get
            {
                return null;
            }
        }
        protected virtual List<DbCounter> DbCounters
        {
            get
            {
                return null;
            }
        }
        protected abstract void StartTask();
        public abstract string Url { get; }
        public abstract string Name { get; }

        #endregion

        private void LoadConfiguration()
        {
            if (!File.Exists(this.ConfigurationFile)) return;

            using (System.IO.Stream s = System.IO.File.OpenRead(this.ConfigurationFile))
            {
                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(this.GetType());
                var obj = serializer.Deserialize(s);

                foreach (PropertyInfo pi in this.GetType().GetProperties())
                {
                    if (pi.SetMethod != null)
                        pi.SetValue(this,
                            obj.GetType().GetProperty(pi.Name).GetValue(obj)
                            );
                }
            }
        }
        private void OnActionChanged(ActionChangedEventArgs e)
        {
            if (this.ActionChanged != null)
                this.ActionChanged(e);

            var log = GetLogFile();

            File.AppendAllText(log, string.Format("{0}{1}", e.DescriptionWithTime, Environment.NewLine));
        }
        private System.Threading.CancellationTokenSource _cancellationTokenSource;
        protected void CheckIsCancellationOrPauseRequested()
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();

            if (_inPause)
            {
                if (Paused != null)
                {
                    Paused();
                }

                while (_inPause)
                {

                }
            }
        }
        public string LastVersion
        {
            get
            {
                if (this.Versions != null && this.Versions.Count > 0)
                    return this.Versions[this.Versions.Count - 1];
                return " - ";
            }
        }
        public void Dispose()
        {
            if (_dbFactory != null)
            {
                _dbFactory.Dispose();
            }
        }

        #region Static Members
        private static List<Project> _all;

        /// <summary>
        /// Gets all the projects.
        /// </summary>
        /// <value>List</value>
        public static List<Project> All
        {
            get
            {
                if (_all == null)
                {
                    _all = new List<Project>();

                    Assembly assem = Assembly.LoadFrom(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "TaskUI.Tasks.dll"));

                    foreach (Type t in assem.GetTypes())
                    {
                        if (!t.IsAbstract && t.IsSubclassOf(typeof(Project)) && t.IsPublic)
                        {
                            var p = Activator.CreateInstance(t) as Project;
                            p.LoadConfiguration();
                            _all.Add(p);
                        }
                    }

                }

                return _all;
            }
        }

        public static event ActionChangedEventHandler DefaultActionChanged;

        #endregion Static Members

    }

}

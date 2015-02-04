using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskUI
{
    using Lib;
    using Net.Bndy;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Project _currentProject;

        public MainWindow()
        {
            InitializeComponent();

            Project.DefaultActionChanged += Project_DefaultActionChanged;

            this.ctlSwitchers.Items.Clear();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var model = this.DataContext as Project;
            model.Save();
            MessageBox.Show("OK");
        }
        private void btnTestDb_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject.IsDatabaseReady)
                MessageBox.Show("OK");
            else
                MessageBox.Show("The database is NOT ready.");
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject != null)
                System.Diagnostics.Process.Start(_currentProject.Url);
        }
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject == null) return;

            if ((_currentProject.Status & ProjectStatus.Started) != ProjectStatus.Started)
            {
                if (string.IsNullOrWhiteSpace(_currentProject.Workspace))
                {
                    MessageBox.Show("The project is not ready to be started. Workspace can not be empty!");
                    return;
                }

                if (_currentProject.RequireCheckConfig && !_currentProject.IsDatabaseReady)
                {
                    MessageBox.Show("The project is not ready to be started. Please specify database and can be connected!");
                    return;
                }


                if (MessageBox.Show("Are you sure you want to start this project?", "Start", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes)
                    != MessageBoxResult.Yes)
                {
                    return;
                }

                SetEventsForProject(_currentProject);
                _currentProject.Start();
            }
            else if ((_currentProject.Status & ProjectStatus.Started) == ProjectStatus.Started)
            {
                if (MessageBox.Show("Are you sure you want to cancel this task?", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.Yes)
                {
                    // Cancel task
                    _currentProject.Cancel();
                }
            }
        }

        private void SetEventsForProject(Project project)
        {
            project.ProgressChanged -= project_ProgressChanged;
            project.Started -= project_Started;
            project.Canceling -= project_Canceling;
            project.Canceled -= project_Canceled;
            project.Completed -= project_Completed;
            project.Done -= project_Done;
            project.ErrorOccurred -= project_ErrorOccurred;
            project.Pausing -= project_Pausing;
            project.Paused -= project_Paused;
            project.Resumed -= project_Resumed;

            project.ProgressChanged += project_ProgressChanged;
            project.Started += project_Started;
            project.Canceling += project_Canceling;
            project.Canceled += project_Canceled;
            project.Completed += project_Completed;
            project.Done += project_Done;
            project.ErrorOccurred += project_ErrorOccurred;
            project.Pausing += project_Pausing;
            project.Paused += project_Paused;
            project.Resumed += project_Resumed;
        }

        void project_Resumed()
        {
            SetPauseButtonStatus();

            UpdateUI(() =>
            {
                UpdateStatus("Resuming...", StatusBarFlag.Warning, DateTime.Now);
            });
        }
        void project_Pausing()
        {
            UpdateUI(() =>
            {
                this.IsEnabled = false;
                UpdateStatus("Pausing...", StatusBarFlag.Warning, DateTime.Now);
            });
        }
        void project_Paused()
        {
            SetPauseButtonStatus();

            UpdateUI(() =>
            {
                this.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
                UpdateStatus("Paused", StatusBarFlag.Warning, DateTime.Now);
            });
        }

        void project_ProgressChanged(double value)
        {
            UpdateUI(() =>
            {
                this.btnStart.Content = Regex.Replace(
                    this.btnStart.Content.ToString(),
                    @"\.{3}|\d{1,}\.\d{2}%",
                    string.Format("{0:0.00}%", value)
                    );
                this.ProjectProgress.Value = value;

                if (this.ProjectProgress.Visibility != System.Windows.Visibility.Visible)
                    this.ProjectProgress.Visibility = System.Windows.Visibility.Visible;
            });
        }
        void project_Done(ProjectStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                UpdateStatus("Done", StatusBarFlag.Normal, DateTime.Now);
            });
        }

        ActionTypeOnError project_ErrorOccurred(ErrorOccurredEventArgs e)
        {
            UpdateStatus(string.Format("Error: {0} ({1})",
                e.Description,
                e.ErrorParameters.UnionPropertiesWith(",")),
                StatusBarFlag.Error, DateTime.Now);

            string message = "";

            if (e.ErrorParameters != null)
            {
                message = string.Format(@"
{{0}}
===================================
{0}
    {1}",
                    e.Description,
                    e.ErrorParameters.UnionPropertiesWith(Environment.NewLine + "    ")
                );
            }
            else
            {
                message = "{0}";
            }

            ActionTypeOnError result = ActionTypeOnError.Default;
            MessageBoxResult dialogResult = MessageBoxResult.None;

            switch (e.TodoType)
            {
                case ActionTypeOnError.Skipped:
                    result = ActionTypeOnError.Skipped;
                    break;

                case ActionTypeOnError.Interrupted:
                    message = string.Format(message,
                        "Fatal Error");
                    dialogResult = MessageBox.Show(message, "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    result = ActionTypeOnError.Interrupted;
                    break;

                case ActionTypeOnError.Retry:
                    message = string.Format(message,
                        "The following error occurred. Try again?");
                    dialogResult = MessageBox.Show(message, "Retry?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    switch (dialogResult)
                    {
                        case MessageBoxResult.Yes:
                            result = ActionTypeOnError.Skipped;
                            break;

                        case MessageBoxResult.No:
                            result = ActionTypeOnError.Interrupted;
                            break;
                    }

                    break;

                case ActionTypeOnError.Default:
                    message = string.Format(message,
                        "The following error occurred.");
                    dialogResult = MessageBox.Show(message, "Continue?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    switch (dialogResult)
                    {
                        case MessageBoxResult.Yes:
                            result = ActionTypeOnError.Skipped;
                            break;

                        case MessageBoxResult.No:
                            result = ActionTypeOnError.Interrupted;
                            break;
                    }
                    break;
            }

            return result;
        }

        void project_Completed(ProjectStatusChangedEventArgs e)
        {
            e.Project.Dispose();
            Project_DefaultActionChanged(new ActionChangedEventArgs(e.Project,
                e.Status.ToString(), ActionType.None));

            UpdateUI(() =>
            {
                this.Icon = BitmapFrame.Create(
                    new Uri("pack://application:,,,/images/logo.ico")); ;
                this.StatusIcon.Visibility = System.Windows.Visibility.Collapsed;

                this.ProjectList.IsEnabled = true;
                this.btnSave.IsEnabled = true;
                this.btnStart.IsEnabled = true;
                this.btnTestDb.IsEnabled = true;
                this.btnStart.Content = string.Format("{0} ---> {1}       Click to Restart",
                    e.Project.StartInfo.StartTime,
                    e.Project.StartInfo.EndTime);

                this.ProjectProgress.Visibility = System.Windows.Visibility.Collapsed;
                this.btnPause.Visibility = System.Windows.Visibility.Collapsed;

                UpdateStatus("Completed.", StatusBarFlag.Normal);
                SetPauseButtonStatus();

            });
        }

        void project_Canceled(ProjectStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                this.btnPause.IsEnabled = true;
                this.btnStart.IsEnabled = true;
                this.StatusIcon.Visibility = System.Windows.Visibility.Collapsed;

                UpdateStatus(string.Format("Canceled at {0}", DateTime.Now),
                     StatusBarFlag.Warning, DateTime.Now);
            });
        }

        void project_Canceling(ProjectStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                this.btnStart.Content = "Canceling...";
                this.btnStart.IsEnabled = false;
                this.btnPause.IsEnabled = false;
            });
        }

        void project_Started(ProjectStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                this.Icon = BitmapFrame.Create(
                    new Uri("pack://application:,,,/images/busy.ico"));
                this.ProjectList.IsEnabled = false;
                this.btnSave.IsEnabled = false;
                this.btnTestDb.IsEnabled = false;
                this.btnStart.Content = string.Format("{0} ---> ...       Click to Cancel",
                    e.Project.StartInfo.StartTime);

                this.LogsText.Blocks.Clear();
                UpdateStatus("Starting...", StatusBarFlag.Normal,
                    DateTime.Now);

                this.btnPause.Visibility = System.Windows.Visibility.Visible;

                if (_currentProject.DbCountersSource.Count > 0)
                {
                    this.CountersPanel.Visibility = System.Windows.Visibility.Visible;
                    this.CountersSummary.Text = _currentProject.GetCountersSummary();
                }
            });
        }
        private void ProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ProjectProgress.Visibility = System.Windows.Visibility.Collapsed;

            _currentProject = this.ProjectList.SelectedItem as Project;

            if (_currentProject == null) return;

            this.btnStart.IsEnabled = true;
            this.DataContext = _currentProject;

            this.Taskbar_Title.Text = _currentProject.Name;

            if (_currentProject.Theme != null)
            {
                var theme = this.TryFindResource("ThemeBackground");
                if (theme != null && !string.IsNullOrWhiteSpace(_currentProject.Theme.Value.BackgroundColor))
                {
                    this.Resources["ThemeBackground"] = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(_currentProject.Theme.Value.BackgroundColor));
                }
                theme = this.TryFindResource("ThemeForeground");
                if (theme != null && !string.IsNullOrEmpty(_currentProject.Theme.Value.ForegroundColor))
                {
                    this.Resources["ThemeForeground"] = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(_currentProject.Theme.Value.ForegroundColor));
                }
            }

            if (string.IsNullOrWhiteSpace(_currentProject.ConnectionString)
                || string.IsNullOrWhiteSpace(_currentProject.Workspace))
                UpdateStatus("Some fields have not been specified!",
                    StatusBarFlag.Error);
            else
                UpdateStatus("Ready", StatusBarFlag.Normal);

            this.LogsText.Blocks.Clear();
            this.CountersPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.Title = _currentProject.Name;
            this.btnStart.Content = "Start";
        }

        private void btnFolderDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select a folder to place files";

            if (!string.IsNullOrWhiteSpace(_currentProject.Workspace))
                dialog.SelectedPath = _currentProject.Workspace;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _currentProject.Workspace = dialog.SelectedPath;
            }
        }

        private void Workspace_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var folder = ((TextBox)sender).Text;
            if (!string.IsNullOrWhiteSpace(folder))
            {
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                System.Diagnostics.Process.Start(folder);
            }
        }

        private void UpdateUI(Action action)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (action != null)
                    action();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        private void UpdateStatus(string message, StatusBarFlag flag,
            DateTime? dt = null)
        {
            UpdateUI(() =>
            {
                this.StatusMessage.Text = message;
                switch (flag)
                {
                    case StatusBarFlag.Error:
                        this.StatusBar.Foreground = Brushes.Red;
                        break;

                    case StatusBarFlag.Normal:
                        this.StatusBar.Foreground = Brushes.Black;
                        break;

                    case StatusBarFlag.Warning:
                        this.StatusBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CA5100"));
                        break;
                }

                // logs
                if (dt.HasValue)
                {
                    Paragraph p = new Paragraph();
                    p.Margin = new Thickness(5, 5, 5, 0);
                    p.Inlines.Add(new Run(string.Format(@"{0} ---> {1}",
                        dt ?? DateTime.Now, message)));
                    TextRange tr = new TextRange(p.ContentStart, p.ContentEnd);
                    switch (flag)
                    {
                        case StatusBarFlag.Error:
                            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
                            tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                            break;

                        case StatusBarFlag.Warning:
                            tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CA5100")));
                            break;

                        default:
                            break;
                    }
                    this.LogsText.Blocks.Add(p);
                    this.Logs.ScrollToEnd();
                }
            });


        }

        private void Project_DefaultActionChanged(ActionChangedEventArgs e)
        {
            UpdateStatus(e.Description, StatusBarFlag.Normal, e.ActionTime);

            UpdateUI(() =>
            {
                var titleFormat = e.Project.Name;
                if (e.Project.StartInfo.Requests > 0)
                    titleFormat += string.Format(" - {0} requests", e.Project.StartInfo.Requests);
                if (e.Project.StartInfo.Downloads > 0)
                    titleFormat += string.Format(", {0} downloads", e.Project.StartInfo.Downloads);
                if (e.Project.StartInfo.AffectedRows > 0)
                    titleFormat += string.Format(", ~{0} affected rows,", e.Project.StartInfo.AffectedRows);
                if (e.Project.StartInfo.ErrorCount > 0)
                    titleFormat += string.Format(", {0} errors", e.Project.StartInfo.ErrorCount);

                this.Title = titleFormat;
                this.CountersSummary.Text = _currentProject.GetCountersSummary();

                if (!_currentProject.RequireHighPerformance)
                {
                    ImageSource imgSource = this.StatusIcon.Source;
                    switch (e.ActionType)
                    {
                        case ActionType.Request:
                            imgSource = new BitmapImage(new Uri("images\\net.png", UriKind.Relative));
                            break;

                        case ActionType.Save2Database:
                            imgSource = new BitmapImage(new Uri("images\\db.png", UriKind.Relative));
                            break;

                        case ActionType.Save2Disk:
                            imgSource = new BitmapImage(new Uri("images\\disk.png", UriKind.Relative));
                            break;

                        case ActionType.Analyze:
                            imgSource = new BitmapImage(new Uri("images\\analysis.png", UriKind.Relative)); ;
                            break;


                    }
                    if (imgSource != null)
                    {

                        this.StatusIcon.Visibility = System.Windows.Visibility.Visible;
                        this.StatusIcon.Source = imgSource;

                    }
                    else
                    {
                        this.StatusIcon.Visibility = System.Windows.Visibility.Hidden;
                    }
                }
            });
        }


        private enum StatusBarFlag
        {
            Normal,
            Warning,
            Error
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_currentProject != null && _currentProject.Status == ProjectStatus.Started)
            {
                if (MessageBox.Show("Current project started.\r\nAre you sure you want to stop it and exit?",
                    "Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning)
                    == MessageBoxResult.Yes)
                {
                    _currentProject.Completed += __ =>
                    {
                        UpdateUI(() =>
                        {
                            _currentProject.Dispose();
                            this.Close();
                        });
                    };
                    _currentProject.Cancel();
                    e.Cancel = true;
                    return;
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            Application.Current.Shutdown();
        }

        private void StatusMessage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.Logs.Visibility == System.Windows.Visibility.Visible)
            {
                this.Logs.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.Logs.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ProjectVariable_Click(object sender, RoutedEventArgs e)
        {
            SwitcherCollection sc = this.ctlSwitchers.ItemsSource as SwitcherCollection;
            if (sc != null)
            {
                var cb = sender as CheckBox;

                if (!cb.IsEnabled)
                    e.Handled = true;

                var cbData = cb.DataContext as Switcher;

                List<CheckBox> lst = FindCheckBoxes(this.ctlSwitchers, cbData.Name);
                foreach (var c in lst)
                {
                    c.IsEnabled = cb.IsChecked ?? false;
                    if (cb.IsChecked.HasValue && !cb.IsChecked.Value)
                        c.IsChecked = false;
                }
            }
        }

        public List<CheckBox> FindCheckBoxes(DependencyObject obj, string name)
        {
            DependencyObject child = null;
            List<CheckBox> lst = new List<CheckBox>();
            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);
                if (child is CheckBox && ((Switcher)((CheckBox)child).Tag).DependTo == name)
                {
                    lst.Add((CheckBox)child);
                }
                else
                {
                    lst.AddRange(FindCheckBoxes(child, name));
                }
            }
            return lst;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
            }
            else
            {
                this.ShowInTaskbar = true;
            }
        }

        private void Advance_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.PanelProxy.Visibility == System.Windows.Visibility.Collapsed)
                this.PanelProxy.Visibility = System.Windows.Visibility.Visible;
            else
                this.PanelProxy.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject != null)
            {
                if ((_currentProject.Status & ProjectStatus.Paused) == ProjectStatus.Paused)
                {
                    _currentProject.Resume();
                }
                else
                {
                    _currentProject.Pause();
                }
                SetPauseButtonStatus();
            }
        }

        private void SetPauseButtonStatus()
        {
            if (_currentProject != null)
            {
                UpdateUI(() =>
                {
                    Image img = this.btnPause.Content as Image;
                    if ((_currentProject.Status & ProjectStatus.Paused) == ProjectStatus.Paused)
                    {
                        img.Source = new BitmapImage(new Uri("images/resume.png", UriKind.Relative));
                        this.btnPause.ToolTip = "Resume";
                    }
                    else
                    {
                        img.Source = new BitmapImage(new Uri("images/pause.png", UriKind.Relative));
                        this.btnPause.ToolTip = "Pause";
                    }
                });
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ProjectList.ItemsSource = Project.All;
        }

        private void BrowseUrl_Click(object sender, RoutedEventArgs e)
        {
            var url = ((FrameworkElement)sender).Tag.ToString();
            System.Diagnostics.Process.Start(url);
        }
    }
}

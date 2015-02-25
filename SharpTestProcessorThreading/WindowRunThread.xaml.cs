using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SystemProcessorInfo
{
	/// <summary>
	/// Interaction logic for WindowRunThreads.xaml
	/// </summary>
	public partial class WindowRunThread : Window
	{
		readonly WindowRunThreadModel _windowRunThreadModel = new WindowRunThreadModel();

		private DateTime _dateTimeStarted;

		public WindowRunThread()
		{
			InitializeComponent();
			DataContext = _windowRunThreadModel;
		}

		// ******************************************************************
		public void Show(int numberOfThread, int millisecs, bool useThreadPool)
		{
			WindowRunThreadModel model = Model;
			model.NumberOfThread = numberOfThread;
			Model.Millisecs = millisecs;
			Model.UseThreadPool = useThreadPool;



			Model.CollThreadInfo = new ObservableCollection<ThreadInfo>();

			for (int n = 0; n < Model.NumberOfThread; n++)
			{
				var ti = new ThreadInfo();
				Model.CollThreadInfo.Add(ti);
			}

			Task.Run(()=>Model.StartThreads());

			this.Show();

			Dispatcher.BeginInvoke(new Action(UpdateStatus), DispatcherPriority.ContextIdle);
			_dateTimeStarted = DateTime.Now;
		}

		// ******************************************************************
		private async void UpdateStatus()
		{
			foreach (ThreadInfo ti in Model.CollThreadInfo)
			{
				ti.RefreshInterface();
			}

			await Task.Delay(100);

			if (Model.IsRunning)
			{
				Dispatcher.BeginInvoke(new Action(
					() => Dispatcher.BeginInvoke(new Action(UpdateStatus), DispatcherPriority.Background)
					), DispatcherPriority.ContextIdle);

				if ((DateTime.Now.TimeOfDay - _dateTimeStarted.TimeOfDay).Milliseconds > Model.Millisecs)
				{
					Model.IsRunning = false;
				}
			}
		}

		// ******************************************************************
		public WindowRunThreadModel Model
		{
			get { return _windowRunThreadModel; }
		}

		// ******************************************************************
		private void DataGridOnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyName == "Dispatcher")
			{
				e.Cancel = true;
			}
		}

		// ******************************************************************
		private void WindowRunThreadOnClosed(object sender, EventArgs e)
		{
			Model.IsRunning = false;
		}

		// ******************************************************************
	}
}

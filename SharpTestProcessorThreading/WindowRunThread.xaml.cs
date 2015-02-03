using System;
using System.Collections.Generic;
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

namespace SystemProcessorInfo
{
	/// <summary>
	/// Interaction logic for WindowRunThreads.xaml
	/// </summary>
	public partial class WindowRunThread : Window
	{
		readonly WindowRunThreadModel _windowRunThreadModel = new WindowRunThreadModel();

		public WindowRunThread()
		{
			InitializeComponent();
			DataContext = _windowRunThreadModel;
		}

		public void Show(int numberOfThread, int millisecs, bool useThreadPool)
		{
			WindowRunThreadModel model = Model;
			model.NumberOfThread = numberOfThread;
			Model.Millisecs = millisecs;
			Model.UseThreadPool = useThreadPool;

			Task.Run(()=>Model.StartThreads());

			this.Show();
		}

		public WindowRunThreadModel Model
		{
			get { return _windowRunThreadModel; }
		}

		private void DataGridOnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyName == "Dispatcher")
			{
				e.Cancel = true;
			}
		}
	}
}

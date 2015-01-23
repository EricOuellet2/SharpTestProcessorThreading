using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SystemProcessorInfo
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private MainWindowModel Model { get; set; }

		// ******************************************************************
		public MainWindow()
		{
			InitializeComponent();
			Model = new MainWindowModel();
			DataContext = Model;
		}

		// ******************************************************************
		private void CmdStartThreadsClick(object sender, RoutedEventArgs e)
		{
			int numberOfThreads = int.Parse(TextBoxThreadCount.Text);
			int numberOfSeconds = int.Parse(TextBoxSeconds.Text);
			bool useThreadPool = CheckBoxTestThreadPool.IsChecked == true;
			
			var until = DateTime.Now.AddSeconds(numberOfSeconds);

			if (useThreadPool)
			{
				Parallel.For(0, numberOfThreads, (n) => LooseYourTime(until));
			}
			else
			{
				for (int n = 0; n < numberOfThreads; n++)
				{
					var thread = new Thread(() => LooseYourTime(until));
					thread.Start();
				}
			}
		}

		// ******************************************************************
		private void LooseYourTime(DateTime until)
		{
			int i = 1;
			while (DateTime.Now < until)
			{
				i = i + 1;
				if (i > 1000000)
				{
					i = 0;
				}
			}
		}

		// ******************************************************************
	}
}

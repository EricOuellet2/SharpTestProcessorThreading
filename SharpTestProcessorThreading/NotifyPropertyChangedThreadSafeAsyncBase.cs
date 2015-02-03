using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;

// using System.Windows.Forms;

// using Microsoft.CSharp.RuntimeBinder;

// ATTENTION: Can only be used with Framework 4.0 and up


namespace SystemProcessorInfo
{
	[Serializable]
	public class NotifyPropertyChangedThreadSafeAsyncBase : INotifyPropertyChanged
	{
		// ******************************************************************
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		[field: NonSerialized]
		private Dispatcher _dispatcher = null;

		// ******************************************************************
		[Browsable(false)]
		[XmlIgnore]
		public Dispatcher Dispatcher
		{
			get
			{
				if (_dispatcher == null)
				{
					if (Application.Current != null)
					{
						_dispatcher = Application.Current.Dispatcher;
					}
				}

				return _dispatcher;
			}
			set
			{
				_dispatcher = value;
			}
		}

		// ******************************************************************
		protected void NotifyPropertyChanged(String propertyName)
		{
			try
			{
				PropertyChangedEventHandler propertyChanged = PropertyChanged;
				if (propertyChanged != null)
				{
					if (Dispatcher.CheckAccess())
					{
						propertyChanged(this, new PropertyChangedEventArgs(propertyName));
					}
					else
					{
						Dispatcher.BeginInvoke(new Action(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName))));
					}
				}
			}
			catch (TaskCanceledException ex) // Prevent MT error when closing app...
			{

			}
		}

		// ******************************************************************
		protected void NotifyPropertyChanged<T2>(Expression<Func<T2>> propAccess)
		{
			try
			{
				PropertyChangedEventHandler propertyChanged = PropertyChanged;
				if (propertyChanged != null)
				{
					var asMember = propAccess.Body as MemberExpression;
					if (asMember == null)
						return;

					string propertyName = asMember.Member.Name;

					if (Dispatcher != null)
					{
						if (Dispatcher.CheckAccess())
						{
							propertyChanged(this, new PropertyChangedEventArgs(propertyName));
						}
						else
						{
							Dispatcher.BeginInvoke(new Action(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName))));
						}
					}
				}
			}
			catch (TaskCanceledException ex) // Prevent MT error when closing app...
			{
				
			}
		}

		// ******************************************************************
		//protected void NotifyPropertyChangedNowInSameThread<T2>(Expression<Func<T2>> propAccess)
		//{
		//	PropertyChangedEventHandler propertyChanged = PropertyChanged;
		//	if (propertyChanged != null)
		//	{
		//		var asMember = propAccess.Body as MemberExpression;
		//		if (asMember == null)
		//			return;

		//		string propertyName = asMember.Member.Name;

		//		propertyChanged(this, new PropertyChangedEventArgs(propertyName));
		//	}
		//}

		// ******************************************************************
		protected void NotifyPropertyChangedNow<T2>(Expression<Func<T2>> propAccess)
		{
			try
			{
				PropertyChangedEventHandler propertyChanged = PropertyChanged;
				if (propertyChanged != null)
				{
					var asMember = propAccess.Body as MemberExpression;
					if (asMember == null)
						return;

					string propertyName = asMember.Member.Name;

					if (Dispatcher != null)
					{
						if (Dispatcher.CheckAccess())
						{
							propertyChanged(this, new PropertyChangedEventArgs(propertyName));
						}
						else
						{
							Dispatcher.Invoke(new Action(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName))));
						}
					}
				}
			}
			catch (TaskCanceledException ex) // Prevent MT error when closing app...
			{
				
			}

		}
		// ******************************************************************
	}
}
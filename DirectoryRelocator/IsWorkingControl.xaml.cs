using System.Windows;

namespace DirectoryRelocator
{
	public partial class IsWorkingControl
	{
		public static readonly DependencyProperty IsWorkingProperty = DependencyProperty.Register("IsWorking", typeof (bool), typeof (IsWorkingControl), new PropertyMetadata(default(bool)));

		public IsWorkingControl()
		{
			InitializeComponent();
		}

		public bool IsWorking
		{
			get { return (bool) GetValue(IsWorkingProperty); }
			set { SetValue(IsWorkingProperty, value); }
		}
	}
}
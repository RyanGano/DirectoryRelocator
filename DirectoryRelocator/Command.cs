using System;
using System.Windows.Input;

namespace DirectoryRelocator
{
	public class Command : ICommand
	{
		private readonly Action m_execute;
		private readonly Func<bool> m_canExecute;

		public Command(Action mExecute, Func<bool> mCanExecute)
		{
			m_execute = mExecute;
			m_canExecute = mCanExecute;
		}

		public bool CanExecute(object parameter)
		{
			return m_canExecute();
		}

		public void Execute(object parameter)
		{
			m_execute();
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged.Invoke(this, null);
		}

		public event EventHandler CanExecuteChanged;
	}
}
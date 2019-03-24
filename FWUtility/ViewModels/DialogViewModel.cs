namespace FWUtility.ViewModels
{
	using System.Windows;
	using Caliburn.Micro;

	public class DialogViewModel : Screen
	{
		private string _firstMessage;
		private string _secondMessage;
		private string _thirdMessage;
		private Visibility _yesNoVisibility;
		private Visibility _okVisibility;
		private Visibility _cancelVisibility;

		/// <summary>
		/// Тип диалогового окна
		/// </summary>
		public enum DialogType
		{
			YES_NO,
			YES_CANCEL_NO,
			OK
		}

		public DialogViewModel(
			string firstMessage, DialogType dialogType,
			string secondMessage = null, string thirdMessage = null)
		{
			FirstMessage = firstMessage;
			SecondMessage = secondMessage;
			ThirdMessage = thirdMessage;

			switch (dialogType)
			{
				case DialogType.YES_NO:
					YesNoVisibility = Visibility.Visible;
					OkVisibility = Visibility.Hidden;
					CancelVisibility = Visibility.Hidden;
					return;
				case DialogType.YES_CANCEL_NO:
					YesNoVisibility = Visibility.Visible;
					OkVisibility = Visibility.Hidden;
					CancelVisibility = Visibility.Visible;
					return;
				case DialogType.OK:
					YesNoVisibility = Visibility.Hidden;
					OkVisibility = Visibility.Visible;
					CancelVisibility = Visibility.Hidden;
					return;
			}
		}

		public void YesAction()
		{
			TryClose(true);
		}

		public void OkAction()
		{
			TryClose(true);
		}

		public void CancelAction()
		{
			TryClose(false);
		}

		public void NoAction()
		{
			TryClose(false);
		}

		public string FirstMessage
		{
			get => _firstMessage;
			set
			{
				_firstMessage = value;
				NotifyOfPropertyChange(() => FirstMessage);
			}
		}

		public string SecondMessage
		{
			get => _secondMessage;
			set
			{
				_secondMessage = value;
				NotifyOfPropertyChange(() => SecondMessage);
			}
		}

		public string ThirdMessage
		{
			get => _thirdMessage;
			set
			{
				_thirdMessage = value;
				NotifyOfPropertyChange(() => ThirdMessage);
			}
		}

		public Visibility YesNoVisibility
		{
			get => _yesNoVisibility;
			set
			{
				_yesNoVisibility = value;
				NotifyOfPropertyChange(() => YesNoVisibility);
			}
		}

		public Visibility OkVisibility
		{
			get => _okVisibility;
			set
			{
				_okVisibility = value;
				NotifyOfPropertyChange(() => OkVisibility);
			}
		}

		public Visibility CancelVisibility
		{
			get => _cancelVisibility;
			set
			{
				_cancelVisibility = value;
				NotifyOfPropertyChange(() => CancelVisibility);
			}
		}
	}
}

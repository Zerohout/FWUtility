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
		private Visibility _errorVisibility;

		private Visibility _messageVisibility;

		/// <summary>
		/// Тип диалогового окна
		/// </summary>
		public enum DialogType
		{
			YES_NO,
			YES_CANCEL_NO,
			OK,
			ERROR
		}

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="firstMessage">Первое сообщение</param>
		/// <param name="dialogType">Режим</param>
		/// <param name="secondMessage">Недоступно</param>
		/// <param name="thirdMessage">Недоступно</param>
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
					MessageVisibility = Visibility.Visible;
					ErrorVisibility = Visibility.Hidden;
					return;
				case DialogType.YES_CANCEL_NO:
					YesNoVisibility = Visibility.Visible;
					OkVisibility = Visibility.Hidden;
					CancelVisibility = Visibility.Visible;
					MessageVisibility = Visibility.Visible;
					ErrorVisibility = Visibility.Hidden;
					return;
				case DialogType.OK:
					YesNoVisibility = Visibility.Hidden;
					OkVisibility = Visibility.Visible;
					CancelVisibility = Visibility.Hidden;
					MessageVisibility = Visibility.Visible;
					ErrorVisibility = Visibility.Hidden;
					return;
				case DialogType.ERROR:
					YesNoVisibility = Visibility.Hidden;
					CancelVisibility = Visibility.Hidden;
					OkVisibility = Visibility.Visible;
					MessageVisibility = Visibility.Hidden;
					ErrorVisibility = Visibility.Visible;
					return;
			}
		}

		#region Buttons

		/// <summary>
		/// Кнопка "Да, конечно"
		/// </summary>
		public void Yes()
		{
			TryClose(true);
		}

		/// <summary>
		/// Кнопка "Я понял"
		/// </summary>
		public void Ok()
		{
			TryClose();
		}

		/// <summary>
		/// Кнопка "Я передумал"
		/// </summary>
		public void Cancel()
		{
			TryClose(false);
		}

		/// <summary>
		/// Кнопка "Нет, что вы"
		/// </summary>
		public void No()
		{
			TryClose(false);
		}

		#endregion

		#region Properties

		/// <summary>
		/// Первое сообщение
		/// </summary>
		public string FirstMessage
		{
			get => _firstMessage;
			set
			{
				_firstMessage = value;
				NotifyOfPropertyChange(() => FirstMessage);
			}
		}

		/// <summary>
		/// Второе сообщение
		/// </summary>
		public string SecondMessage
		{
			get => _secondMessage;
			set
			{
				_secondMessage = value;
				NotifyOfPropertyChange(() => SecondMessage);
			}
		}

		/// <summary>
		/// Третье сообщение
		/// </summary>
		public string ThirdMessage
		{
			get => _thirdMessage;
			set
			{
				_thirdMessage = value;
				NotifyOfPropertyChange(() => ThirdMessage);
			}
		}

		/// <summary>
		/// Свойство Visibility в режиме Yes_No и Yes_Cancel_No
		/// </summary>
		public Visibility YesNoVisibility
		{
			get => _yesNoVisibility;
			set
			{
				_yesNoVisibility = value;
				NotifyOfPropertyChange(() => YesNoVisibility);
			}
		}

		/// <summary>
		/// Свойство Visibility в режиме Ok
		/// </summary>
		public Visibility OkVisibility
		{
			get => _okVisibility;
			set
			{
				_okVisibility = value;
				NotifyOfPropertyChange(() => OkVisibility);
			}
		}

		/// <summary>
		/// Свойство Visibility в режиме Yes_Cancel_No
		/// </summary>
		public Visibility CancelVisibility
		{
			get => _cancelVisibility;
			set
			{
				_cancelVisibility = value;
				NotifyOfPropertyChange(() => CancelVisibility);
			}
		}

		public Visibility MessageVisibility
		{
			get => _messageVisibility;
			set
			{
				_messageVisibility = value;
				NotifyOfPropertyChange(() => MessageVisibility);
			}
		}

		public Visibility ErrorVisibility
		{
			get => _errorVisibility;
			set
			{
				_errorVisibility = value;
				NotifyOfPropertyChange(() => ErrorVisibility);
			}
		}

		#endregion

	}
}

namespace FWUtility.ViewModels
{
	using System.Data.Entity;
	using System.Linq;
	using System.Windows;
	using Caliburn.Micro;
	using Database;
	using Models;
	using static Helpers.Helper;

	public class EditableViewModel : Screen
	{
		private Account _editingAccount;

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="editingAccount">Изменяемый аккаунт</param>
		/// <param name="currentState">Текущий режим</param>
		public EditableViewModel(Account editingAccount, EditingState currentState)
		{
			_editingAccount = editingAccount;

			if (currentState == EditingState.CREATING)
			{
				CreatingVisibility = Visibility.Visible;
				EditingVisibility = Visibility.Hidden;
			}
			else
			{
				CreatingVisibility = Visibility.Hidden;
				EditingVisibility = Visibility.Visible;
			}
		}

		#region Buttons

		/// <summary>
		/// Кнопка Создать
		/// </summary>
		public async void CreateAccount()
		{
			using (var ctx = new FWUDbContext())
			{
				ctx.Accounts.Add(_editingAccount);
				await ctx.SaveChangesAsync();
			}
			
			(Parent as MainViewModel)?.LoadAccountData();
		}

		public bool CanCreateAccount => CreateAccountValidation;


		/// <summary>
		/// Кнопка Изменить
		/// </summary>
		public async void EditAccount()
		{
			using (var ctx = new FWUDbContext())
			{
				ctx.Entry(_editingAccount).State = EntityState.Modified;
				await ctx.SaveChangesAsync();
			}

			((MainViewModel)Parent).LoadAccountData();
		}

		public bool CanEditAccount => EditAccountValidation;

		/// <summary>
		/// Кнопка Удалить
		/// </summary>
		public async void RemoveAccount()
		{
			var wm = new WindowManager();
			var dvm = new DialogViewModel(
				firstMessage: "Вы действительно хотите удалить выбранный аккаунт?",
				dialogType: DialogViewModel.DialogType.YES_NO);

			if (wm.ShowDialog(dvm) ?? false)
			{
				using (var ctx = new FWUDbContext())
				{
					ctx.Entry(((MainViewModel)Parent).SelectedAccount).State = EntityState.Deleted;
					await ctx.SaveChangesAsync();
				}

				((MainViewModel)Parent).LoadAccountData();
			}
		}

		public bool CanRemoveAccount => 
			ParentAccounts.Any(p => p.Name == _editingAccount.Name)
			&& ParentAccounts.Any(p => p.Email == _editingAccount.Email)
			&& ParentAccounts.Any(p => p.Password == _editingAccount.Password);

		/// <summary>
		/// Кнопка Отменить
		/// </summary>
		public void Cancel() { ((MainViewModel)Parent).SelectedAccount = null; }

		#endregion
		
		#region Properties

		/// <summary>
		/// Редактируемый аккаунт
		/// </summary>
		public Account EditingAccount
		{
			get => _editingAccount;
			set
			{
				_editingAccount = value;
				NotifyOfPropertyChange(() => EditingAccount);
			}
		}

		/// <summary>
		/// Список аккаунтов в родительской модели представления
		/// </summary>
		private BindableCollection<Account> ParentAccounts => (Parent as MainViewModel)?.Accounts;

		/// <summary>
		/// Свойство Visibility в режиме Создания
		/// </summary>
		public Visibility CreatingVisibility { get; set; }

		/// <summary>
		/// Свойство Visibility в режиме Редактирования
		/// </summary>
		public Visibility EditingVisibility { get; set; }

		/// <summary>
		/// Определение изменения данных аккаунта
		/// </summary>
		private bool EditAccountValidation
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_editingAccount.Name)
				    || string.IsNullOrWhiteSpace(_editingAccount.Email)
				    || string.IsNullOrWhiteSpace(_editingAccount.Password)
				    || _editingAccount.Name == CreatingName)
				{
					return false;
				}

				if (((MainViewModel)Parent).SelectedAccount.Name == _editingAccount.Name
				    && ((MainViewModel)Parent).SelectedAccount.Email == _editingAccount.Email
				    && ((MainViewModel)Parent).SelectedAccount.Password == _editingAccount.Password)
				{
					return false;
				}

				return true;
			}
		}

		private bool CreateAccountValidation
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_editingAccount.Name)
				    || string.IsNullOrWhiteSpace(_editingAccount.Email)
				    || string.IsNullOrWhiteSpace(_editingAccount.Password)
				    || _editingAccount.Name == CreatingName)
				{
					return false;
				}

				if (ParentAccounts.Any(p => p.Name == _editingAccount.Name) ||
				    ParentAccounts.Any(p => p.Email == _editingAccount.Email))
				{
					return false;
				}

				return true;
			}
		}

		#endregion

		#region Actions

		public void TextChanged()
		{
			NotifyOfPropertyChange(() => CanCreateAccount);
			NotifyOfPropertyChange(() => CanEditAccount);
			NotifyOfPropertyChange(() => CanRemoveAccount);
		}

		#endregion

	}
}

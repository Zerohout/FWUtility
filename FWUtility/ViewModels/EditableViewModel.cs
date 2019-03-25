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
		private BindableCollection<Account> ParentAccounts => (Parent as MainViewModel)?.Accounts;

		public Visibility CreatingVisibility { get; set; }
		public Visibility EditingVisibility { get; set; }

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

		#region Buttons Actions

		public async void CreateAccount()
		{
			using (var ctx = new FWUDbContext())
			{
				ctx.Accounts.Add(_editingAccount);
				await ctx.SaveChangesAsync();
			}
			(Parent as MainViewModel)?.LoadData();
		}

		public bool CanCreateAccount => AccountValidation;

		public async void EditAccount()
		{
			((MainViewModel)Parent).SelectedAccount.Name = _editingAccount.Name;
			((MainViewModel)Parent).SelectedAccount.Email = _editingAccount.Email;
			((MainViewModel)Parent).SelectedAccount.Password = _editingAccount.Password;

			using (var ctx = new FWUDbContext())
			{
				ctx.Entry(((MainViewModel)Parent).SelectedAccount).State = EntityState.Modified;
				await ctx.SaveChangesAsync();
			}

			((MainViewModel)Parent).LoadData();
		}

		public bool CanEditAccount => AccountValidation;

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

				((MainViewModel)Parent).LoadData();
			}
		}

		public void Cancel() { ((MainViewModel)Parent).SelectedAccount = null; }

		#endregion

		public void TextChanged()
		{
			NotifyOfPropertyChange(() => CanCreateAccount);
			NotifyOfPropertyChange(() => CanEditAccount);
		}

		private bool AccountValidation =>
			(!string.IsNullOrWhiteSpace(_editingAccount.Name)
			 && !string.IsNullOrWhiteSpace(_editingAccount.Email)
			 && !string.IsNullOrWhiteSpace(_editingAccount.Password))
			&& (ParentAccounts.All(p => p.Name != _editingAccount.Name)
				&& ParentAccounts.All(p => p.Email != _editingAccount.Email))
			&& _editingAccount.Name != CreateName;

		public Account EditingAccount
		{
			get => _editingAccount;
			set
			{
				_editingAccount = value;
				NotifyOfPropertyChange(() => EditingAccount);
			}
		}
	}
}

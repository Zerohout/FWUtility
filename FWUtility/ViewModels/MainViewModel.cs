namespace FWUtility.ViewModels
{
	using System.Data.Entity;
	using System.Linq;
	using System.Windows;
	using Caliburn.Micro;
	using Database;
	using Model;
	using static Helpers.Helper;

	public class MainViewModel : Conductor<Screen>.Collection.OneActive
	{
		private BindableCollection<Account> _accounts = new BindableCollection<Account>();
		private Account _selectedAccount;


		public MainViewModel()
		{
			LoadData();
		}

		protected override void OnInitialize()
		{
			base.OnInitialize();
		}

		public async void LoadData()
		{
			Accounts.Clear();
			using (var ctx = new FWUDbContext())
			{
				await ctx.Accounts.LoadAsync();
				Accounts.AddRange(ctx.Accounts.Local);
			}

			if (Accounts.All(a => a.Name != CreateName))
			{
				Accounts.Add(CreatingAccount);
			}
		}

		public void Start()
		{

		}

		public void SelectionChanged()
		{
			ActiveItem?.TryClose();

			if (_selectedAccount == null)
			{
				return;
			}
			
			ActiveItem = _selectedAccount.Name == CreateName
				? new EditableViewModel(new Account(), EditingState.CREATING)
				: new EditableViewModel(new Account
				{
					Name = _selectedAccount.Name,
					Email = _selectedAccount.Email,
					Password = _selectedAccount.Password
				}, EditingState.EDITING);
		}

		public Account SelectedAccount
		{
			get => _selectedAccount;
			set
			{
				_selectedAccount = value;
				NotifyOfPropertyChange(() => SelectedAccount);
			}
		}
		
		public BindableCollection<Account> Accounts
		{
			get => _accounts;
			set
			{
				_accounts = value;
				NotifyOfPropertyChange(() => Accounts);
			}
		}

		public Account CreatingAccount => new Account
		{
			Name = "Добавить",
			Visibility = Visibility.Hidden
		};


		private Screen _editableScreen;

		public Screen EditableScreen
		{
			get => _editableScreen;
			set
			{
				_editableScreen = value;
				NotifyOfPropertyChange(() => EditableScreen);
			}
		}
	}
}

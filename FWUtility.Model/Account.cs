namespace FWUtility.Model
{
	using System.ComponentModel.DataAnnotations;
	using System.ComponentModel.DataAnnotations.Schema;
	using System.Windows;

	public class Account
	{
		public int AccountId { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }

		[NotMapped]
		public bool IsChecked { get; set; }

		[NotMapped] public Visibility Visibility { get; set; } = Visibility.Visible;
	}
}

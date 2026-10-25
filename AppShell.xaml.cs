using Artemix.Pages;

namespace Artemix;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("category", typeof(CategoryPage));
		Routing.RegisterRoute("import", typeof(ImportPage));
		Routing.RegisterRoute("detail", typeof(DetailPage));
	}
}

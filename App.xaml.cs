using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CMCS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseFacade userFacade = new DatabaseFacade(new UserData());
            userFacade.EnsureCreated();

            DatabaseFacade adminFacade = new DatabaseFacade(new AdminData());
            adminFacade.EnsureCreated();

            DatabaseFacade claimsFacade = new DatabaseFacade(new ClaimsData());
            claimsFacade.EnsureCreated();

            base.OnStartup(e);
        }
    }
}

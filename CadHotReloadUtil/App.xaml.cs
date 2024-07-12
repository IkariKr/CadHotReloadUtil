using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using CadHotReloadUtil.ViewModels;
using CadHotReloadUtil.Views;
using Ikari.AutoCAD.Utility.CADInfo;
using Ikari.AutoCAD.Utility.ComUtils;
using Ikari.Common.Utility.BaseUtility;
using IkariCommon.Utility.AssemblyUtility;
using Microsoft.Extensions.DependencyInjection;

namespace CadHotReloadUtil;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static IServiceCollection Service;
    public static ServiceProvider Builder;
    public static string curPath;
    
    public App()
    {
        CheckAdministrator();
        
        Service = new ServiceCollection();
        //设置文件路径
        curPath = Path.GetDirectoryName(GetType().Assembly.Location) + "\\";
        AssemblyUtility.LoadDLL(curPath + "HandyControl.dll");

        //注册
        Service.AddSingleton<MainWindow>();
        Service.AddSingleton<MainWindowViewModel>();
        
        //构建服务
        Builder = Service.BuildServiceProvider();
        
        Builder.GetService<MainWindow>()?.Show();
    }
    
    private void CheckAdministrator()
    {
        var (isAdmin, message) = BaseUtilityExtension.CheckAdministrator();
        if (!isAdmin)
        {
            MessageBox.Show("请以管理员权限重新运行本程序。");
            Current.Shutdown();
            return;
        }
    }
    
    
    override sealed protected void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }
}
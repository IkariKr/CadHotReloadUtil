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
    private static IServiceCollection _service;
    public static ServiceProvider? Builder;
    public static string? CurPath;
    
    public App()
    {
        CheckAdministrator();
        
        _service = new ServiceCollection();
        //设置文件路径
        CurPath = Path.GetDirectoryName(GetType().Assembly.Location) + "\\";
        AssemblyUtility.LoadDll(CurPath + "HandyControl.dll");

        //注册
        _service.AddSingleton<MainWindow>();
        _service.AddSingleton<MainWindowViewModel>();
        
        //构建服务
        Builder = _service.BuildServiceProvider();
        
        Builder.GetService<MainWindow>()?.Show();
    }
    
    private void CheckAdministrator()
    {
        var (isAdmin, message) = BaseUtilityExtension.CheckAdministrator();
        if (isAdmin) return;
        MessageBox.Show("请以管理员权限重新运行本程序。");
        Current.Shutdown();
    }
    
    
    protected sealed override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }
}
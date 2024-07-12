using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using Autodesk.AutoCAD.Runtime;
using CadHotReloadUtil.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ikari.AutoCAD.Utility.CADInfo;
using Ikari.AutoCAD.Utility.CommandHelper;
using Ikari.AutoCAD.Utility.ComUtils;
using Ikari.AutoCAD.Utility.Methods;
using Ikari.Common.Utility.AppDomainManager;
using Ikari.Common.Utility.Extensions;
using Ikari.Framework.Utility.FileUtility;
using Microsoft.Extensions.DependencyInjection;
using Exception = System.Exception;

namespace CadHotReloadUtil.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        _installedCadVersions = GetInstalledCadVersion().ToObservableCollection();
    }

    [ObservableProperty] private ObservableCollection<string> _installedCadVersions;


    public RelayCommand CloseCommand => new RelayCommand(() =>
    {
        var view = App.Builder.GetService<MainWindow>();
        view?.Close();
    });

    public AssemblyDynamicLoader Adl { get; set; }

    public AsyncRelayCommand OpenCadCommand => new AsyncRelayCommand(async () =>
    {
        InfoText += nameof(OpenCadCommand) + "...";
        await Task.Run(() =>
        {
            ProgressBarVisible = Visibility.Visible;
            try
            {
                //检查AppDomain
                if (_adl == null)
                {
                    var dllPath = "Resources\\Interop.Common\\" + InstalledCadVersions[_selectedAutoCadVersionIndex];
                    _adl = AutoCADAppDomainExtension.CreateAndLoadInteropAppDomain(App.curPath, dllPath);
                }

                var innerVersion = AutoCadVersion.DisplayAutoCADToInnerVersionDic[InstalledCadVersions[_selectedAutoCadVersionIndex]];
                var cadInstallPath = _installedCadVersionDic[_installedCadVersionDic.Keys.FirstOrDefault(x => x.Contains(innerVersion))];
                if (string.IsNullOrEmpty(cadInstallPath)) return Task.CompletedTask;
                cadInstallPath += "\\acad.exe";

                (_adl.remoteLoader as AutoCadRemoteLoader)?.CreateAndShowCadApp(cadInstallPath,
                    AutoCadVersion.DisplayAutoCADToAutoCADDic[InstalledCadVersions[_selectedAutoCadVersionIndex]]);
                var pluginPath = App.curPath + "\\Ikari.AutoCAD.Utility.dll";
                (_adl.remoteLoader as AutoCadRemoteLoader)?.ExecuteNetloadMethod(pluginPath);
                
                InfoText += "打开成功";
            }
            catch (Exception e)
            {
                InfoText += e.Message;
            }
            finally
            {
                ProgressBarVisible = Visibility.Hidden;
            }

            return Task.CompletedTask;
        });

    });
    
    public RelayCommand CloseCadCommand => new RelayCommand(() =>
    {
        try
        {
            (this._adl?.remoteLoader as AutoCadRemoteLoader)?.CloseCadApp();
            InfoText+="关闭成功";
        }
        catch (Exception ex)
        {
            InfoText += ex.Message;
        }
        
    });
    
    public RelayCommand SelectPluginPathCommand => new RelayCommand(() =>
    {
        var filePaths = FileGUI.OpenFileDialog(FileGUI.FileFilter.dll);
        if (filePaths.Length == 0) return;
        PluginPath = filePaths[0];
    });
    
    public RelayCommand LoadPluginCommand => new RelayCommand(() =>
    {
        if (string.IsNullOrEmpty(PluginPath))
        {
            InfoText+="请选择插件路径";
            return;
        }

        try
        {
            //ReloadMethodExtension.LoadPlugin(PluginPath);
            InfoText += "加载成功";
        }
        catch(Exception ex)
        {
            InfoText+=ex.Message;
        }
        
    });

    private AssemblyDynamicLoader? _adl;
    private Dictionary<string, string> _installedCadVersionDic => FindAutoCADLocation.GetInstalledAutoCadVersion();
    private int _selectedAutoCadVersionIndex = 0;
    public int SelectedAutoCadVersionIndex
    {
        get => _selectedAutoCadVersionIndex;
        set
        {
            SetProperty(ref _selectedAutoCadVersionIndex, value);
            SelectedAutoCadVersionIndexChanged();
        }
    }

    private void SelectedAutoCadVersionIndexChanged()
    {
        try
        {
            var dllPath = "Resources/Interop.Common/" + InstalledCadVersions[_selectedAutoCadVersionIndex];
            _adl = AutoCADAppDomainExtension.CreateAndLoadInteropAppDomain(App.curPath, dllPath);
        }
        catch
        {
        }
    }

    public IEnumerable<string> GetInstalledCadVersion()
    {
        if (_installedCadVersionDic.Count == 0) yield break;
        foreach (var key in _installedCadVersionDic.Keys.Where(key => AutoCadVersion.InnerVersionToDisplayAutoCADDic.Keys.Contains(Path.GetFileName(key))))
        {
            yield return AutoCadVersion.InnerVersionToDisplayAutoCADDic[Path.GetFileName(key)];
        }
    }
    
    [ObservableProperty] private string _pluginPath = string.Empty;
    [ObservableProperty] private Visibility _progressBarVisible = Visibility.Hidden;
    
    public RelayCommand ExecuteMethodCommand => new RelayCommand(() =>
    {
        try
        {
            if (_adl?.remoteLoader is not AutoCadRemoteLoader rl) return;
            rl.ExecuteSendCommandMethod(parameters: new object[] { $"ExecuteMethod {PluginPath} {TypeName} {MethodName}\r" });
        }
        catch(Exception ex)
        {
            InfoText += ex.ToString();
        }


    });

    [ObservableProperty] private string _TypeName;
    [ObservableProperty] private string _MethodName;
    
    private string _infoText = string.Empty;
    public string InfoText
    {
        get => _infoText;
        set
        {
            // if (value.Contains("\n"))
            // {
            //     value.Insert(value.LastIndexOf("\n"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :"));
            //     value += "\n";
            // }
            // else
            // {
            //     value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss :") + value + "\n";
            // }
            value += "\n";
            SetProperty(ref _infoText, value);
        }
    }
};

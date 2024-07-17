using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.AutoCAD.Runtime;
using CadHotReloadUtil.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ikari.AutoCAD.Utility;
using Ikari.AutoCAD.Utility.CADInfo;
using Ikari.AutoCAD.Utility.CommandHelper;
using Ikari.AutoCAD.Utility.ComUtils;
using Ikari.AutoCAD.Utility.EnvTools;
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
        //如果包含AutoCAD 2014 则在注册表中注册
        EnvTools.AddConfidencePathForAutoCad2014(App.CurPath);
    }

    private Assembly? _assembly = null;
    
    [ObservableProperty]
    private ObservableCollection<string> _installedCadVersions;
    
    public RelayCommand CloseCommand => new RelayCommand(() =>
    {
        var view = App.Builder.GetService<MainWindow>();
        view?.Close();
    });
    
    public AsyncRelayCommand OpenCadCommand => new AsyncRelayCommand(async () =>
    {
        InfoText += nameof(OpenCadCommand) + "...";
        await Task.Run(() =>
        {
            ProgressBarVisible = Visibility.Visible;
            try
            {
                if (_adl == null)
                {
                    var dllPath = "Resources\\Interop.Common\\" + InstalledCadVersions[_selectedAutoCadVersionIndex];
                    _adl = AutoCADAppDomainExtension.CreateAndLoadInteropAppDomain(App.CurPath, dllPath);
                }

                var innerVersion = AutoCadVersion.DisplayAutoCADToInnerVersionDic[InstalledCadVersions[_selectedAutoCadVersionIndex]];
                var cadInstallPath = _installedCadVersionDic[_installedCadVersionDic.Keys.FirstOrDefault(x => x.Contains(innerVersion))];
                if (string.IsNullOrEmpty(cadInstallPath)) return Task.CompletedTask;
                cadInstallPath += "\\acad.exe";

                (_adl.remoteLoader as AutoCadRemoteLoader)?.CreateAndShowCadApp(cadInstallPath,
                    AutoCadVersion.DisplayAutoCADToAutoCADDic[InstalledCadVersions[_selectedAutoCadVersionIndex]]);
                var pluginPath = App.CurPath + "\\Ikari.AutoCAD.Utility.dll";
                (_adl.remoteLoader as AutoCadRemoteLoader)?.ExecuteNetloadCommand(pluginPath);
                
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
            (_adl?.remoteLoader as AutoCadRemoteLoader)?.CloseCadApp();
            _adl?.Unload();
            _adl = null;
            InfoText+="关闭成功";
        }
        catch (Exception ex)
        {
            InfoText += ex.Message;
        }
        
    });
    
    public RelayCommand SelectPluginPathCommand => new RelayCommand(() =>
    {
        try
        {
            var filePaths = FileGUI.OpenFileDialog(FileGUI.FileFilter.dll);
            if (filePaths.Length == 0) return;
            PluginPath = filePaths[0];

            _assembly = Assembly.Load(File.ReadAllBytes(PluginPath));
        }
        catch(Exception ex)
        {
            InfoText += ex.ToString();
        }

    });
    
    public RelayCommand LinkCadCommand => new RelayCommand(() =>
    {
        try
        {
            if (_adl == null)
            {
                var dllPath = "Resources\\Interop.Common\\" + InstalledCadVersions[_selectedAutoCadVersionIndex];
                _adl = AutoCADAppDomainExtension.CreateAndLoadInteropAppDomain(App.CurPath, dllPath);
            }
            var rl = _adl.remoteLoader as AutoCadRemoteLoader;
            rl?.GetExistingAutoCadInstance(AutoCadVersion.DisplayAutoCADToAutoCADDic[InstalledCadVersions[_selectedAutoCadVersionIndex]]);
        
            InfoText += $"连接成功:{rl.Com.GetComObject().ToString()}";
 
        }
        catch (Exception ex)
        {
            InfoText += ex.ToString();
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
            if (_adl != null)
            {
                (_adl.remoteLoader as AutoCadRemoteLoader)?.ReleaseCadApp();
                _adl.Unload();
                _adl = null;
            }
            
            var dllPath = "Resources/Interop.Common/" + InstalledCadVersions[_selectedAutoCadVersionIndex];
            _adl = AutoCADAppDomainExtension.CreateAndLoadInteropAppDomain(App.CurPath, dllPath);
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
            if (rl.ExecuteSendCommandMethod(parameters: new object[] { $"ExecuteMethod {PluginPath} {TypeName} {MethodName}\r" }))
            {
                InfoText +=  $"执行成功： {PluginPath} {TypeName} {MethodName}\r";
            }
            else
            {
                InfoText +=  $"执行失败： {PluginPath} {TypeName} {MethodName}\r";
            }
            
        }
        catch(Exception ex)
        {
            InfoText += ex.ToString();
        }


    });

    private string _typeName = string.Empty;
    public string TypeName
    {
        get => _typeName;
        set
        {
            SetProperty(ref _typeName, value);
            TypeNameChanged();
        }
    }

    private void TypeNameChanged()
    {
        try
        {
            _methodNames = _assembly?.GetType(_typeName)?.GetMethods()?.Select(x => x.Name);
            MethodNamesDisplay = _methodNames?.ToObservableCollection();
        }
        catch
        {
            // ignored
        }
    }

    [ObservableProperty] private string _methodName = string.Empty;
    
    private string _infoText = string.Empty;
    public string InfoText
    {
        get => _infoText;
        set
        {
            value += "\n";
            SetProperty(ref _infoText, value);
        }
    }
    
    private IEnumerable<string>? _methodNames = new List<string>();
    
    [ObservableProperty] private ObservableCollection<string> _typeNamesDisplay;
    private ObservableCollection<string>? _methodNamesDisplay;
    public ObservableCollection<string>? MethodNamesDisplay
    {
        get => _methodNamesDisplay;
        set
        {
            SetProperty(ref _methodNamesDisplay, value);
        }
    }

    private int _methodNamesDisplayIndex = 0;
    public int MethodNamesDisplayIndex
    {
        get => _methodNamesDisplayIndex;
        set
        {
            SetProperty(ref _methodNamesDisplayIndex, value);
            MethodNamesDisplayIndexChanged();
        }
    }

    private void MethodNamesDisplayIndexChanged()
    {
        try
        {
            MethodName = MethodNamesDisplay[MethodNamesDisplayIndex];
        }
        catch(Exception ex)
        {
            
        }
    }
};

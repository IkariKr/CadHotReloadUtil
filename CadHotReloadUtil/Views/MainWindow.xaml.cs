using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CadHotReloadUtil.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CadHotReloadUtil.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = App.Builder.GetService<MainWindowViewModel>();
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        //拖动窗体
        try
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window.GetWindow(this).DragMove();
            }
        }
        catch
        {
            
        }
    }

    public void Close(object sender, MouseButtonEventArgs e)
    {
        var viewModel = this.DataContext as MainWindowViewModel;
        viewModel?.CloseCommand.Execute(null);
    }

    private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (textBox != null)
        {
            // 将光标位置设置为文本末尾
            textBox.CaretIndex = textBox.Text.Length;
        
            // 滚动到文本末尾
            textBox.ScrollToEnd();
        
            // 更新布局，确保滚动位置正确
            textBox.UpdateLayout();
        }
    }
}
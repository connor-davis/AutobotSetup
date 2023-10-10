using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutobotSetup.Components;

public partial class Titlebar : UserControl
{
    public Titlebar()
    {
        InitializeComponent();
    }

    private void CloseBtn_OnClick(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void DragArea_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            MainWindow.GetInstance()?.DragMove();
        }
    }
}
using System.Windows;

namespace AutobotSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static MainWindow? _instance;
        
        public MainWindow()
        {
            _instance = this;
            
            InitializeComponent();
        }

        public static MainWindow? GetInstance()
        {
            return _instance;
        }

        private void ContinueBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var setupWindow = new SetupWindow();
            
            setupWindow.Show();
            Close();
        }
    }
}
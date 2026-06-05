using System.Windows;
using System.Windows.Controls;
using TIAS.ClassView.Pages;
using TIAS.Core.Models;
using TIAS.Elements;

namespace TIAS.Pages
{
    /// <summary>
    /// Логика взаимодействия для SelectLevel.xaml
    /// </summary>
    public partial class SelectLevel : Page
    {
        public SelectLevelView ThisSelectLevelView {get; private set; }
        public SelectLevel()
        {
            InitializeComponent();

            ThisSelectLevelView = new SelectLevelView();
            DataContext = ThisSelectLevelView;
            MainWindow.Instance.MapChanged += ChangeSelectLevel;
            MainWindow.Instance.LoadMapsFromDatabase();
            LoadLevel();
        }
        public void LoadLevel()
        {
            if(MainWindow.Instance.Maps == null)
            {
                return;
            }
            parrent.Children.Clear();
            foreach (var map in MainWindow.Instance.Maps)
            {
                if(map != null)
                {
                    parrent.Children.Add(new Level(map));
                }
            }
        }
        public void ChangeSelectLevel(HexMap newLevel)
        {
            if (newLevel == null)
            {
                MessageBox.Show("newLevel == null");
                return;
            }
            ThisSelectLevelView.LevelMap = newLevel;
            UpdatePanelSelectLevel();
        }
        public void UpdatePanelSelectLevel()
        {
            if (ThisSelectLevelView.LevelMap == null)
            {
                PanelNoSeletLevel.Visibility = Visibility.Visible;
                PanelSeletLevel.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelSeletLevel.Visibility = Visibility.Visible;
                PanelNoSeletLevel.Visibility = Visibility.Collapsed;
            }
        }
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if(ThisSelectLevelView.LevelMap != null)
            {
                MainWindow.Instance.frame.Navigate(new LoadLevel(ThisSelectLevelView.LevelMap));
            }
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.frame.Navigate(new MainMenu());
            MainWindow.Instance.MapChanged -= ChangeSelectLevel;
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if(ThisSelectLevelView.LevelMap == null)
            {
                return;
            }
            MainWindow.Instance.frame.Navigate(new LevelEdit(ThisSelectLevelView.LevelMap));
            MainWindow.Instance.MapChanged -= ChangeSelectLevel;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.frame.Navigate(new LevelEdit());
            MainWindow.Instance.MapChanged -= ChangeSelectLevel;
        }
    }
}

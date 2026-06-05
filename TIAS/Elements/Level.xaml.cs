using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TIAS.Core.Hex;
using TIAS.Core.Models;
using TIAS.Models;

namespace TIAS.Elements
{
    /// <summary>
    /// Логика взаимодействия для Level.xaml
    /// </summary>
    public partial class Level : UserControl
    {
        private LevelModel CurrentLevel {  get; set; }
        public Level(HexMap level)
        {
            InitializeComponent();
            if (level == null || level.Units == null)
            {
                MessageBox.Show("Ошибка level == null", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CurrentLevel = new LevelModel
            {
                LevelName = $"Level №{level.Id}",
                LevelDescription = $"Размер: {level.Width}x{level.Height}\nКоличество юнитов {level.Units.Count}",
                LevelMap = level
            };
            DataContext = CurrentLevel;
        }
        private void SelectLevel(object sender, MouseButtonEventArgs e)
        {
            if (CurrentLevel.LevelMap == null)
            {
                MessageBox.Show("CurrentLevel.LevelMap == null");
                return;
            }
            MainWindow.Instance.SelectLevel = CurrentLevel.LevelMap;
        }
    }
}

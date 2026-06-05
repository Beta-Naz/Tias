using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Windows;
using TIAS.Core.Base;
using TIAS.Core.Database;
using TIAS.Core.Models;
using TIAS.Core.Structure;
using TIAS.Models;
using TIAS.Pages;

namespace TIAS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public List<HexMap> Maps { get; set; }
        public HexMap _selectLevel { get; set; }
        public int CurrentLevel { get; set; }
        public HexMap SelectLevel
        {
            get
            {
                return _selectLevel;
            }
            set
            {
                if( _selectLevel != value)
                {
                    _selectLevel = value;
                    MapChanged?.Invoke(value);
                }
            }
        }
        public Unit SelectUnit { get; set; }
        public List<string> ErrorMessages { get; set; }

        private MapDatabase _db;
        public event Action<HexMap> MapChanged;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            // Инициализируем подключение к БД
            _db = new MapDatabase("localhost", "TIAS_Game", "root", "");

            // Загружаем карты из БД
            LoadMapsFromDatabase();
            frame.Navigate(new Pages.MainMenu());
        }

        public void LoadMapsFromDatabase()
        {
            try
            {
                Maps = _db.LoadAllMaps();
                CurrentLevel = 0;
                ErrorMessages = new List<string>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки карт: {ex.Message}");
            }
        }
        private void AddTestLevel()
        {
            HexMap map = new HexMap(0,12,12);
            Unit unit = new Tank(0,new HexCoord(3,2), Core.Enums.TypeAlliance.USSR);
            Unit unit1 = new Tank(1, new HexCoord(5, 2), Core.Enums.TypeAlliance.Germany);
            Unit unit2 = new Artillery(2, new HexCoord(4, 2), Core.Enums.TypeAlliance.USSR);
            Unit unit3 = new Infanity(3, new HexCoord(6, 2), Core.Enums.TypeAlliance.Germany);
            map.AddUnit(unit);
            map.AddUnit(unit1);
            map.AddUnit(unit2);
            map.AddUnit(unit3);
            Maps.Add(map);
        }
    }
}

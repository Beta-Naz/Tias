using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TIAS.Core.Base;
using TIAS.Core.Database;
using TIAS.Core.Enums;
using TIAS.Core.Factory;
using TIAS.Core.Models;
using TIAS.Core.Structure;
using TIAS.Models;

namespace TIAS.Pages
{
    public partial class LevelEdit : Page
    {
        private HexMap _currentMap;
        private bool _isNewMap;
        private MapDatabase _database;
        private UnitFactory _unitFactory;

        // Режимы редактирования
        private CellType _selectedCellType = CellType.Plain;
        private UnitType? _selectedUnitType = null;
        private TypeAlliance? _selectedAlliance = null;
        private bool _isErasing = false;
        private bool _isSelecting = false;

        // Визуальные элементы
        private Dictionary<HexCoord, Polygon> _hexPolygons = new Dictionary<HexCoord, Polygon>();
        private Dictionary<Unit, Border> _unitElements = new Dictionary<Unit, Border>();
        private static readonly float _hexSize = 40f;

        public LevelEdit(HexMap currentMap = null)
        {
            InitializeComponent();

            _database = new MapDatabase("localhost", "TIAS_Game", "root", "1234");
            _unitFactory = new UnitFactory();

            if (currentMap != null)
            {
                // Режим редактирования
                _isNewMap = false;
                _currentMap = currentMap;
                SizePanel.Visibility = Visibility.Collapsed;
                IdPanel.Visibility = Visibility.Visible;
                MapIdTextBox.Text = currentMap.Id.ToString();
                MapNameTextBox.Text = $"Level {currentMap.Id}";
                LoadMap();
            }
            else
            {
                // Режим создания
                _isNewMap = true;
                SizePanel.Visibility = Visibility.Visible;
                IdPanel.Visibility = Visibility.Collapsed;
            }

            // По умолчанию выбираем кисть с равниной
            HighlightSelectedButton(PlainButton);
            CurrentCellTypeText.Text = "Выбрано: Равнина";
            CurrentToolText.Text = "Инструмент: Кисть";
        }

        private void LoadMap()
        {
            if (_currentMap == null) return;

            // Очищаем канвас
            MapCanvas.Children.Clear();
            _hexPolygons.Clear();
            _unitElements.Clear();

            // Устанавливаем размер канваса
            UpdateCanvasSize();

            // Рисуем клетки
            for (int q = 0; q < _currentMap.Width; q++)
            {
                for (int r = 0; r < _currentMap.Height; r++)
                {
                    DrawHexCell(new HexCoord(q, r));
                }
            }

            // Рисуем юнитов
            foreach (var unit in _currentMap.Units)
            {
                DrawUnit(unit);
            }
        }

        private void CreateNewMap()
        {
            if (!int.TryParse(WidthTextBox.Text, out int width) || width < 5 || width > 30)
            {
                MessageBox.Show("Ширина должна быть от 5 до 30");
                return;
            }

            if (!int.TryParse(HeightTextBox.Text, out int height) || height < 5 || height > 30)
            {
                MessageBox.Show("Высота должна быть от 5 до 30");
                return;
            }

            _currentMap = new HexMap(0, width, height);
            if (_currentMap.Cells == null)
            {
                _currentMap.Cells = new CellType[width, height];
                for (int q = 0; q < width; q++)
                    for (int r = 0; r < height; r++)
                        _currentMap.Cells[q, r] = CellType.Plain;
            }
            LoadMap();
            SizePanel.Visibility = Visibility.Collapsed;
        }

        private void UpdateCanvasSize()
        {
            if (_currentMap == null) return;

            float xOffset = _hexSize * (float)Math.Sqrt(3);
            float yOffset = _hexSize * 1.5f;

            double width = xOffset * _currentMap.Width + (xOffset / 2) + 200;
            double height = yOffset * _currentMap.Height + 200;

            MapCanvas.Width = width;
            MapCanvas.Height = height;
        }

        private void DrawHexCell(HexCoord coord)
        {
            Point center = GetPixelPosition(coord, _hexSize);
            Point[] points = GetHexVertices(center, _hexSize);

            Polygon hexagon = new Polygon
            {
                Points = new PointCollection(points),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = GetTerrainBrush(_currentMap.Cells[coord.Q, coord.R]),
                Tag = coord
            };

            hexagon.MouseLeftButtonDown += (s, e) => OnHexClick(coord);
            hexagon.MouseEnter += (s, e) => OnHexHover(coord);

            MapCanvas.Children.Add(hexagon);
            _hexPolygons[coord] = hexagon;
        }

        private void DrawUnit(Unit unit)
        {
            Point center = GetPixelPosition(unit.Position, _hexSize);

            var border = new Border
            {
                Width = 40,
                Height = 40,
                Background = GetAllianceColor(unit.NameAlliance),
                CornerRadius = new CornerRadius(20),
                BorderBrush = Brushes.Gold,
                BorderThickness = new Thickness(0),
                Tag = unit
            };

            var image = new Image
            {
                Source = GetUnitImage(unit),
                Width = 35,
                Height = 35
            };

            border.Child = image;
            border.MouseLeftButtonDown += (s, e) => OnUnitClick(unit);

            Canvas.SetLeft(border, center.X - 20);
            Canvas.SetTop(border, center.Y - 20);

            MapCanvas.Children.Add(border);
            _unitElements[unit] = border;
        }

        private ImageSource GetUnitImage(Unit unit)
        {
            string path = "";
            if (unit is Tank)
                path = unit.NameAlliance == TypeAlliance.USSR ? "/Images/Unit/T34.png" : "/Images/Unit/Panjer.png";
            else if (unit is Infanity)
                path = unit.NameAlliance == TypeAlliance.USSR ? "/Images/Unit/infanity_ussr.png" : "/Images/Unit/infanity_germany.png";
            else if (unit is Artillery)
                path = unit.NameAlliance == TypeAlliance.USSR ? "/Images/Unit/art1.png" : "/Images/Unit/art2.png";

            return new System.Windows.Media.Imaging.BitmapImage(new Uri(path, UriKind.Relative));
        }

        private Brush GetAllianceColor(TypeAlliance alliance)
        {
            return alliance == TypeAlliance.USSR ?
                new SolidColorBrush(Color.FromArgb(128, 0, 255, 0)) :
                new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        }

        private void OnHexClick(HexCoord coord)
        {
            if (_currentMap == null)
            {
                CreateNewMap();
                return;
            }

            if (_isErasing)
            {
                // Стираем все с клетки
                var unit = _currentMap.GetUnit(coord);
                if (unit != null)
                {
                    _currentMap.DeleteUnit(unit);
                    if (_unitElements.ContainsKey(unit))
                    {
                        MapCanvas.Children.Remove(_unitElements[unit]);
                        _unitElements.Remove(unit);
                    }
                }
                else
                {
                    // Возвращаем равнину
                    _currentMap.Cells[coord.Q, coord.R] = CellType.Plain;
                    UpdateHexColor(coord, GetTerrainBrush(CellType.Plain));
                }
            }
            else if (_selectedUnitType.HasValue && _selectedAlliance.HasValue)
            {
                // Размещаем юнита
                var existingUnit = _currentMap.GetUnit(coord);
                if (existingUnit != null)
                {
                    _currentMap.DeleteUnit(existingUnit);
                    if (_unitElements.ContainsKey(existingUnit))
                    {
                        MapCanvas.Children.Remove(_unitElements[existingUnit]);
                        _unitElements.Remove(existingUnit);
                    }
                }

                int newId = _currentMap.Units.Count + 1;
                var newUnit = _unitFactory.CreateUnit(newId, coord, _selectedUnitType.Value, _selectedAlliance.Value);
                _currentMap.AddUnit(newUnit);
                DrawUnit(newUnit);
            }
            else
            {
                // Меняем тип клетки
                _currentMap.Cells[coord.Q, coord.R] = _selectedCellType;
                UpdateHexColor(coord, GetTerrainBrush(_selectedCellType));
            }
        }

        private void OnHexHover(HexCoord coord)
        {
            InfoTextBlock.Text = $"Клетка ({coord.Q}, {coord.R}) - {_currentMap?.Cells[coord.Q, coord.R]}";
        }

        private void OnUnitClick(Unit unit)
        {
            if (_isErasing)
            {
                _currentMap.DeleteUnit(unit);
                if (_unitElements.ContainsKey(unit))
                {
                    MapCanvas.Children.Remove(_unitElements[unit]);
                    _unitElements.Remove(unit);
                }
            }
        }

        private void UpdateHexColor(HexCoord coord, Brush brush)
        {
            if (_hexPolygons.TryGetValue(coord, out var polygon))
            {
                polygon.Fill = brush;
            }
        }

        private Brush GetTerrainBrush(CellType type)
        {
            switch (type)
            {
                case CellType.Plain: return new SolidColorBrush(Color.FromRgb(144, 238, 144));
                case CellType.Forest: return new SolidColorBrush(Color.FromRgb(34, 139, 34));
                case CellType.Hill: return new SolidColorBrush(Color.FromRgb(160, 82, 45));
                case CellType.Mountain: return new SolidColorBrush(Color.FromRgb(128, 128, 128));
                case CellType.Water: return new SolidColorBrush(Color.FromRgb(64, 164, 223));
                case CellType.City: return new SolidColorBrush(Color.FromRgb(169, 169, 169));
                default: return new SolidColorBrush(Color.FromRgb(200, 200, 200));
            };
        }

        public Point GetPixelPosition(HexCoord hexCoord, float hexSize)
        {
            float xOffset = hexSize * (float)Math.Sqrt(3);
            float yOffset = hexSize * 1.5f;

            float x = xOffset * hexCoord.Q;
            float y = yOffset * hexCoord.R;

            if (hexCoord.R % 2 == 1)
            {
                x += xOffset / 2;
            }

            return new Point(x + 100, y + 100);
        }

        private Point[] GetHexVertices(Point center, float size)
        {
            Point[] vertices = new Point[6];
            for (int i = 0; i < 6; i++)
            {
                double angle = 60 * i * Math.PI / 180;
                double x = center.X + size * Math.Sin(angle);
                double y = center.Y - size * Math.Cos(angle);
                vertices[i] = new Point(x, y);
            }
            return vertices;
        }

        private void CellTypeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string typeName = button.Tag.ToString();

            _selectedCellType = (CellType)Enum.Parse(typeof(CellType), typeName);
            _selectedUnitType = null;
            _selectedAlliance = null;
            _isErasing = false;
            _isSelecting = false;

            HighlightSelectedButton(button);
            CurrentCellTypeText.Text = $"Выбрано: {typeName}";
            CurrentToolText.Text = "Инструмент: Кисть";
        }

        private void UnitButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string[] parts = button.Tag.ToString().Split(',');

            _selectedUnitType = (UnitType)Enum.Parse(typeof(UnitType), parts[0]);
            _selectedAlliance = (TypeAlliance)Enum.Parse(typeof(TypeAlliance), parts[1]);
            _isErasing = false;
            _isSelecting = false;

            HighlightSelectedButton(button);
            CurrentToolText.Text = $"Юнит: {_selectedUnitType} ({_selectedAlliance})";
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string tool = button.Tag.ToString();

            if (tool == "Eraser")
            {
                _isErasing = true;
                _selectedUnitType = null;
                _selectedAlliance = null;
                HighlightSelectedButton(button);
                CurrentToolText.Text = "Инструмент: Ластик";
            }
            else if (tool == "Select")
            {
                _isSelecting = true;
                _isErasing = false;
                _selectedUnitType = null;
                _selectedAlliance = null;
                HighlightSelectedButton(button);
                CurrentToolText.Text = "Инструмент: Выделение";
            }
        }

        private void HighlightSelectedButton(Button selectedButton)
        {
            // Сбрасываем выделение у всех кнопок
            var buttons = new[]
            {
                PlainButton, ForestButton, HillButton, MountainButton, WaterButton, CityButton,
                SovietTankButton, SovietInfantryButton, SovietArtilleryButton,
                GermanTankButton, GermanInfantryButton, GermanArtilleryButton,
                EraserButton, SelectButton
            };

            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    btn.SetValue(Button.IsEnabledProperty, true);
                    btn.ClearValue(BackgroundProperty);
                    if (btn == PlainButton) btn.Background = new SolidColorBrush(Color.FromRgb(144, 238, 144));
                    else if (btn == ForestButton) btn.Background = new SolidColorBrush(Color.FromRgb(34, 139, 34));
                    else if (btn == HillButton) btn.Background = new SolidColorBrush(Color.FromRgb(160, 82, 45));
                    else if (btn == MountainButton) btn.Background = new SolidColorBrush(Color.FromRgb(128, 128, 128));
                    else if (btn == WaterButton) btn.Background = new SolidColorBrush(Color.FromRgb(64, 164, 223));
                    else if (btn == CityButton) btn.Background = new SolidColorBrush(Color.FromRgb(169, 169, 169));
                    else btn.Background = new SolidColorBrush(Color.FromArgb(74, 0, 0, 0));
                }
            }

            // Выделяем выбранную кнопку
            selectedButton.Background = Brushes.Gold;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMap == null)
            {
                MessageBox.Show("Сначала создайте карту!");
                return;
            }

            try
            {
                if (_isNewMap)
                {
                    int newId = _database.SaveNewMap(_currentMap);
                    MessageBox.Show($"Карта сохранена! ID: {newId}");
                }
                else
                {
                    _database.UpdateMap(_currentMap);
                    MessageBox.Show("Карта обновлена!");
                }

                MainWindow.Instance.frame.Navigate(new SelectLevel());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageBox.Show($"Вы точно хотите выйти?", $"Exit",
            MessageBoxButton.YesNo, MessageBoxImage.Information);
            if(message == MessageBoxResult.OK)
            {
                MainWindow.Instance.frame.Navigate(new SelectLevel());
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Обработка клика по пустому месту
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Отмена текущего инструмента
                _selectedUnitType = null;
                _selectedAlliance = null;
                _isErasing = false;
                _isSelecting = false;
                HighlightSelectedButton(PlainButton);
                CurrentToolText.Text = "Инструмент: Кисть";
            }
        }

        private void Create_CLick(object sender, RoutedEventArgs e)
        {
            CreateNewMap();
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var message = MessageBox.Show($"Вы точно хотите удалить картку под Id {_currentMap.Id} \n Это действие нельзя будет отменить", $"Delete map {_currentMap.Id}",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if(message == MessageBoxResult.Yes)
            {
                _database.DeleteMap(_currentMap.Id);
                MainWindow.Instance.frame.Navigate(new SelectLevel());
            }
            else
            {
                MessageBox.Show("Спасибо, никто и не узнает, что тут была самая страшная ошибка...");
            }
        }
    }
}
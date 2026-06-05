using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.Managers;
using TIAS.Core.Models;
using TIAS.Core.Structure;
using TIAS.Elements;

namespace TIAS.Pages
{
    public partial class LoadLevel : Page
    {
        private HexMap CurrentLevel;
        private static readonly float _hexSize = 50f;
        private GameManager _gameManager;

        // Highlighting
        private List<Polygon> _highlightedCells = new List<Polygon>();
        private Dictionary<HexCoord, Polygon> _hexPolygons = new Dictionary<HexCoord, Polygon>();

        // Dragging
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private double _scrollViewerHorizontalOffset;
        private double _scrollViewerVerticalOffset;
        private double _minHorizontalOffset, _maxHorizontalOffset;
        private double _minVerticalOffset, _maxVerticalOffset;

        public LoadLevel(HexMap currentLevel)
        {
            InitializeComponent();

            if (currentLevel == null)
            {
                NavigationService?.GoBack();
                return;
            }

            CurrentLevel = currentLevel;
            _gameManager = GameManager.Instance;
            _gameManager.SetCurrentMap(currentLevel);

            Loaded += Page_Loaded;
            SizeChanged += Page_SizeChanged;

            // Subscribe to game events
            _gameManager.OnUnitSelected += OnUnitSelected;
            _gameManager.OnPhaseChanged += OnPhaseChanged;
            _gameManager.OnGameMessages += OnGameMessages;

            CreateMap(currentLevel);
            _gameManager.StartGame(currentLevel.Units);

            // Add turn button to UI
            AddTurnButton();
        }

        private void AddTurnButton()
        {
            var turnButton = new Button
            {
                Content = "ЗАКОНЧИТЬ ХОД",
                Width = 150,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 10, 0),
                Background = new SolidColorBrush(Color.FromArgb(170, 0, 71, 171)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            turnButton.Click += (s, e) => _gameManager.EndTurn();

            var grid = (Grid)Content;
            grid.Children.Add(turnButton);
        }

        private void OnPhaseChanged(GamePhase newPhase)
        {
            Dispatcher.Invoke(() =>
            {
                PhaseTextBlock.Text = newPhase == GamePhase.PlayerTurn ? "ВАШ ХОД" : "ХОД ПРОТИВНИКА";
                PhaseTextBlock.Foreground = newPhase == GamePhase.PlayerTurn ? Brushes.Green : Brushes.Red;

                if (newPhase == GamePhase.GameOver)
                {
                    ShowGameOverMessage();
                }
            });
        }

        private void OnUnitSelected(Unit unit)
        {
            Dispatcher.Invoke(() =>
            {
                ClearHighlights();

                if (unit != null)
                {
                    if (!unit.HasMovedThisTurn)
                    {
                        // Подсвечиваем доступные для перемещения клетки
                        foreach (var pos in unit.ReachablePositions)
                        {
                            if (!pos.Equals(unit.Position))
                            {
                                HighlightHex(pos, Brushes.Green, 0.3);
                            }
                        }
                    }

                    if (!unit.HasAttackedThisTurn)
                    {
                        // Подсвечиваем доступные для атаки клетки
                        foreach (var pos in unit.AttackablePositions)
                        {
                            if (_hexPolygons.TryGetValue(pos, out var polygon))
                            {
                                if (CurrentLevel.GetUnit(pos) != null)
                                {
                                    HighlightHex(pos, Brushes.Red, 0.5);
                                }
                            }
                        }
                    }

                    // Подсвечиваем самого юнита
                    if (_hexPolygons.TryGetValue(unit.Position, out var unitHex))
                    {
                        unitHex.Stroke = Brushes.Gold;
                        unitHex.StrokeThickness = 3;
                    }

                    UnitInfoTextBlock.Text = $"{unit.UnitName}\nHP: {unit.Health}/{unit.MaxHealth}\n" +
                                             $"Действия: {(unit.HasMovedThisTurn ? "✗" : "✓")} ход, " +
                                             $"{(unit.HasAttackedThisTurn ? "✗" : "✓")} атака";
                }
                else
                {
                    UnitInfoTextBlock.Text = "Выберите юнита";
                }
            });
        }

        private void OnGameMessages(List<string> messages)
        {
            Dispatcher.Invoke(() =>
            {
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
                MessagesScrollViewer.ScrollToBottom();
            });
        }

        private void HighlightHex(HexCoord coord, Brush color, double opacity = 0.3)
        {
            if (_hexPolygons.TryGetValue(coord, out var polygon))
            {
                var highlight = new Polygon
                {
                    Points = polygon.Points,
                    Fill = color,
                    Opacity = opacity,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(highlight, Canvas.GetLeft(polygon));
                Canvas.SetTop(highlight, Canvas.GetTop(polygon));

                parrent.Children.Add(highlight);
                _highlightedCells.Add(highlight);
            }
        }

        private void ClearHighlights()
        {
            foreach (var highlight in _highlightedCells)
            {
                parrent.Children.Remove(highlight);
            }
            _highlightedCells.Clear();

            // Reset hex borders
            foreach (var kvp in _hexPolygons)
            {
                kvp.Value.Stroke = Brushes.Black;
                kvp.Value.StrokeThickness = 1;
            }
        }

        private void CreateMap(HexMap currentLevel)
        {
            parrent.Children.Clear();
            _hexPolygons.Clear();
            _unitElements.Clear();

            UpdateCanvasSize();

            for (int i = 0; i < currentLevel.Width; i++)
            {
                for (int j = 0; j < currentLevel.Height; j++)
                {
                    HexCoord hexCoord = new HexCoord(i, j);
                    DrawHexCell(hexCoord);
                }
            }

            // Add units
            foreach (var unit in currentLevel.Units)
            {
                AddUnitToMap(unit);
            }
        }
        private Dictionary<Unit, ElementUnit> _unitElements = new Dictionary<Unit, ElementUnit>();
        private void AddUnitToMap(Unit unit)
        {
            // Устанавливаем ссылку на карту для юнита
            unit.SetCurrentMap(CurrentLevel);

            Point center = GetPixelPosition(unit.Position, _hexSize);
            var unitElement = new ElementUnit(unit);

            // Подписываемся на событие изменения позиции
            unit.OnPositionChanged += (oldPos, newPos) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (unit.CurrentPath != null && unit.CurrentPath.Count > 0)
                    {
                        AnimateUnitMovement(unitElement, unit.CurrentPath);
                        unit.CurrentPath = null; // Очищаем путь после анимации
                    }
                    else
                    {
                        UpdateUnitPosition(unitElement, newPos);
                    }
                });
            };

            // Подписываемся на событие смерти
            unit.OnUnitDied += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_unitElements.ContainsKey(unit))
                    {
                        // Анимация смерти
                        var deathAnimation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(300)
                        };
                        deathAnimation.Completed += (s, e) =>
                        {
                            parrent.Children.Remove(unitElement);
                            _unitElements.Remove(unit);
                        };
                        unitElement.BeginAnimation(UIElement.OpacityProperty, deathAnimation);
                    }
                });
            };

            // Подписываемся на событие получения урона
            unit.OnTakeDamage += (damage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ShowDamageNumber(unitElement, damage);
                });
            };

            Canvas.SetLeft(unitElement, center.X - 25);
            Canvas.SetTop(unitElement, center.Y - 25);

            if (!_unitElements.ContainsKey(unit))
            {
                _unitElements[unit] = unitElement;
            }

            parrent.Children.Add(unitElement);
        }
        private void ShowDamageNumber(ElementUnit unitElement, float damage)
        {
            var damageText = new TextBlock
            {
                Text = $"-{damage}",
                Foreground = Brushes.Red,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(damageText, Canvas.GetLeft(unitElement) + 20);
            Canvas.SetTop(damageText, Canvas.GetTop(unitElement) - 10);

            parrent.Children.Add(damageText);

            // Анимация появления и исчезновения
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1000)
            };

            var moveUp = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = Canvas.GetTop(damageText),
                To = Canvas.GetTop(damageText) - 30,
                Duration = TimeSpan.FromMilliseconds(1000)
            };

            fadeOut.Completed += (s, e) => parrent.Children.Remove(damageText);

            damageText.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            damageText.BeginAnimation(Canvas.TopProperty, moveUp);
        }
        private void UpdateUnitPosition(ElementUnit unitElement, HexCoord newPos)
        {
            if (unitElement == null || parrent == null) return;

            Point newCenter = GetPixelPosition(newPos, _hexSize);
            Canvas.SetLeft(unitElement, newCenter.X - 25);
            Canvas.SetTop(unitElement, newCenter.Y - 25);
        }
        private void OnClickHex(HexCoord hex)
        {
            if (_gameManager.CurrentPhase != GamePhase.PlayerTurn)
            {
                AddMessage("Сейчас не ваш ход!");
                return;
            }

            var unit = CurrentLevel.GetUnit(hex);

            if (unit != null)
            {
                // Клик по юниту
                if (unit.NameAlliance == _gameManager.CurrentPlayer)
                {
                    // Свой юнит - выбираем
                    _gameManager.SelectUnit(unit);
                }
                else
                {
                    // Враг - атакуем если выбран юнит
                    if (_gameManager.SelectedUnit == null)
                    {
                        AddMessage("Сначала выберите своего юнита для атаки!");
                    }
                    else
                    {
                        _gameManager.AttackWithSelectedUnit(unit);
                    }
                }
            }
            else
            {
                // Пустая клетка - перемещаем выбранного юнита
                if (_gameManager.SelectedUnit == null)
                {
                    AddMessage("Сначала выберите юнита!");
                }
                else
                {
                    _gameManager.MoveSelectedUnit(hex);
                }
            }
        }
        private void AddMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var messages = new List<string>(MessagesListBox.ItemsSource as List<string> ?? new List<string>());
                messages.Add($"[{DateTime.Now:T}] {message}");
                MessagesListBox.ItemsSource = null;
                MessagesListBox.ItemsSource = messages;
                MessagesScrollViewer.ScrollToBottom();
            });
        }

        private void ShowGameOverMessage()
        {
            var result = MessageBox.Show(
                _gameManager.PlayerUnits.Count > 0 ? "Победа! Хотите сыграть еще?" : "Поражение... Хотите попробовать снова?",
                "Игра окончена",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                MainWindow.Instance.frame.Navigate(new SelectLevel());
            }
            else
            {
                MainWindow.Instance.frame.Navigate(new MainMenu());
            }
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

            return new Point(x + 100, y + 100); // Add padding
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

        private Brush GetTerrainBrush(HexCoord hexCoord)
        {
            CellType type = CurrentLevel.Cells[hexCoord.Q, hexCoord.R];
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

        private void DrawHexCell(HexCoord hexCoord)
        {
            Point center = GetPixelPosition(hexCoord, _hexSize);
            Point[] points = GetHexVertices(center, _hexSize);

            Polygon hexagon = new Polygon()
            {
                Points = new PointCollection(points),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = GetTerrainBrush(hexCoord),
                Tag = hexCoord
            };

            Canvas.SetLeft(hexagon, 0);
            Canvas.SetTop(hexagon, 0);

            hexagon.MouseLeftButtonDown += (s, e) =>
            {
                OnClickHex(hexCoord);
                e.Handled = true;
            };

            parrent.Children.Add(hexagon);
            _hexPolygons[hexCoord] = hexagon;
        }

        private void UpdateCanvasSize()
        {
            float xOffset = _hexSize * (float)Math.Sqrt(3);
            float yOffset = _hexSize * 1.5f;

            double width = xOffset * CurrentLevel.Width + (xOffset / 2) + 200;
            double height = yOffset * CurrentLevel.Height + 200;

            parrent.Width = width;
            parrent.Height = height;
        }

        private void UpdateScrollBounds()
        {
            if (MainScrollViewer == null || parrent == null)
                return;

            _maxHorizontalOffset = Math.Max(0, parrent.Width - MainScrollViewer.ViewportWidth);
            _maxVerticalOffset = Math.Max(0, parrent.Height - MainScrollViewer.ViewportHeight);
        }

        private void ClampScrollPosition()
        {
            if (MainScrollViewer == null) return;

            double currentH = MainScrollViewer.HorizontalOffset;
            double currentV = MainScrollViewer.VerticalOffset;

            double clampedH = Math.Max(_minHorizontalOffset, Math.Min(_maxHorizontalOffset, currentH));
            double clampedV = Math.Max(_minVerticalOffset, Math.Min(_maxVerticalOffset, currentV));

            if (Math.Abs(clampedH - currentH) > 0.01 || Math.Abs(clampedV - currentV) > 0.01)
            {
                MainScrollViewer.ScrollToHorizontalOffset(clampedH);
                MainScrollViewer.ScrollToVerticalOffset(clampedV);
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(MainScrollViewer);
            _scrollViewerHorizontalOffset = MainScrollViewer.HorizontalOffset;
            _scrollViewerVerticalOffset = MainScrollViewer.VerticalOffset;
            parrent.Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentMousePosition = e.GetPosition(MainScrollViewer);
                double deltaX = currentMousePosition.X - _lastMousePosition.X;
                double deltaY = currentMousePosition.Y - _lastMousePosition.Y;

                double newH = _scrollViewerHorizontalOffset - deltaX;
                double newV = _scrollViewerVerticalOffset - deltaY;

                newH = Math.Max(_minHorizontalOffset, Math.Min(_maxHorizontalOffset, newH));
                newV = Math.Max(_minVerticalOffset, Math.Min(_maxVerticalOffset, newV));

                MainScrollViewer.ScrollToHorizontalOffset(newH);
                MainScrollViewer.ScrollToVerticalOffset(newV);
                e.Handled = true;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            parrent.Cursor = Cursors.Arrow;
            e.Handled = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateScrollBounds();
            if (_maxHorizontalOffset > 0)
                MainScrollViewer.ScrollToHorizontalOffset(_maxHorizontalOffset / 2);
            if (_maxVerticalOffset > 0)
                MainScrollViewer.ScrollToVerticalOffset(_maxVerticalOffset / 2);

            this.Focus();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollBounds();
            ClampScrollPosition();
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_isDragging)
            {
                ClampScrollPosition();
            }
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            double scrollSpeed = 50;
            double newH = MainScrollViewer.HorizontalOffset;
            double newV = MainScrollViewer.VerticalOffset;

            switch (e.Key)
            {
                case Key.Left:
                    newH -= scrollSpeed;
                    break;
                case Key.Right:
                    newH += scrollSpeed;
                    break;
                case Key.Up:
                    newV -= scrollSpeed;
                    break;
                case Key.Down:
                    newV += scrollSpeed;
                    break;
                case Key.Escape:
                    _gameManager.DeselectUnit();
                    break;
                default:
                    return;
            }

            newH = Math.Max(_minHorizontalOffset, Math.Min(_maxHorizontalOffset, newH));
            newV = Math.Max(_minVerticalOffset, Math.Min(_maxVerticalOffset, newV));

            MainScrollViewer.ScrollToHorizontalOffset(newH);
            MainScrollViewer.ScrollToVerticalOffset(newV);
            e.Handled = true;
        }
        private void AnimateUnitMovement(ElementUnit unitElement, List<HexCoord> path)
        {
            if (path == null || path.Count < 2) return;

            int currentStep = 1;
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);

            timer.Tick += (s, e) =>
            {
                if (currentStep < path.Count)
                {
                    Point targetPos = GetPixelPosition(path[currentStep], _hexSize);
                    Canvas.SetLeft(unitElement, targetPos.X - 25);
                    Canvas.SetTop(unitElement, targetPos.Y - 25);
                    currentStep++;
                }
                else
                {
                    timer.Stop();
                }
            };

            timer.Start();
        }
    }
}
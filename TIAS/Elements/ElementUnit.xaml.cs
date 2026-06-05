using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.Managers;
using TIAS.Models;

namespace TIAS.Elements
{
    /// <summary>
    /// Логика взаимодействия для ElementUnit.xaml
    /// </summary>
    public partial class ElementUnit : UserControl
    {
        public Unit CurrentUnit { get; private set; }
        private readonly int _startWidth = 30;

        public ElementUnit(Unit unit)
        {
            InitializeComponent();
            CurrentUnit = unit;

            LoadImages();
            UpdateHealthBar();

            // Подписываемся на событие изменения здоровья
            if (CurrentUnit != null)
            {
                CurrentUnit.OnHealthChanged += UpdateHealthBar;
                CurrentUnit.OnUnitDied += OnUnitDied;
            }
        }

        private void UpdateHealthBar()
        {
            Dispatcher.Invoke(() =>
            {
                if (CurrentUnit == null || HealtBar == null) return;

                double healthPercent = CurrentUnit.Health / CurrentUnit.MaxHealth;
                HealtBar.Width = Math.Max(0, _startWidth * healthPercent);

                // Меняем цвет полоски здоровья в зависимости от процента
                if (healthPercent > 0.6)
                    HealtBar.Background = System.Windows.Media.Brushes.Green;
                else if (healthPercent > 0.3)
                    HealtBar.Background = System.Windows.Media.Brushes.Orange;
                else
                    HealtBar.Background = System.Windows.Media.Brushes.Red;
            });
        }

        private void OnUnitDied()
        {
            Dispatcher.Invoke(() =>
            {
                // Можно добавить анимацию смерти или просто скрыть юнита
                this.Visibility = Visibility.Collapsed;
            });
        }

        private void LoadImages()
        {
            try
            {
                if (CurrentUnit is Tank)
                {
                    if (CurrentUnit.NameAlliance == TypeAlliance.USSR)
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/T34.png", UriKind.Relative));
                    }
                    else
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/Panjer.png", UriKind.Relative));
                    }
                }
                else if (CurrentUnit is Infanity)
                {
                    // Исправляем: для СССР и Германии разные текстуры пехоты
                    if (CurrentUnit.NameAlliance == TypeAlliance.USSR)
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/infanity_ussr.png", UriKind.Relative));
                    }
                    else
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/infanity_germany.png", UriKind.Relative));
                    }
                }
                else if (CurrentUnit is Artillery)
                {
                    if (CurrentUnit.NameAlliance == TypeAlliance.USSR)
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/art1.png", UriKind.Relative));
                    }
                    else
                    {
                        UnitImage.Source = new BitmapImage(new Uri("/Images/Unit/art2.png", UriKind.Relative));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
            }
        }

        private void Unit_Click(object sender, MouseButtonEventArgs e)
        {
            if (CurrentUnit == null || CurrentUnit.IsDead) return;

            var gameManager = GameManager.Instance; // Используем GameManager вместо MainWindow

            if (gameManager.CurrentPhase == GamePhase.PlayerTurn)
            {
                if (gameManager.SelectedUnit == null)
                {
                    // Выбираем юнита
                    gameManager.SelectUnit(CurrentUnit);

                    // Визуально выделяем выбранного юнита
                    this.BorderBrush = System.Windows.Media.Brushes.Gold;
                    this.BorderThickness = new Thickness(3);
                }
                else if (gameManager.SelectedUnit.NameAlliance != CurrentUnit.NameAlliance)
                {
                    // Атакуем врага
                    gameManager.AttackWithSelectedUnit(CurrentUnit);
                }
                else
                {
                    // Снимаем выделение
                    gameManager.DeselectUnit();
                }
            }

            e.Handled = true;
        }

        // Очищаем подписки при выгрузке
        public void Cleanup()
        {
            if (CurrentUnit != null)
            {
                CurrentUnit.OnHealthChanged -= UpdateHealthBar;
                CurrentUnit.OnUnitDied -= OnUnitDied;
            }
        }
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Cleanup();
        }

    }
}
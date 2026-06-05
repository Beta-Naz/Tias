using System.ComponentModel;
using System.Runtime.CompilerServices;
using TIAS.Core.Models;
using TIAS.Elements;

namespace TIAS.ClassView.Pages
{
    public class SelectLevelView
    {

        private HexMap _levelMap;
        public HexMap LevelMap
        {
            get 
            { 
                return _levelMap; 
            }
            set
            {
                if(value == null)
                {
                    return;
                }
                if(_levelMap != value)
                {
                    _levelMap = value;
                    LevelName = $"Level №{_levelMap.Id}";
                    SizeLevel = $@"{_levelMap.Width}x{_levelMap.Height}";
                    CountUnits = $"{_levelMap.Units.Count}";
                }
            }
        }
        private string _levelName { get; set; }
        public string LevelName
        {
            get
            {
                return _levelName;
            }
            set
            {
                if (_levelName != value)
                {
                    _levelName = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _sizeLevel { get; set; }
        public string SizeLevel
        {
            get
            {
                return _sizeLevel;
            }
            set
            {
                if (SizeLevel != value)
                {
                    _sizeLevel = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _countUnits { get; set; }
        public string CountUnits
        {
            get
            {
                return _countUnits;
            }
            set
            {
                if (_countUnits != value)
                {
                    _countUnits = value;
                    OnPropertyChanged();
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

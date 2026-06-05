using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TIAS.Core.Models;
using TIAS.Elements;

namespace TIAS.Models
{
    public class LevelModel : INotifyPropertyChanged
    {
        public HexMap LevelMap { get; set; }
        private string _levelName {  get; set; }
        public string LevelName
        {
            get 
            { 
                return _levelName; 
            }
            set
            {
                if(_levelName != value)
                {
                    _levelName = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _levelDescription { get; set; }
        public string LevelDescription
        {
            get
            {
                return _levelDescription;
            }
            set
            {
                if(_levelDescription != value)
                {
                    _levelDescription = value;
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

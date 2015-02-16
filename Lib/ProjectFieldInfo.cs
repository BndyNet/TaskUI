// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 2/16/2015 11:49:11 PM
// ---------------------------------------------------------------------------------
// Summary & Changes
// =================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskUI.Lib
{
    public class ProjectFieldInfo : INotifyPropertyChanged
    {
        private string _name;
        private string _value;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public ProjectFieldInfo(string name)
        {
            this.Name = name;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

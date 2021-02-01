using HueShift2.Configuration.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2
{
    public interface IConfigFileHelper
    {
        public void AddOrUpdateSetting<T>(string filePath, string key, T value);
    }
}

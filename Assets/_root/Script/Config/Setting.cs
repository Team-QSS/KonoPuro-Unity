using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Config_Manager
{
    public static class Settings
    {
        public class Data
        {
            public float SoundVolume = 1f;
            public float Light = 1f;
            public int FPS_Limit = 60;
        }
        public static void Save()
        {
            Data Setting = ConfigManager.ConfigData;
            var stream = new FileStream(Application.dataPath + "/Config.json", FileMode.Create);
            var jsonData = JsonConvert.SerializeObject(Setting);
            var data = Encoding.UTF8.GetBytes(jsonData);
            stream.Write(data, 0, data.Length);
            stream.Close();
        }
        public static void Load()
        {
            try
            {
                var stream = new FileStream(Application.dataPath + "/Config.json", FileMode.Open);
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                stream.Close();
                var jsonData = Encoding.UTF8.GetString(data);
                ConfigManager.ConfigData = JsonConvert.DeserializeObject<Data>(jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError("Create New Save File.");
                ConfigManager.ConfigData = new Data() { FPS_Limit = 60, Light = 1, SoundVolume = 1 };
                Save();
                var stream = new FileStream(Application.dataPath + "/Config.json", FileMode.Open);
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                stream.Close();
                var jsonData = Encoding.UTF8.GetString(data);
                ConfigManager.ConfigData = JsonConvert.DeserializeObject<Data>(jsonData);
            }
        }
    }
}

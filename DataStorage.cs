using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Telegram_Bot
{
    public class DataStorage
    {
        private Dictionary<string, HomeObject> Data { get; set; } = new Dictionary<string, HomeObject>();

        public DataStorage()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(CommonInfo.MainFolderPath);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            FileInfo data = new FileInfo(CommonInfo.JsonDatabasePath);

            if (!data.Exists)
            {
                var emptyData = new Dictionary<string, HomeObject>();
                Save(emptyData);
            }
            else
            {
                ReadAll(); // Записать в поле Data сохранённую на диске базу.
            }
        }

        /// <summary>
        /// Считывает базу с диска.
        /// </summary>
        /// <returns>Всю базу целиком</returns>
        public Dictionary<string, HomeObject> ReadAll()
        {
            // read from disk
            var text = File.ReadAllText(CommonInfo.JsonDatabasePath);

            // return entire dictionary
            Data = JsonConvert.DeserializeObject<Dictionary<string, HomeObject>>(text);
            return Data;
        }

        private void Save(Dictionary<string, HomeObject> data)
        {
            var save = JsonConvert.SerializeObject(data);
            File.WriteAllText(CommonInfo.JsonDatabasePath, save);
        }

        public void AddRecord(string key, HomeObject value)
        {
            Data.Add(key, value);
            Save(Data);
        }

        public void DeleteRecord(string key)
        {
            Data.Remove(key);
            Save(Data);
        }
    }
}

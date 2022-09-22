using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Bot
{
    public static class WorkingWithCategories
    {
        /// <summary>
        /// Возвращает все объекты категории, приведённые к строке, в Списке.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static List<string> GetAllItems(List<HomeObject> category)
        {
            List<string> allItems = new List<string>();

            foreach (var item in category)
            {
                allItems.Add(item.ToString());
            }

            return allItems;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram_Bot
{
    public class Categories
    {
        public Categories()
        {
            Food = new List<HomeObject>
            {
                new HomeObject { Category = Category.Food, Name = "Кофе", AproxLeftAmount = AproxLeft.ALot },
                new HomeObject { Category = Category.Food, Name = "Молоко", AproxLeftAmount = AproxLeft.ALot },
                new HomeObject { Category = Category.Food, Name = "Яйца", AproxLeftAmount = AproxLeft.Normal },
                new HomeObject { Category = Category.Food, Name = "Помидоры", AproxLeftAmount = AproxLeft.AlmostNone },
                new HomeObject { Category = Category.Food, Name = "Мороженое", AproxLeftAmount = AproxLeft.Normal },
                new HomeObject { Category = Category.Food, Name = "Бананы", AproxLeftAmount = AproxLeft.Little },
                new HomeObject { Category = Category.Food, Name = "Джем", AproxLeftAmount = AproxLeft.None },
                new HomeObject { Category = Category.Food, Name = "Сметана", AproxLeftAmount = AproxLeft.ALot },
                new HomeObject { Category = Category.Food, Name = "Огурцы", AproxLeftAmount = AproxLeft.Normal },
                new HomeObject { Category = Category.Food, Name = "Ваниль", AproxLeftAmount = AproxLeft.Little },
            };

            //HouseholdChemistry = new List<HomeObject>
            //{
            //    new HomeObject { Name = "Средство для мытья посуды Fairy", Amount = 1, AproxLeftAmount = AproxLeft.Normal },
            //    new HomeObject { Name = "Средство для мытья полов Domestos", Amount = 1, AproxLeftAmount = AproxLeft.ALot }
            //};

            //Cosmetics = new List<HomeObject>
            //{
            //    new HomeObject { Name = "Бепантен", Amount = 1, AproxLeftAmount = AproxLeft.AlmostNone },
            //    new HomeObject { Name = "Офломелид", Amount = 2, AproxLeftAmount = AproxLeft.AboveNormal },
            //};
        }

        public List<HomeObject> Food { get; set; }
        public List<HomeObject> HouseholdChemistry { get; set; }
        public List<HomeObject> Cosmetics { get; set; }
      
    }    
}

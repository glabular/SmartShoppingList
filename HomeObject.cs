using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading;
using Telegram.Bot.Types.InputFiles;
using System.Collections.Generic;

namespace Telegram_Bot
{
    public class HomeObject    
    {
        public string Name { get; set; }

        public AproxLeft AproxLeftAmount { get; set; }

        public Category Category { get; set; }

        public static Category GetCategoryFromString(string theString)
        {
            return theString.ToLower() switch
            {
                "еда" => Category.Food,
                "вкусняшки" => Category.Goodies,
                "бытовая химия" => Category.HouseHoldChemistry,
                "приправы" => Category.Spices,
                "косметика" => Category.Cosmetics,
                "быт" => Category.Household,
                "лекарства" => Category.Drugs,
                _ => Category.Different,
            };
        }

        public static AproxLeft GetAmountFromString(string theString)
        {
            return theString.ToLower() switch
            {
                "почти ничего" => AproxLeft.AlmostNone,
                "немного" => AproxLeft.Little,
                "достаточно" => AproxLeft.Normal,
                "много" => AproxLeft.ALot,
                _ => AproxLeft.None,
            };
        }

        public override string ToString()
        {
            var leftString = string.Empty;

            switch (AproxLeftAmount)
            {
                case AproxLeft.AlmostNone:
                    leftString = "почти ничего не";
                    return $"{Name} - {leftString} осталось ";
                case AproxLeft.None:
                    leftString = "ничего не";
                    return $"{Name} - {leftString} осталось ";
                case AproxLeft.Little:
                    leftString = "немного";
                    break;
                case AproxLeft.Normal:
                    leftString = "достаточно";
                    break;
                case AproxLeft.ALot:
                    leftString = "много";
                    break;                
                default:
                    break;
            }

            return $"{Name} - осталось {leftString}";
        }

        public static bool NewObjCreated { get; set; }
        public static string NewObjName { get; set; }
        public static string NewObjCategory { get; set; }
        public static string NewObjAmount { get; set; }
        public static string DelObjCategory { get; set; } // Временное поле, хранит категорию объекта, который будет удалён.
        public static string DelObjName { get; set; } // Временное поле, хранит имя объекта, который будет удалён.

    }

    public enum AproxLeft
    {
        None, // ничего
        AlmostNone, // почти ничего
        Little, // немного
        Normal, // достаточно
        ALot // много
    }

    public enum Category
    {
        Food, 
        HouseHoldChemistry,
        Cosmetics,
        Spices,
        Goodies,
        Drugs,
        Household, // Быт.
        Different
    }
}

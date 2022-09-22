using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram_Bot
{
    public static class Keyboards
    {
        public static async Task MainMenuKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Посмотреть категорию", callbackData: ButtonsGUID.ShowCategory),
                        InlineKeyboardButton.WithCallbackData(text: "Что купить?", callbackData: ButtonsGUID.Buy)
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Изменить количество", callbackData: ButtonsGUID.RefreshAmount),
                        InlineKeyboardButton.WithCallbackData(text: "Удалить объект", callbackData: ButtonsGUID.DelObject),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Добавить объект", callbackData: ButtonsGUID.AddObject),
                    },
            });

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Главное меню:",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }
               

        public static async Task CategoriesKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Еда", "Приправы", "Бытовая химия" },
                new KeyboardButton[] { "Вкусняшки", "Косметика", "Быт" },
                new KeyboardButton[] { "Лекарства", "Разное", "Отмена"}
            })
            {
                ResizeKeyboard = true
            };

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите категорию",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        public static async Task RemoveKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            //To remove keyboard you have to send an instance of ReplyKeyboardRemove object:
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "↓↓↓↓↓↓↓↓↓",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        public static async Task AmountKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Ничего", "Почти ничего" },
                new KeyboardButton[] { "Немного", "Достаточно" },
                new KeyboardButton[] { "Много", "Отмена" }
            })
            {
                ResizeKeyboard = true
            };

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите количество",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }

        public static async Task CustomKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, List<string> buttons, string textline)
        {
            int columnsNumber = 2;
            var kb = new List<List<KeyboardButton>>();
            var row = new List<KeyboardButton>();

            var currentColumn = 1;

            for (int i = 0; i < buttons.Count; i++)
            {
                string buttonText = buttons[i];
                row.Add(buttonText);

                if (currentColumn == columnsNumber || i == buttons.Count - 1)
                {
                    kb.Add(row);
                    row = new List<KeyboardButton>();
                    currentColumn = 1;
                }
                else
                {
                    currentColumn++;
                }
            }

            var rkm = new ReplyKeyboardMarkup(kb)
            {
                ResizeKeyboard = true
            };

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: textline,
                replyMarkup: rkm,
                cancellationToken: cancellationToken);
        }
    }
}

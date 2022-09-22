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
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace Telegram_Bot
{
    public class Program
    {
        private static Random _rand = new Random();
        private static string _lastButtonPressed;

        private static DataStorage DS { get; set; }

        static async Task Main(string[] args)
        {
            DS = new DataStorage();

            var botClient = new TelegramBotClient("5095930902:AAGdpfiZouFU1-J2Egn8rvkG2NJoDPflIXk");
            using var cts = new CancellationTokenSource();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);
            Console.ReadLine();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            long chatId = 0;

            switch (update.Type)
            {
                case UpdateType.Unknown:
                    break;
                case UpdateType.Message:
                    if (update.Message!.Type != MessageType.Text) // Only process text mesages
                    {
                        return;
                    }
                    else
                    {
                        chatId = update.Message.Chat.Id;
                        var messageText = update.Message.Text;

                        await TextInputProcessorAsync(messageText, botClient, chatId, cancellationToken);

                        if (_lastButtonPressed == ButtonsGUID.AddObject && !HomeObject.NewObjCreated)
                        {
                            await GetUserInputForNewObjAsync(botClient, chatId, cancellationToken, messageText);
                        }

                        if (_lastButtonPressed == ButtonsGUID.DelObject)
                        {
                            await GetUserInputToDeleteObjAsync(botClient, chatId, cancellationToken, messageText);
                        }

                        if (_lastButtonPressed == ButtonsGUID.ShowCategory)
                        {
                            await GetUserInputToShowCategoriesAsync(botClient, chatId, cancellationToken, messageText);
                        }

                        if (_lastButtonPressed == ButtonsGUID.RefreshAmount)
                        {
                            await GetUserInputToRefreshAmountAsync(botClient, chatId, cancellationToken, messageText);
                        }

                        Console.WriteLine($"[LOG][{DateTime.Now}] Received a '{messageText}' message in chat {chatId}.");
                    }
                    break;
                case UpdateType.CallbackQuery:
                    chatId = update.CallbackQuery.Message.Chat.Id;
                    var pressedButtonID = update.CallbackQuery.Data;
                    Console.WriteLine($"[LOG][{DateTime.Now}] Pressed button = {pressedButtonID}");

                    await PressedButtonProcessorAsync(botClient, chatId, cancellationToken, pressedButtonID);

                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id); // Убирает часики, появляющиеся после нажатия на инлайн кнопку.
                    break;
                default:
                    break;
            }
        }

        private static async Task GetUserInputToRefreshAmountAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string messageText)
        {
            var data = DS.ReadAll();

            await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);

            if (CommonInfo.CategoryRefreshAmount == null) // В какую категорию будет внесено изменение.
            {
                if (messageText == "Отмена")
                {
                    NullRefresh();
                    await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }

                CommonInfo.CategoryRefreshAmount = messageText;

                var categoryList = new List<string>();
                var category = HomeObject.GetCategoryFromString(CommonInfo.CategoryRefreshAmount);

                var emptyCategory = true;

                foreach (var item in data.Values)
                {
                    if (item.Category == category)
                    {
                        categoryList.Add(item.Name);
                        emptyCategory = false;
                    }
                }

                if (emptyCategory)
                {
                    await SendTextInChat(botClient, chatId, cancellationToken, "В данной категории ничего нет.");
                    CommonInfo.CategoryRefreshAmount = null;
                    CommonInfo.ItemRefreshAmount = null;
                    CommonInfo.NewAmount = null;
                    _lastButtonPressed = string.Empty;
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                }
                else
                {
                    categoryList.Add("Отмена");
                    await Keyboards.CustomKeyboard(botClient, chatId, cancellationToken, categoryList, "Выберите объект");
                }

                return;
            }

            if (CommonInfo.ItemRefreshAmount == null) // Какой конкретно объект будет изменён.
            {
                if (messageText == "Отмена")
                {
                    NullRefresh();
                    await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }

                CommonInfo.ItemRefreshAmount = messageText;
                await Keyboards.AmountKeyboard(botClient, chatId, cancellationToken);
                return;
            }

            if (CommonInfo.NewAmount == null) // Новое количество, которое будет приписано объекту.
            {
                if (messageText == "Отмена")
                {
                    NullRefresh();
                    await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }

                CommonInfo.NewAmount = messageText;
            }

            var amount = HomeObject.GetAmountFromString(CommonInfo.NewAmount);

            var tempObj = data.GetValueOrDefault(CommonInfo.ItemRefreshAmount);

            tempObj.AproxLeftAmount = amount;

            DS.DeleteRecord(CommonInfo.ItemRefreshAmount);
            DS.AddRecord(tempObj.Name, tempObj);

            await SendTextInChat(botClient, chatId, cancellationToken, $"{CommonInfo.ItemRefreshAmount}: количество успешно изменено на \"{CommonInfo.NewAmount}\".\nВызов главного меню.");

            NullRefresh();

            await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
        }

        private static void NullRefresh()
        {
            CommonInfo.CategoryRefreshAmount = null;
            CommonInfo.ItemRefreshAmount = null;
            CommonInfo.NewAmount = null;
            _lastButtonPressed = string.Empty;
        }

        private static async Task GetUserInputToShowCategoriesAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string userInputCategory)
        {
            await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);

            if (userInputCategory == "Отмена")
            {
                await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
            }
            else
            {
                await PrintCategoryInChat(botClient, chatId, cancellationToken, userInputCategory);
            }

        }

        /// <summary>
        /// Отправляет в чат все объекты, которые находятся в определённой категории в одном сообщении.
        /// Отправляет в чат соответствующее сообщение, если категория пуста. 
        /// </summary>
        private static async Task PrintCategoryInChat(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string userInputCategory)
        {
            var category = HomeObject.GetCategoryFromString(userInputCategory);
            var data = DS.ReadAll();
            var outputString = new StringBuilder();
            var emptyCategory = true;

            foreach (var item in data.Values)
            {
                if (item.Category == category)
                {
                    outputString.Append(item.ToString() + "\n");
                    emptyCategory = false;
                }
            }

            if (emptyCategory)
            {
                await SendTextInChat(botClient, chatId, cancellationToken, "В данной категории ничего нет.");
                _lastButtonPressed = string.Empty;
            }
            else
            {
                await SendTextInChat(botClient, chatId, cancellationToken, outputString.ToString());
            }

            await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
        }

        private static async Task GetUserInputToDeleteObjAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string messageText)
        {
            await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);

            if (messageText == "Отмена")
            {
                await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                _lastButtonPressed = string.Empty;
                HomeObject.DelObjCategory = null;
                await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                return;
            }

            if (HomeObject.DelObjCategory == null)
            {
                HomeObject.DelObjCategory = messageText;
                var category = HomeObject.GetCategoryFromString(messageText);
                var data = DS.ReadAll();
                var isCategoryEmpty = true;
                var itemsList = new List<string>();

                foreach (var item in data.Values)
                {
                    if (item.Category == category)
                    {
                        itemsList.Add(item.Name);
                        isCategoryEmpty = false;                        
                    }
                }   
                
                if (isCategoryEmpty)
                {
                    await SendTextInChat(botClient, chatId, cancellationToken, "Данная категория пуста.");
                    _lastButtonPressed = string.Empty;
                    HomeObject.DelObjCategory = null;
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                }
                else
                {
                    itemsList.Add("Отмена");
                    await Keyboards.CustomKeyboard(botClient, chatId, cancellationToken, itemsList, "Выберите объект, который нужно удалить");
                }

                return;
            }

            if (messageText == "Отмена")
            {
                await SendTextInChat(botClient, chatId, cancellationToken, "Удаление отменено.");
                _lastButtonPressed = string.Empty;
                HomeObject.DelObjCategory = null;
                await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                return;
            }

            var tmpData = DS.ReadAll();

            foreach (var item in tmpData.Keys)
            {
                if (messageText == item)
                {
                    DS.DeleteRecord(item);
                    await SendTextInChat(botClient, chatId, cancellationToken, $"Объект {item} успешно удалён.");
                    _lastButtonPressed = string.Empty;
                    HomeObject.DelObjCategory = null;
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }       
            }

            await SendTextInChat(botClient, chatId, cancellationToken, "Объект не найден в базе.");
            await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);                      

            _lastButtonPressed = string.Empty;
            HomeObject.DelObjCategory = null;
        }

        private static async Task PressedButtonProcessorAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string pressedButton)
        {
            switch (pressedButton)
            {
                case ButtonsGUID.Buy:
                    await ProcessBuyButton(botClient, chatId, cancellationToken); // Кнопка "что купить?"
                    break;
                case ButtonsGUID.RefreshAmount:
                    await ProcessRefreshAmountButton(botClient, chatId, cancellationToken); // Кнопка "Изменить количество"
                    break;
                case ButtonsGUID.AddObject:                    
                    await ProcessAddObjectButton(botClient, chatId, cancellationToken); // Кнопка "Добавить объект"
                    break;
                case ButtonsGUID.DelObject:
                    await ProcessDelObjectButton(botClient, chatId, cancellationToken); // Кнопка "Удалить объект"
                    break;
                case ButtonsGUID.ShowCategory:
                    await ProcessShowCategoryButton(botClient, chatId, cancellationToken); // Кнопка "Посмотреть категорию"
                    break;
                default:
                    break;
            }
        }

        private static async Task ProcessShowCategoryButton(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            _lastButtonPressed = ButtonsGUID.ShowCategory;
            await Keyboards.CategoriesKeyboard(botClient, chatId, cancellationToken);
        }

        private static async Task ProcessDelObjectButton(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            _lastButtonPressed = ButtonsGUID.DelObject;
            await SendTextInChat(botClient, chatId, cancellationToken, "Выберите категорию объекта, который нужно удалить.");
            await Keyboards.CategoriesKeyboard(botClient, chatId, cancellationToken);
        }

        /// <summary>
        /// Записывает текст, введённый пользователем, в специальную переменную для последующего использования при создании нового объекта.
        /// </summary>
        /// <param name="text">введённый пользователем текст</param>
        private static async Task GetUserInputForNewObjAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string text)
        {
            if (text.ToLower() == "отмена")
            {
                ResetNewObject();
                _lastButtonPressed = string.Empty;
                await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);
                await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                return;
            }

            var data = DS.ReadAll();            

            if (HomeObject.NewObjName == null)
            {
                var keysToLower = new List<string>();

                foreach (var item in data.Keys)
                {
                    keysToLower.Add(item.ToLower());
                }

                if (keysToLower.Contains(text.ToLower()))
                {
                    await SendTextInChat(botClient, chatId, cancellationToken, $"Ошибка: данный объект уже находится в списке:\n{data.GetValueOrDefault(text).ToString()}");
                    await SendTextInChat(botClient, chatId, cancellationToken, "Вызов главного меню.");
                    ResetNewObject();
                    _lastButtonPressed = string.Empty;
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }
                else
                {
                    HomeObject.NewObjName = text;
                    await Keyboards.CategoriesKeyboard(botClient, chatId, cancellationToken);
                    return;
                }
            }

            if (HomeObject.NewObjCategory == null)
            {
                if (text.ToLower() == "отмена")
                {
                    ResetNewObject();
                    _lastButtonPressed = string.Empty;
                    await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);
                    await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }

                HomeObject.NewObjCategory = text;
                await Keyboards.AmountKeyboard(botClient, chatId, cancellationToken);
                return;
            }

            if (HomeObject.NewObjAmount == null)
            {
                if (text.ToLower() == "отмена")
                {
                    ResetNewObject();
                    _lastButtonPressed = string.Empty;
                    await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);
                    await SendTextInChat(botClient, chatId, cancellationToken, "Операция отменена");
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    return;
                }

                HomeObject.NewObjAmount = text;
            }            

            var category = HomeObject.GetCategoryFromString(HomeObject.NewObjCategory);
            var amount = HomeObject.GetAmountFromString(HomeObject.NewObjAmount);

            var toAdd = new HomeObject { Name = HomeObject.NewObjName, Category = category, AproxLeftAmount = amount }; // Создание нового объекта для добавления в базу данных
            
            DS.AddRecord(HomeObject.NewObjName, toAdd); // объект добавлен в базу данных.
            await SendTextInChat(botClient, chatId, cancellationToken, "Объект добавлен:");
            await SendTextInChat(botClient, chatId, cancellationToken, toAdd.ToString());

            ResetNewObject();

            await Keyboards.RemoveKeyboard(botClient, chatId, cancellationToken);
            _lastButtonPressed = string.Empty;

            await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
        }

        

        private static async Task ProcessAddObjectButton(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            _lastButtonPressed = ButtonsGUID.AddObject;
            await SendTextInChat(botClient, chatId, cancellationToken, "Введите имя добавляемого объекта или 'Отмена' для отмены операции.");
        }

        private static async Task ProcessRefreshAmountButton(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            _lastButtonPressed = ButtonsGUID.RefreshAmount;
            await SendTextInChat(botClient, chatId, cancellationToken, "Чтобы внести изменения...");
            await Keyboards.CategoriesKeyboard(botClient, chatId, cancellationToken);
        }

        private static async Task ProcessBuyButton(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var data = DS.ReadAll();

            var urge1 = new List<HomeObject>();
            var urge2 = new List<HomeObject>();
            var urge3 = new List<HomeObject>();
            var urge4 = new List<HomeObject>();
            var urge5 = new List<HomeObject>();

            foreach (var item in data.Values)
            {
                switch (item.AproxLeftAmount)
                {
                    case AproxLeft.None:
                        urge1.Add(item);
                        break;
                    case AproxLeft.AlmostNone:
                        urge2.Add(item);
                        break;
                    case AproxLeft.Little:
                        urge3.Add(item);
                        break;
                    case AproxLeft.Normal:
                        urge4.Add(item);
                        break;
                    case AproxLeft.ALot:
                        urge5.Add(item);
                        break;
                    default:
                        break;
                }
            }
            
            if (urge1.Count != 0)
            {
                var output = new StringBuilder();
                output.Append("Ничего не осталось:");

                foreach (var item in urge1)
                {
                    output.Append($"\n{item.Name}");
                }

                await SendTextInChat(botClient, chatId, cancellationToken, output.ToString());
            }

            if (urge2.Count != 0)
            {
                var output = new StringBuilder();
                output.Append("Почти ничего не осталось:");

                foreach (var item in urge2)
                {
                    output.Append($"\n{item.Name}");
                }

                await SendTextInChat(botClient, chatId, cancellationToken, output.ToString());
            }

            if (urge3.Count != 0)
            {
                var output = new StringBuilder();
                output.Append("Осталось немного:");

                foreach (var item in urge3)
                {
                    output.Append($"\n{item.Name}");
                }

                await SendTextInChat(botClient, chatId, cancellationToken, output.ToString());
            }

            if (urge4.Count != 0)
            {
                var output = new StringBuilder();
                output.Append("Осталось достаточно:");

                foreach (var item in urge4)
                {
                    output.Append($"\n{item.Name}");
                }

                await SendTextInChat(botClient, chatId, cancellationToken, output.ToString());
            }

            if (urge5.Count != 0)
            {
                var output = new StringBuilder();
                output.Append("Осталось много:");

                foreach (var item in urge5)
                {
                    output.Append($"\n{item.Name}");
                }

                await SendTextInChat(botClient, chatId, cancellationToken, output.ToString());
            }

            await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
        }

        private static async Task TextInputProcessorAsync(string messageText, ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var input = messageText.ToLower();

            switch (input)
            {
                case "/start":
                    _lastButtonPressed = string.Empty;
                    await Keyboards.MainMenuKeyboard(botClient, chatId, cancellationToken);
                    break;
                case "/test":
                    _lastButtonPressed = string.Empty;
                    
                    break;
                default:
                    break;
            }
        }

        private static async Task RespondWithCallbackButtons(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "Кухня", callbackData: ButtonsGUID.ShowCategories),
                        InlineKeyboardButton.WithCallbackData(text: "Ванная и туалет", callbackData: "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(text: "2.1", callbackData: "21"),
                        InlineKeyboardButton.WithCallbackData(text: "2.2", callbackData: "22"),
                    },
            });

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "A message with an inline keyboard markup",
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken);
        }

        private static async Task RemoveKeyboard(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            //To remove keyboard you have to send an instance of ReplyKeyboardRemove object:
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Removing keyboard",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        private static async Task RequestLocationOrContact(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                KeyboardButton.WithRequestLocation("Share Location"),
                KeyboardButton.WithRequestContact("Share Contact"),
            });

            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Who or Where are you?",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }


        private static async Task RespondWithPoll(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message pollMessage = await botClient.SendPollAsync(
                chatId: chatId,
                question: "Did you ever hear the tragedy of Darth Plagueis The Wise?",
                options: new[]
                {
                    "Yes for the hundredth time!",
                    "No, who`s that?"
                },
                cancellationToken: cancellationToken);
            return;
        }

        private static async Task RespondWithAnimation(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken) // Send animation files (GIF or H.264/MPEG-4 AVC video without sound).
        {
            Message message = await botClient.SendAnimationAsync(
                chatId: chatId,
                animation: "https://raw.githubusercontent.com/TelegramBots/book/master/src/docs/video-waves.mp4",
                caption: "Waves",
                cancellationToken: cancellationToken);
        }

        private static async Task RespondWithDocument(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message message = await botClient.SendDocumentAsync(
                            chatId: chatId,
                            document: "https://sun4-16.userapi.com/impg/Zt734Mquz-DR4Mk2vwl5dhU_dp4-Y7EmUd8d3g/17mq4XoZy2g.jpg?size=1000x1470&quality=95&sign=78265ccda02f1d4f6703bec80a9837de&type=album",
                            caption: "<b>Ara bird</b>. <i>Source</i>: <a href=\"https://pixabay.com\">Pixabay</a>",
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken);
        }

        private static async Task RespondWithVideo(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message message = await botClient.SendAnimationAsync(
                chatId: chatId,
                animation: "https://raw.githubusercontent.com/TelegramBots/book/master/src/docs/video-waves.mp4",
                caption: "Waves",
                cancellationToken: cancellationToken);
        }

        private static async Task RespondWithGroupOfPhotos(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message[] messages = await botClient.SendMediaGroupAsync(
                chatId: chatId,
                media: new IAlbumInputMedia[]
                        {
                        new InputMediaPhoto("https://cdn.pixabay.com/photo/2017/06/20/19/22/fuchs-2424369_640.jpg"),
                        new InputMediaPhoto("https://cdn.pixabay.com/photo/2017/04/11/21/34/giraffe-2222908_640.jpg"),
                        },
                    cancellationToken: cancellationToken);
        }

        private static async Task RespondWithVideomessage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            Message message;
            using (var stream = System.IO.File.OpenRead("video-waves.mp4")) // Имя файла видеосообщения в папке с exe файлом проекта.
            {
                message = await botClient.SendVideoNoteAsync(
                    chatId: chatId,
                    videoNote: stream,
                    duration: 47,
                    length: 360, // value of width/height
                    cancellationToken: cancellationToken);
            }
        }

        private static async Task RespondWithTextAndLink(ITelegramBotClient botClient, Update update, long chatId, CancellationToken cancellationToken)
        {
            // SendTextMessageAsync method sends a text message and returns the message object sent.
            Message message = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: @"Any text\. It is situated\. *In the message, that bot sends*\. `You can try insert some text here`\.",
                parseMode: ParseMode.MarkdownV2,
                disableNotification: true, // Tell Telegram client on user's device not to show/sound a notification.
                replyToMessageId: update.Message.MessageId,
                replyMarkup: new InlineKeyboardMarkup(
                InlineKeyboardButton.WithUrl(
                    "Click to open YouTube", // The text on the button.
                    "https://www.youtube.com/")), //The URL to open.
                    cancellationToken: cancellationToken);
            ShowInformationAboutTheMessage(message);
        }

        private static void ShowInformationAboutTheMessage(Message message)
        {
            Console.WriteLine(
                $"{message.From.FirstName} sent message {message.MessageId} " +
                $"to chat {message.Chat.Id} at {message.Date}. " + // in UTC format and not your local timezone. Convert it to local time by calling message.Date.ToLocalTime() method.
                $"It is a reply to message {message.ReplyToMessage.MessageId} " +
                $"and has {message.Entities.Length} message entities."
            );
        }

        private static async Task EchoReceivingMessageText(ITelegramBotClient botClient, long chatId, string messageText, CancellationToken cancellationToken)
        {
            List<string> variantsOfPreMessage = new List<string>()
            {
                "You said:\n",
                "You just said that:\n",
                "I'm pretty sure you said that:\n",
                "Wow! You said:\n",
            };

            Message sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: variantsOfPreMessage[_rand.Next(4)] + messageText,
                            cancellationToken: cancellationToken);

            
        }

        private static string ChoseRandomSticker()
        {            
            int chosenOne = _rand.Next(3);
            List<string> stickersDatabase = new List<string>()
            {
                "https://chpic.su/_data/stickers/s/SportGuy/SportGuy_018.webp",
                "https://github.com/TelegramBots/book/raw/master/src/docs/sticker-dali.webp",
                "https://chpic.su/_data/archived/stickers/s/su/SuchAGentleman.webp"
            };

            return stickersDatabase[chosenOne];
        }

        private static async Task SendTextInChat(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string textToSend)
        {
            Message sentMessage = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: textToSend,
                            cancellationToken: cancellationToken);
        }

        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Обнуляет все временные поля, из которых берётся информация для создания нового объекта.
        /// </summary>
        private static void ResetNewObject()
        {
            HomeObject.NewObjName = null;
            HomeObject.NewObjCategory = null;
            HomeObject.NewObjAmount = null;
            HomeObject.NewObjCreated = false;
        }
    }
}
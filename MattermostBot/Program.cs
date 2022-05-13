﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using ApiAdapter;
using ApiClient;
using Microsoft.Extensions.Configuration;

namespace MattermostBot
{
  class MattermostBot
  {
    #region Константы

    /// <summary>
    /// Количество дней до предупреждения по умолчанию.
    /// </summary>
    const int DaysBeforeWarningByDefault = 7;

    /// <summary>
    /// Количество дней до отпинивания сообщения по умолчанию.
    /// </summary>
    const int DaysBeforeUnpiningByDefault = 3;

    ///<summary>
    /// Период проверки канала в минутах по умолчанию.
    ///</summary>
    const int ChannelCheckPeriodInMinutesbyDefault = 60;

    /// <summary>
    /// Текст предупреждения.
    /// </summary>
    const string WarningTextMessage = "Новых сообщений не было уже больше {0} дней. Закрываем консультацию?";

    /// <summary>
    /// Текст при отпинивании сообщения.
    /// </summary>
    const string UnpiningTextMessage = "Консультация закрыта.";

    /// <summary>
    /// Название эмодзи. 
    /// </summary>
    const string EmojiName = "no_entry_sign";       

    #endregion

    #region Вложенные типы   

    /// <summary>
    /// Информация о запиненном сообщении.
    /// </summary>
    private class MessageInfo
    {
      public string id;
      public MessageAction action;
    }

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Uri сервера Mattermost.
    /// </summary>
    private static string mattermostUri;

    /// <summary>
    /// Токен личного доступа бота.
    /// </summary>
    private static string accessToken;

    /// <summary>
    /// Клиент для работы со маттермостом.
    /// </summary>
    private static IApiClient mattermostApi;

    /// <summary>
    /// Информация о всех каналах.
    /// </summary>
    private static readonly List<ChannelInfo> channelsInfo = new List<ChannelInfo>();

    /// <summary>
    /// Список из task`ов, окончание которых нужно дождаться
    /// </summary>
    private static readonly List<Task> tasks = new List<Task>();

    /// <summary>
    /// ID пользователя бота.
    /// </summary>
    private static string botUserID;

    /// <summary>
    /// Период проверки канала в минутах.
    /// </summary>
    private static int channelCheckPeriodInMinutes;

    ///<summary>
    ///Путь к папке с сохраненными тредами
    ///</summary>
    private static string pathToDownloadDirectory;

    /// <summary>
    /// Действие, которое надо совершить над запиненным сообщением.
    /// </summary>
    private enum MessageAction
    {
      NeedUnpin,
      NeedWarning,
      DoNothing
    }

    #endregion

    #region Методы

    /// <summary>
    /// Попытаться конвертировать строку в число.
    /// </summary>
    /// <param name="paramName">Имя параметра.</param>
    /// <param name="value">Конвертируемое значение.</param>
    /// <param name="defaultValue">Значение по умолчанию.</param>
    /// <returns></returns>
    private static int TryConvertStringToInt(string paramName, string value, int defaultValue)
    {
      int resultValue;
      try
      { 
        resultValue = Convert.ToInt32(value);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Произошла ошибка при чтении параметра {paramName}: {ex.Message}. " +
          $"Взято значение по умолчанию {defaultValue}.");
        resultValue = defaultValue;
      }

      return resultValue;
    }

    /// <summary>
    /// Прочитать конфиг.
    /// </summary>
    private static void ReadConfig()
    {
      try
      {
        var config = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

        int i = 0;
        while (config.GetSection($"Channels:{i}:ChannelID").Exists())
        {
          var channelID = config.GetSection($"Channels:{i}:ChannelID").Value;
          var daysBeforeWarning = TryConvertStringToInt("daysBeforeWarning", config.GetSection($"Channels:{i}:DaysBeforeWarning").Value,
            DaysBeforeWarningByDefault);
          var daysBeforeUnpining = TryConvertStringToInt("daysBeforeUnpining", config.GetSection($"Channels:{i}:DaysBeforeUnpining").Value,
            DaysBeforeUnpiningByDefault);
          var autoPinNewMessage = bool.Parse(config.GetSection($"Channels:{i}:AutoPinNewMessage").Value);
          var welcomeMessage = config.GetSection($"Channels:{i}:WelcomeMessage").Value;

          channelsInfo.Add(new ChannelInfo(channelID, daysBeforeWarning, daysBeforeUnpining, autoPinNewMessage, welcomeMessage));
          i++;
        }
        mattermostUri = config["MattermostUri"];
        accessToken = config["AccessToken"];
        botUserID = config["BotUserID"];
        channelCheckPeriodInMinutes = TryConvertStringToInt("ChannelCheckPeriodInMinutes", config["ChannelCheckPeriodInMinutes"],
            ChannelCheckPeriodInMinutesbyDefault);
        pathToDownloadDirectory = config["PathToDownloadDirectory"];
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка чтения конфигурационного файла: {ex.Message}");
        throw;
      }
    }

    public static void Main()
    {
      Console.WriteLine("Работа бота начата.");
      using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
      try
      {
        ReadConfig();
        mattermostApi = new MattermostApiClientBuilder(mattermostUri, accessToken)
          .RegisterNewPostEventHandler(m => NewPostEventHandler(m))
          .RegisterErrorEventHandler(e => Console.WriteLine(e))
          .Connect(cancellationTokenSource.Token);
        while (true)
        {
          foreach(var channelInfo in channelsInfo)
          {
            tasks.Add(Task.Run(() => ProcessPinsList(channelInfo)));
          }
          Task.WaitAll(tasks.ToArray());
          Thread.Sleep(TimeSpan.FromMinutes(channelCheckPeriodInMinutes));
        }
      }
      catch
      {
        cancellationTokenSource.Cancel();
        Console.WriteLine("Работа бота завершилась из-за ошибок.");
      }
    }

    /// <summary>
    /// Обработчик события появления на канале нового сообщения.
    /// </summary>
    /// <param name="messageEventInfo">Информация о событии.</param>
    private static void NewPostEventHandler(MessageEventInfo messageEventInfo)
    {
      if (messageEventInfo.rootID == "")
      {
        foreach (var channelInfo in channelsInfo.Where(info => info.ChannelID == messageEventInfo.channelID))
        {
          if (!string.IsNullOrEmpty(channelInfo.WelcomeMessage))
            mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID, channelInfo.WelcomeMessage);

          if (channelInfo.AutoPinNewMessage)
            mattermostApi.PinMessage(messageEventInfo.id);

          break;
        }
      }
      else 
      {
        if (messageEventInfo.message.Contains('@') == true)
        {
          if (Regex.IsMatch(messageEventInfo.message, @"(?:roberto)", RegexOptions.IgnoreCase))
          {
            if (Regex.IsMatch(messageEventInfo.message, @"(?:download)", RegexOptions.IgnoreCase))
              DownloadThread(messageEventInfo);
            else
              mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, messageEventInfo.userID,string.Format(
                ":rotating_light: Ошибка!!! Роберто не знает команды {0} :rotating_light: \nДоступные команды: {1}",
                messageEventInfo.message.Substring(messageEventInfo.message.IndexOf('@') + "@roberto".Length),"download"));
          }
        }
      }
    }

    /// <summary>
    /// Скачать тред.
    /// </summary>
    /// <param name="messageEventInfo">Информация о событии.</param>
    private static void DownloadThread(MessageEventInfo messageEventInfo)
    {
      Message[] messages = mattermostApi.GetThreadMessages(messageEventInfo.rootID);
      IEnumerable<Message> messagesSorted = GetMessagesSorted(RemoveDupesFromMessages(messages));
      UserInfo rootUserInfo = mattermostApi.GetUserInfoByID(messageEventInfo.userID);

      var pathToThread = GetPathToSaveThread(pathToDownloadDirectory, rootUserInfo, messages[0].dateTime.ToLocalTime());
      try 
      {
        bool needToCreateFilesFolder = false;
        Directory.CreateDirectory(pathToThread);
        var pathToThreadFile = Path.Combine(pathToThread, "thread.txt");
        StreamWriter thread = File.CreateText(pathToThreadFile);
        foreach (Message message in messagesSorted)
        {
          if (message.fileIDs != null)
            needToCreateFilesFolder = true;
          var userInfo = mattermostApi.GetUserInfoByID(message.userId);
          if (userInfo.firstName != null && userInfo.lastName != null)
            thread.WriteLine(message.dateTime.ToLocalTime() + "\n" + userInfo.firstName + " " + userInfo.lastName + ":" + message.message + "\n");
          else
            thread.WriteLine(message.dateTime.ToLocalTime() + "\n" + " (" + userInfo.userName + "):" + message.message + "\n");
          if(message.fileNames != null)
          {
            foreach(var name in message.fileNames)
            {
              thread.WriteLine("Добавлен файл - {0}\n", name.ToString());
            }
          }
        }
        thread.Close();

        if (needToCreateFilesFolder == true)
        {
          var pathToFilesFolder = Path.Combine(pathToThread, "files");
          Directory.CreateDirectory(pathToFilesFolder);
          foreach (Message message in messagesSorted)
          {
            DownloadLinkedFiles(message, pathToFilesFolder);
          }
        }
        mattermostApi.PostEphemeralMessage(messageEventInfo.channelID, rootUserInfo.userID, "Тред скачан и находится здесь: " + pathToThread);
        Console.WriteLine("Тред скачан по адресу: " + pathToThreadFile);
      }

      catch (DirectoryNotFoundException dirEx)
      {
        mattermostApi.PostMessage(messageEventInfo.channelID, ":rotating_light: Ошибка!!! {0} :rotating_light:" +
          dirEx.Message);
        Console.WriteLine("Путь не найден: " + dirEx.Message);
      }
    }

    ///<summary>
    ///Скачать файлы прикрепленные к сообщению.
    ///</summary>
    /// <param name="messageEventInfo">Информация о событии.</param>
    /// <param name="pathToFilesFolder">Путь до папки скачанного треда.</param>
    private static void DownloadLinkedFiles(Message message, string pathToFilesFolder)
    {
      if(message.fileIDs != null)
      {
        var fileIDs = message.fileIDs;
        int i = 0;
        foreach(var fileID in fileIDs)
        {
          string pathToFile = Path.Combine(pathToFilesFolder, message.fileNames[i]);
          mattermostApi.CreateLinkedFile(message.messageId, fileID, pathToFile);
          i++;
        }
      }
    }

    /// <summary>
    /// Отсортировать тред.
    /// </summary>
    /// <param name="messages">Список сообщений в треде.</param>
    private static IEnumerable<Message> GetMessagesSorted(IEnumerable<Message> messages)
    {
      return messages.OrderBy(message => message.dateTime);
    }

    /// <summary>
    /// Убрать дубли сообщений.
    /// Mattermost выдает "дубликат" первого сообщения, поэтому нам надо его удалить.    
    /// </summary>
    /// <param name="messages">Список сообщений в треде.</param>
    private static IEnumerable<Message> RemoveDupesFromMessages(Message[] messages)
    {
      return messages.GroupBy(x => x.messageId).Select(y => y.First());
    }


    /// <summary>
    /// Создать путь для скачивания треда.
    /// </summary>
    /// <param name="pathToDownloadDirectory">Путь до папки со всеми скачанными тредами.</param>
    /// <param name="rootDateTime">Дата создания корневого сообщения треда. </param>
    /// <param name="rootUserInfo">Информация о создателе корневого сообщения треда.</param>
    private static string GetPathToSaveThread(string pathToDownloadDirectory, UserInfo rootUserInfo, DateTime rootDateTime)
    {
      var rootDateTimeString = rootDateTime.ToString("yyyy.MM.dd HH-mm");
      if (rootUserInfo.firstName != null && rootUserInfo.lastName != null)
      {
        var path = Path.Combine(pathToDownloadDirectory, string.Format("{0} {1} {2}", rootDateTimeString, rootUserInfo.firstName, rootUserInfo.lastName));
        return path;
      }
      else
      {
        var path = Path.Combine(pathToDownloadDirectory, string.Format("{0} {1}", rootDateTimeString, rootUserInfo.userName));
        return path;
      }
    }

    /// <summary>
    /// Обработать список запиненных сообщений.
    /// </summary>
    private static void ProcessPinsList(ChannelInfo channelInfo)
    {
      try
      {
        var pinnedMessages = mattermostApi.GetPinnedMessages(channelInfo.ChannelID);
        var oldMessageTSList = GetOldMessageList(pinnedMessages, channelInfo);
        ReplyMessageInOldThreads(oldMessageTSList, channelInfo);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Обработка сообщений завершилась с ошибкой: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Отправить сообщение в тред.
    /// </summary>
    /// <param name="messageInfos">Список запиненных сообщений.</param>
    private static void ReplyMessageInOldThreads(List<MessageInfo> messageInfos, ChannelInfo channelInfo)
    {
      foreach (MessageInfo messageInfo in messageInfos)
      {
        if (messageInfo.action == MessageAction.NeedWarning)
        {
          SendMessage(String.Format(WarningTextMessage, channelInfo.DaysBeforeWarning), messageInfo.id, channelInfo);
        }
        else if (messageInfo.action == MessageAction.NeedUnpin)
        {
          SendMessage(UnpiningTextMessage, messageInfo.id, channelInfo);
          AddEmoji(messageInfo.id);
          UnpinMessage(messageInfo.id);
        }
      }
    }

    /// <summary>
    /// Открепить сообщение.
    /// </summary>
    /// <param name="messageID">ИД закрепленного сообщения.</param>
    private static void UnpinMessage(string messageID)
    {
      mattermostApi.UnpinMessage(messageID);
    }

    /// <summary>
    /// Получить список старых закрепленных сообщений.
    /// </summary>
    /// <param name="pinedMessages">Полный список закрепленных сообщений.</param>
    /// <returns>Список закрепленных сообщений, с момента создания которых прошло больше DaysCountBeforeWarning дней.</returns>
    private static List<MessageInfo> GetOldMessageList(Message[] pinedMessages, ChannelInfo channelInfo)
    {
      var oldPinedMessageList = new List<MessageInfo>();
      if (pinedMessages != null)
      {
        foreach (var pinedMessage in pinedMessages)
        {
          if (IsOldPinedMessage(pinedMessage.dateTime, Math.Min(channelInfo.DaysBeforeWarning, channelInfo.DaysBeforeUnpining)))
          {
            MessageAction msgAction = Task.Run(() => GetPinedMessageAction(pinedMessage.messageId, channelInfo)).Result;
            oldPinedMessageList.Add(new MessageInfo()
            {
              id = pinedMessage.messageId,
              action = msgAction
            });
          }
        }
      }
      return oldPinedMessageList;
    }

    /// <summary>
    /// Определить действие, которое необходимо с закрепленным сообщением.
    /// </summary>
    /// <param name="messageId">ИД запиненного сообщения.</param>
    /// <returns>Действие, которое необходимо с закрепленным сообщением.</returns>
    private static MessageAction GetPinedMessageAction(string messageId, ChannelInfo channelInfo) 
    {
      var messages = mattermostApi.GetThreadMessages(messageId);
      var latest_message = messages.OrderBy(m => m.dateTime).Last();
      return DefineActionByDateAndAuthorOfMessage(latest_message.dateTime, latest_message.userId, channelInfo );
    }

    /// <summary>
    /// Добавить эмодзи на открепляемое сообщение.
    /// </summary>
    /// <param name="messageTimestamp">Отметка времени открепляемого сообщения.</param>
    private static void AddEmoji(string messageID)
    {
      mattermostApi.AddReaction(botUserID, messageID, EmojiName);
    }

    /// <summary>
    /// Определить действие над закрепленным сообщением по дате и автору последнего ответа.
    /// </summary>
    /// <param name="messageDateTime">Отметка времени последнего сообщения из треда.</param>
    /// <param name="userID">ID автора последнего сообщения из треда. </param>
    /// <param name="channelInfo">Информация о канале, в котором нахоидтся тред.</param>
    /// <returns>Действие над закрепленным сообщением.</returns>
    private static MessageAction DefineActionByDateAndAuthorOfMessage(DateTime messageDateTime, string userID, ChannelInfo channelInfo)
    {
      if (messageDateTime != null)      
      {
        if ((userID != botUserID) & (IsOldPinedMessage(messageDateTime, channelInfo.DaysBeforeWarning)))
        {
          return MessageAction.NeedWarning;
        }
        else
        if ((userID == botUserID) & (IsOldPinedMessage(messageDateTime, channelInfo.DaysBeforeUnpining)))
        {
          return MessageAction.NeedUnpin;
        }
      }
      return MessageAction.DoNothing;
    }

    /// <summary>
    /// Определить, является ли закрепленное сообщение старым.
    /// </summary>
    /// <param name="messageDateTime">Отметка времени сообщения.</param>
    /// <param name="DayCount">Период (в днях), по прошествию которого следует считать сообщение старым.</param>
    /// <returns>Признак, является ли закрепленное сообщение старым.</returns>
    private static bool IsOldPinedMessage(DateTime messageDateTime, int DayCount)
    {
      return messageDateTime.AddDays(DayCount) < DateTime.UtcNow; 
    }

    /// <summary>
    /// Отправить сообщение в тред закрепленного сообщения.
    /// </summary>
    /// <param name="textMessage">Текст отправляемого сообщения.</param>
    /// <param name="messageId">ИД закрепленного сообщения.</param>
    /// <param name="channelInfo">Информация о канале, в котором нахоидтся тред.</param>
    private static void SendMessage(string textMessage, string messageId, ChannelInfo channelInfo)
    {
      mattermostApi.PostMessage(channelInfo.ChannelID, textMessage, messageId);
    }
    #endregion
  }

}
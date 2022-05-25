using System;
using System.Threading;
using System.Threading.Tasks;
using MattermostApi;

namespace ApiClient
{
  /// <summary>
  /// Класс информации о сообщении.
  /// </summary>
  public class Message
  {
    /// <summary>
    /// ИД сообщения.
    /// </summary>
    public string messageId;

    /// <summary>
    /// Время и дата создания сообщения.
    /// </summary>
    public DateTime dateTime;

    /// <summary>
    /// ИД автора сообщения.
    /// </summary>
    public string userId;

    ///<summary>
    ///Текст сообщения.
    ///</summary>
    public string message;

    ///<summary>
    ///ID прикрепленных файлов
    ///</summary>
    public string[] fileIDs;
  }

  /// <summary>
  /// Класс события о появлении нового сообщении в канале. 
  /// </summary>
  public class MessageEventInfo
  {
    /// <summary>
    /// ИД сообщения.
    /// </summary>
    public string id;

    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public string message;

    /// <summary>
    /// ИД канала.
    /// </summary>
    public string channelID;

    /// <summary>
    /// ИД автора сообщения.
    /// </summary>
    public string userID;

    /// <summary>
    /// ИД главного сообщения, от которого образован тред.
    /// </summary>
    public string rootID;
  }

  public class UserInfo
  {
    /// <summary>
    /// ИД пользователя.
    /// </summary>
    public string userID;

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string firstName;

    /// <summary>
    /// Фамилия пользователя.
    /// </summary>
    public string lastName;

    /// <summary>
    /// Никнейм пользователя.
    /// </summary>
    public string userName;
  }

  /// <summary>
  /// Интерфейс api-клиента.
  /// </summary>
  public interface IApiClient
  {
    /// <summary>
    /// Отправить сообщение.
    /// </summary>
    /// <param name="channelID">Ид канала.</param>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="rootID">ИД сообщения, от которого образован тред.</param>
    public void PostMessage(string channelID, string message, string rootID = null);

    ///<summary>
    ///Получить данные пользователя по ID.
    ///</summary>
    public UserInfo GetUserInfoByID(string userID);

    /// <summary>
    /// Получить список запиненных сообщений канала.
    /// </summary>
    /// <param name="channelID">ИД канала.</param>
    /// <returns>Список запиненных сообщений канала.</returns>
    public Message[] GetPinnedMessages(string channelID);

    /// <summary>
    /// Добавить эмодзи.
    /// </summary>
    /// <param name="userID">ИД пользователя, от которого ставится эмодзи.</param>
    /// <param name="messageID">ИД сообщения, на который ставится эмодзи.</param>
    /// <param name="emodjiName">Имя эмодзи.</param>
    public void AddReaction(string userID, string messageID, string emodjiName);

    /// <summary>
    /// Отпинить сообщение.
    /// </summary>
    /// <param name="messageID">ИД сообщения.</param>
    public void UnpinMessage(string messageID);

    /// <summary>
    /// Получить список сообщений треда.
    /// </summary>
    /// <param name="messageId">ИД сообщения.</param>
    /// <returns></returns>
    public Message[] GetThreadMessages(string messageId);

    /// <summary>
    /// Запинить сообщение.
    /// </summary>
    /// <param name="messageID">ИД сообщения.</param>
    public void PinMessage(string messageID);

    /// <summary>
    /// Отправить сообщение, видимое только пользователю.
    /// </summary>
    /// <param name="channelID">ИД канала.</param>
    /// <param name="userID">ИД пользователя.</param>
    /// <param name="message">Текст сообщения.</param>
    public void PostEphemeralMessage(string channelID, string userID, string message);

    /// <summary>
    /// Стартовать получение сообщений с сервера.
    /// </summary>
    /// <param name="newPostEventHandler">Обработчик событий поступления новых сообщений.</param>
    /// <param name="errorEventHandler">Обработчик ошибок.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public void StartReceivingServerMessages(Action<MessageEventInfo> newPostEventHandler, Action<string> errorEventHandler, 
      CancellationToken cancellationToken);

    public Task<string> GetFileById(string messageID, string fileID, string pathToFile);
  }

  /// <summary>
  /// Интерфейс построителя api-клиента.
  /// </summary>
  public interface IApiClientBuilder
  {
    /// <summary>
    /// Зарегистрировать обработчик события появления на канале нового сообщения.
    /// </summary>
    /// <param name="newPostEventHandler">Обработчик события.</param>
    /// <returns>Экземпляр <see cref="IApiClientBuilder"/>.</returns>
    public IApiClientBuilder RegisterNewPostEventHandler(Action<MessageEventInfo> newPostEventHandler);

    /// <summary>
    /// Зарегистрировать обработчик ошибок.
    /// </summary>
    /// <param name="errorEventHandler">Обработчик ошибок.</param>
    /// <returns>Экземпляр <see cref="IApiClientBuilder"/>.</returns>
    public IApiClientBuilder RegisterErrorEventHandler(Action<string> errorEventHandler);

    /// <summary>
    /// Подключиться к серверу.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Экземпляр <see cref="IApiClient"/>.</returns>
    public IApiClient Connect(CancellationToken cancellationToken);    

  }

}

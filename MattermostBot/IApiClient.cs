using System;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="eventHandler">Обработчик получаемых с сервера сообщений.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public void StartReceivingServerMessages(Action<MessageEventInfo> eventHandler, CancellationToken cancellationToken);
  }

  /// <summary>
  /// Интерфейс построителя api-клиента.
  /// </summary>
  public interface IApiClientBuilder
  {
    /// <summary>
    /// Зарегистрировать обработчик события появления на канале нового сообщения.
    /// </summary>
    /// <param name="eventHandler">Обработчик события.</param>
    /// <returns>Экземпляр <see cref="IApiClientBuilder"/>.</returns>
    public IApiClientBuilder RegisterNewPostEventHandler(Action<MessageEventInfo> eventHandler);

    /// <summary>
    /// Подключиться к api-клиенту.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Экземпляр <see cref="IApiClient"/>.</returns>
    public IApiClient Connect(CancellationToken cancellationToken);    

  }

}

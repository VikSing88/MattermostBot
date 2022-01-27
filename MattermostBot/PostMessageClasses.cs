namespace SlackBot
{
  /// <summary>
  /// Сообщение для отпинивания сообщения.
  /// </summary>
  internal class RemovePinMessage
  {
    public string channel { get; set; }
    public string timestamp { get; set; }
  }

  /// <summary>
  /// Сообщение для простановки реакции на сообщение.
  /// </summary>
  internal class AddReactionMessage
  {
    public string channel { get; set; }
    public string timestamp { get; set; }
    public string name { get; set; }
  }

  /// <summary>
  /// Класс для отправки сообщения от бота.
  /// </summary>
  internal class SlackMessage
  {
    public string channel { get; set; }
    public string text { get; set; }
    public string thread_ts { get; set; }
  }
}

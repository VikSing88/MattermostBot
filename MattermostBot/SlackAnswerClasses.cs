using System.Collections.Generic;

namespace SlackBot
{
  #region Классы парсинга ответа для команды "pins.list"

  internal class PinedMessage
  {
    public string ts { get; set; }
  }

  internal class PinItem
  {
    public PinedMessage message { get; set; }
  }

  internal class PinsResponse
  {
    public bool ok { get; set; }
    public List<PinItem> items { get; set; }
  }

  #endregion

  #region Классы парсинга ответа для команды "conversations.replies"

  internal class ReplyMessage
  {
    public string ts { get; set; }
    public string user { get; set; }
    public string text { get; set; }
  }

  internal class RepliesResponse
  {
    public bool ok { get; set; }
    public List<ReplyMessage> messages { get; set; }
    public bool has_more { get; set; }
  }

  #endregion
}

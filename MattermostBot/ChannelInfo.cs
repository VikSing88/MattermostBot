namespace MattermostBot
{
  class ChannelInfo
  {
    public ChannelInfo(string channelID, int daysBeforeWarning, int daysBeforeUnpining, 
      bool autoPinNewMessage, string reactionRequest, string welcomeThreadMessage)
    {
      ChannelID = channelID;
      DaysBeforeWarning = daysBeforeWarning;
      DaysBeforeUnpining = daysBeforeUnpining;
      AutoPinNewMessage = autoPinNewMessage;
      ReactionRequest = reactionRequest;
      WelcomeThreadMessage = welcomeThreadMessage;
    }

    /// <summary>
    /// ID канала.
    /// </summary>
    public string ChannelID { get; set; }
    /// <summary>
    /// Количество дней до предупреждения.
    /// </summary>
    public int DaysBeforeWarning { get; set; }
    /// <summary>
    /// Количество дней до открепления (отпинивания) сообщения.
    /// </summary>
    public int DaysBeforeUnpining { get; set; }
    /// <summary>
    /// Разрешение или запрет на автопин
    /// </summary>
    public bool AutoPinNewMessage { get; set; }
    /// <summary>
    /// Реакция для автоматического отпинивания
    /// </summary>
    public string ReactionRequest { get; set; }
    /// <summary>
    /// Приветственное сообщение в треде консультации
    /// </summary>
    public string WelcomeThreadMessage { get; set; }
  }
}

namespace SlackBot
{
  class SlackChannelInfo
  {
    public SlackChannelInfo(string channelID, int daysBeforeWarning, int daysBeforeUnpining, bool autoPinNewMessage, string welcomeMessage)
    {
      ChannelID = channelID;
      DaysBeforeWarning = daysBeforeWarning;
      DaysBeforeUnpining = daysBeforeUnpining;
      AutoPinNewMessage = autoPinNewMessage;
      WelcomeMessage = welcomeMessage;
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
    /// Приветственное сообщение
    /// </summary>
    public string WelcomeMessage { get; set; }
  }
}

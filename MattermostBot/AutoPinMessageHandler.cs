using System.Collections.Generic;
using System.Threading.Tasks;
using SlackNet;
using SlackNet.Events;
using System.Linq;

namespace SlackBot
{
  class AutoPinMessageHandler : IEventHandler<MessageEvent>
  {
    private readonly ISlackApiClient slack;
    private readonly List<SlackChannelInfo> slackChannelsInfo;

    public AutoPinMessageHandler(ISlackApiClient slack, List<SlackChannelInfo> slackChannelsInfo)
    {
      this.slack = slack;
      this.slackChannelsInfo = slackChannelsInfo;
    }

    public Task Handle(MessageEvent newMessageEvent)
    {
      var result = Task.CompletedTask;
      if (IsRootMessage(newMessageEvent) && IsSimpleMessage(newMessageEvent))
      {
        foreach (var channelInfo in slackChannelsInfo.Where(info => info.ChannelID == newMessageEvent.Channel))
        {
          if (!string.IsNullOrEmpty(channelInfo.WelcomeMessage))
            result = result.ContinueWith(
              (t) => slack.Chat.PostEphemeralMessageToUser(channelInfo.WelcomeMessage, newMessageEvent.User, newMessageEvent.Channel));

          if (channelInfo.AutoPinNewMessage)
            result = result.ContinueWith(
              (t) => slack.Pins.AddMessage(newMessageEvent.Channel, newMessageEvent.Ts));

          break;
        }
      }
      return result;
    }

    private bool IsRootMessage(MessageEvent messageEvent) => messageEvent.ThreadTs == null;

    private bool IsSimpleMessage(MessageEvent messageEvent) => messageEvent.GetType() == typeof(MessageEvent);
  }
}


using System;
namespace Service;
public class Helper {
  public static void AddMessage(Terminal context, string message, bool priority = true) {
    context.AddString(message);
    var hud = MessageHud.instance;
    if (!hud) return;
    if (priority) {
      var items = hud.m_msgQeue.ToArray();
      hud.m_msgQeue.Clear();
      Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
      foreach (var item in items)
        hud.m_msgQeue.Enqueue(item);
      hud.m_msgQueueTimer = 10f;
    } else {
      Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, message);
    }
  }
  public static void Command(string name, string description, Terminal.ConsoleEvent action, Terminal.ConsoleOptionsFetcher? fetcher = null) {
    new Terminal.ConsoleCommand(name, description, Helper.Catch(action), optionsFetcher: fetcher);
  }
  public static void AddError(Terminal context, string message, bool priority = true) {
    AddMessage(context, $"Error: {message}", priority);
  }
  public static Terminal.ConsoleEvent Catch(Terminal.ConsoleEvent action) =>
    (args) => {
      try {
        if (!Player.m_localPlayer) throw new InvalidOperationException("Player not found.");
        action(args);
      } catch (InvalidOperationException e) {
        Helper.AddError(args.Context, e.Message);
      }
    };
}
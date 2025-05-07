using System;
using System.Collections.Generic;
using System.Linq;
using Service;
namespace RenderLimits;

public class ForceActiveCommand
{
  public ForceActiveCommand()
  {
    Helper.Command("force_active", "[add/remove/toggle] [around=0] - Toggles the force active of the current zone.", (args) =>
    {
      if (RenderLimits.ConfigSync.IsLocked && !RenderLimits.ConfigSync.IsAdmin)
        throw new InvalidOperationException("Only admins can edit locked configs.");
      var mode = "toggle";
      if (args.Length > 1) mode = args[1];
      var around = 0;
      if (args.Length > 2 && int.TryParse(args[2], out var amount)) around = amount;
      var refZone = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
      for (var i = -around; i <= around; i++)
      {
        for (var j = -around; j <= around; j++)
        {
          Vector2i zone = new(refZone.x + i, refZone.y + j);
          var exists = Configuration.ForceActive.Contains(zone);
          var add = mode == "add" || (mode != "remove" && !exists);
          if (add && exists)
            Helper.AddMessage(args.Context, $"Zone {zone.x},{zone.y} is already in active zones.");
          if (!add && !exists)
            Helper.AddMessage(args.Context, $"Zone {zone.x},{zone.y} is not in active zones.");
          if (!add && exists)
          {
            Helper.AddMessage(args.Context, $"Zone {zone.x},{zone.y} removed from active zones.");
            Configuration.ForceActive.Remove(zone);
          }
          if (add && !exists)
          {
            Helper.AddMessage(args.Context, $"Zone {zone.x},{zone.y} added to active zones.");
            Configuration.ForceActive.Add(zone);
          }
        }
      }
      Configuration.SaveForceActive();
    });
    Helper.Command("check_active", "- Prints currently loaded zones.", (args) =>
    {
      var zones = ZoneSystem.instance.m_zones;
      List<Vector2i> forced = [.. zones.Keys.Where(zone => Configuration.ForceActive.Contains(zone))];
      List<Vector2i> notForced = [.. zones.Keys.Where(zone => !Configuration.ForceActive.Contains(zone))];
      Helper.AddMessage(args.Context, $"Forced: {string.Join(", ", forced.Select(zone => $"{zone.x},{zone.y}"))}");
      Helper.AddMessage(args.Context, $"Default: {string.Join(", ", notForced.Select(zone => $"{zone.x},{zone.y}"))}");
    });
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text;

namespace ChatScanner.Models
{
  public class FocusTab
  {
    public Guid FocusTabId = new Guid();
    public string Name = "";
    public bool Open = true;
    public List<FocusTarget> focusTargets = new List<FocusTarget>();

    public FocusTab(string name, int id)
    {
      this.Name = name;
      if (this.focusTargets.Any(t => t.Name == name))
      {
        this.focusTargets.Add(new FocusTarget()
        {
          Id = id,
          Name = name
        });
      }
    }

    public List<string> GetFocusTargetNames()
    {
      return focusTargets
        .Select(t => t.Name)
        .ToList();
    }

    public void AddFocusTarget(string name, int id)
    {
      if (this.focusTargets.Any(t => t.Name == name))
      {
        this.focusTargets.Add(new FocusTarget()
        {
          Id = id,
          Name = name
        });
      }
    }
  }

  public class FocusTarget
  {
    public int Id;
    public string Name;
  }
}

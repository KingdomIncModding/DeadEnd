using KingdomDeadEnd.Core;
using KingdomFoundation.Api;

namespace KingdomDeadEnd.UI
{
    internal sealed class DeadEndModBarElement : ModBarElement
    {
        private readonly DeadEndPlugin plugin;
        private readonly DeadEndFoundationUi owner;

        public DeadEndModBarElement(DeadEndPlugin plugin, DeadEndFoundationUi owner)
            : base(DeadEndPlugin.PluginId, "deadend_toggle", 142, 20, ModBarAlignment.Right)
        {
            this.plugin = plugin;
            this.owner = owner;
        }

        public override IModBarView CreateView(IModBarContext context)
        {
            var view = new DeadEndModBarView(plugin, owner, context);
            owner.SetActiveBarView(view);
            return view;
        }
    }
}

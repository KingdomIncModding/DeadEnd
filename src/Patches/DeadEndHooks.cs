using System;
using System.Collections.Generic;
using KingdomDeadEnd.Core;
using KingdomDeadEnd.Rendering;
using KingdomFoundation.Api;

namespace KingdomDeadEnd.Patches
{
    internal sealed class DeadEndHooks : IDisposable
    {
        private readonly DeadEndPlugin plugin;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        public DeadEndHooks(DeadEndPlugin plugin)
        {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        }

        public void Install()
        {
            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.PortBound,
                after: OnPortBound));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.BuildingBound,
                after: OnBuildingBound));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.BuildingUpdatePorts,
                after: OnBuildingUpdatePorts));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.PipeBound,
                after: OnPipeBound));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.PipeDragEnd,
                after: OnPipeDragChanged));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.PipeDragCancel,
                after: OnPipeDragChanged));

            subscriptions.Add(FoundationApi.GameHooks.Subscribe(
                DeadEndPlugin.PluginId,
                GameHookPoints.WorkspaceShown,
                after: OnWorkspaceShown));
        }

        public void Dispose()
        {
            for (int i = 0; i < subscriptions.Count; i++)
                subscriptions[i]?.Dispose();

            subscriptions.Clear();
        }

        private void OnPortBound(GameHookCall call)
        {
            if (!call.View)
                return;

            var port = call.Argument<WorkspacePort>(0);
            if (port != null && (port.IsInput || port.IsOutput))
                DeadEndPortOverlay.Ensure(call.View, port);
        }

        private void OnBuildingBound(GameHookCall call)
        {
            AttachBuildingPorts(call.View);
        }

        private void OnBuildingUpdatePorts(GameHookCall call)
        {
            AttachBuildingPorts(call.View);
        }

        private void OnPipeBound(GameHookCall call)
        {
            if (call.View)
            {
                var pipe = FoundationApi.WorkspaceVisuals.PipeFromView(call.View);
                bool refreshedAny = false;

                var inputPort = pipe?.ConnectedInput;
                if (inputPort != null && inputPort.World)
                {
                    var overlay = DeadEndPortOverlay.Ensure(inputPort.World, inputPort);
                    overlay?.RefreshVisuals(force: false);
                    refreshedAny = true;
                }

                var outputPort = pipe?.ConnectedOutput;
                if (outputPort != null && outputPort.World)
                {
                    var overlay = DeadEndPortOverlay.Ensure(outputPort.World, outputPort);
                    overlay?.RefreshVisuals(force: false);
                    refreshedAny = true;
                }

                if (refreshedAny)
                    return;
            }

            plugin.RefreshAllOverlays(force: false);
        }

        private void OnPipeDragChanged(GameHookCall call)
        {
            plugin.RefreshAllOverlays(force: false);
        }

        private void OnWorkspaceShown(GameHookCall call)
        {
            plugin.SweepAndAttachOverlays();
            plugin.RefreshAllOverlays(force: true);
        }

        private static void AttachBuildingPorts(UnityEngine.Component buildingView)
        {
            if (!buildingView)
                return;

            var building = FoundationApi.WorkspaceAccess.FromView(buildingView);
            if (building == null)
                return;

            var inputs = building.Inputs;
            for (int i = 0; i < inputs.Count; i++)
            {
                var port = inputs[i];
                if (port != null && port.World)
                    DeadEndPortOverlay.Ensure(port.World, port);
            }

            var outputs = building.Outputs;
            for (int i = 0; i < outputs.Count; i++)
            {
                var port = outputs[i];
                if (port != null && port.World)
                    DeadEndPortOverlay.Ensure(port.World, port);
            }
        }
    }
}

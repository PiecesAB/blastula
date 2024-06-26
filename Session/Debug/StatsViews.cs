using Blastula.Collision;
using Blastula.Graphics;
using Godot;

namespace Blastula.Debug
{
    /// <summary>
    /// Debug commands used to see certain performance or internal game stats.
    /// </summary>
    public partial class StatsViews : Control
    {
        private static Control currentView = null;
        public static string currentMode = "";
        public static StatsViews main;

        public static DebugConsole.CommandGroup commandGroup = new DebugConsole.CommandGroup
        {
            groupName = "Stats Views",
            commands = new System.Collections.Generic.List<DebugConsole.Command>()
            {
                new DebugConsole.Command
                {
                    name = "sv_off",
                    usageTip = "sv_off",
                    description = "Removes any open stats view.",
                    action = (args) =>
                    {
                        if (currentView != null) { currentView.Visible = false; }
                        currentView = null;
                        currentMode = "";
                        DebugConsole.main.Print("Stats views have been closed.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "sv_timings",
                    usageTip = "sv_timings",
                    description = "Displays the time it takes to process essential functions of the engine.",
                    action = (args) =>
                    {
                        if (currentView != null) { currentView.Visible = false; }
                        currentView = main.FindChild("Timings") as Control;
                        currentView.Visible = true;
                        currentMode = "timings";
                        DebugConsole.main.Print("Timings are now visible.");
                    }
                },

                new DebugConsole.Command
                {
                    name = "sv_counts",
                    usageTip = "sv_counts",
                    description = "",
                    action = (args) =>
                    {
                        if (currentView != null) { currentView.Visible = false; }
                        currentView = main.FindChild("Counts") as Control;
                        currentView.Visible = true;
                        currentMode = "counts";
                        DebugConsole.main.Print("Counts are now visible.");
                    }
                },
            }
        };

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }

        private class Timings
        {
            public double exec;
            public double postExec;
            public double col;
            public double rendB;
            public double rendL;

            public void Interpolate(Timings other, float t)
            {
                exec = Mathf.Lerp(exec, other.exec, t);
                postExec = Mathf.Lerp(postExec, other.postExec, t);
                col = Mathf.Lerp(col, other.col, t);
                rendB = Mathf.Lerp(rendB, other.rendB, t);
                rendL = Mathf.Lerp(rendL, other.rendL, t);
            }
        }

        private class Counts
        {
            public int totalBNodes;
            public int bullets;
            public int laserPieces;
            public int blastodiscs;
            public int mqTail;
            public int mqHead;
        }

        private Timings timings = new Timings();

        public override void _Process(double delta)
        {
            base._Process(delta);
            switch (currentMode)
            {
                case "timings":
                    {
                        Node structure = currentView.FindChild("Structure");
                        Timings newTimings = new Timings
                        {
                            exec = ExecuteManager.debugTimer?.Elapsed.TotalMilliseconds ?? 0,
                            postExec = PostExecuteManager.debugTimer?.Elapsed.TotalMilliseconds ?? 0,
                            col = CollisionManager.debugTimer?.Elapsed.TotalMilliseconds ?? 0,
                            rendB = BulletRendererManager.debugTimer?.Elapsed.TotalMilliseconds ?? 0,
                            rendL = LaserRendererManager.debugTimer?.Elapsed.TotalMilliseconds ?? 0,
                        };
                        timings.Interpolate(newTimings, (float)Mathf.Clamp(delta * 3f, 0, 1));
                        double rend = timings.rendB + timings.rendL;
                        double total = timings.exec + timings.col + timings.postExec + rend;
                        ((Label)structure.FindChild("Total")).Text = $"Total: {total.ToString("F2")} ms";
                        ((Label)structure.FindChild("Execute")).Text = $"Exec Bhvrs: {timings.exec.ToString("F2")} ms";
                        ((Label)structure.FindChild("Collision")).Text = $"Collision: {timings.col.ToString("F2")} ms";
                        ((Label)structure.FindChild("PostExecute")).Text = $"Post-Exec: {timings.postExec.ToString("F2")} ms";
                        ((Label)structure.FindChild("Render")).Text = $"Pre-Render: {rend.ToString("F2")} ms";
                        ((Label)structure.FindChild("BulletRender")).Text = $"    Bullet: {timings.rendB.ToString("F2")} ms";
                        ((Label)structure.FindChild("LaserRender")).Text = $"    Laser: {timings.rendL.ToString("F2")} ms";
                        break;
                    }
                case "counts":
                    {
                        Node structure = currentView.FindChild("Structure");
                        Counts counts = new Counts
                        {
                            totalBNodes = BNodeFunctions.totalInitialized,
                            bullets = BulletRenderer.totalRendered,
                            laserPieces = LaserRenderer.totalRendered,
                            blastodiscs = Blastodisc.all.Count,
                            mqHead = BNodeFunctions.mqHead,
                            mqTail = BNodeFunctions.mqTail
                        };
                        ((Label)structure.FindChild("Total")).Text = $"Total BNodes: {counts.totalBNodes}";
                        ((Label)structure.FindChild("Bullets")).Text = $"Bullets: {counts.bullets}";
                        ((Label)structure.FindChild("LaserPieces")).Text = $"Laser Pieces: {counts.laserPieces}";
                        ((Label)structure.FindChild("Blastodiscs")).Text = $"Blastodiscs: {counts.blastodiscs}";
                        ((Label)structure.FindChild("MQTail")).Text = $"Master queue tail: {counts.mqTail}";
                        ((Label)structure.FindChild("MQHead")).Text = $"Master queue head: {counts.mqHead}";
                        break;
                    }
                default: break;
            }
        }
    }
}


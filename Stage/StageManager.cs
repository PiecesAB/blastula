using Blastula.Graphics;
using Blastula.Schedules;
using Blastula.Sounds;
using Blastula.VirtualVariables;
using Godot;
using System;
using System.Threading.Tasks;

namespace Blastula
{
    /// <summary>
    /// Handles actually starting stages in order. But it's incomplete. At the moment, it just spawns the first
    /// stage that is a child of this node.
    /// </summary>
	public partial class StageManager : Node
	{
        [Signal] public delegate void StageSectorChangedEventHandler(StageSector newSector);
        [Signal] public delegate void StageChangedEventHandler(StageSector newStage);
        [Signal] public delegate void SessionBeginningEventHandler();
        [Signal] public delegate void SessionEndingEventHandler();

        /// <summary>
        /// The number of bullets grazed in this stage. Mainly for single-player use.
        /// </summary>
        public ulong grazeCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong pointItemCount { get; private set; } = 0;
        /// <summary>
        /// Number of point items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong powerItemCount { get; private set; } = 0;
        /// <summary>
        /// Number of cancel items collected in this stage. Mainly for single-player use.
        /// </summary>
        public ulong cancelItemCount { get; private set; } = 0;

        private string currentPlayerPath = "";
        private string currentStageGroupName = "";

        public static StageManager main { get; private set; } = null;

        public void Reset()
        {
            grazeCount = 0;
            pointItemCount = 0;
            powerItemCount = 0;
            cancelItemCount = 0;
        }

        public void AddGraze(int amount)
        {
            grazeCount += (ulong)amount;
        }

        public void AddPointItem(int amount)
        {
            pointItemCount += (ulong)amount;
        }

        public void AddPowerItem(int amount)
        {
            pointItemCount += (ulong)amount;
        }

        public void AddCancelItem(int amount)
        {
            cancelItemCount += (ulong)amount;
        }

        public async Task RetrySinglePlayerSession()
        {
            EndSinglePlayerSession();
            await this.WaitOneFrame();
            await InitializeSinglePlayerSession(currentPlayerPath, currentStageGroupName);
        }

        /// <summary>
        /// Begins a single player game.
        /// </summary>
        public async Task InitializeSinglePlayerSession(string playerPath, string stageGroupName)
        {
            while (Session.main == null || PlayerManager.main == null) { await this.WaitOneFrame(); }
            Session.main.Reset();
            currentPlayerPath = playerPath; 
            currentStageGroupName = stageGroupName;
            await PlayerManager.main.SpawnPlayer(playerPath);
            if (Session.main != null) { Session.main.StartInSession(); }
            if (FrameCounter.main != null) { FrameCounter.main.ResetSessionFrame(); }
            // TODO: not always have the same RNG
            RNG.Reseed(0);
            GD.Seed(0);
            StageSector s = (StageSector)FindChild(stageGroupName);
            s.Preload();
            _ = s.Execute();
            EmitSignal(SignalName.SessionBeginning);
        }

        public void EndSinglePlayerSession()
        {
            EmitSignal(SignalName.SessionEnding);
            if (Session.main != null) { Session.main.EndInSession(); }
            _ = BackgroundHolder.FadeAway(0);
            Waiters.IncrementSceneLoadCounter();
            StageSector.DumpStack();
            foreach (var kvp in Player.playersByControl) { kvp.Value.QueueFree(); }
            Player.playersByControl.Clear();
            foreach (Blastodisc bd in Blastodisc.all) 
            {
                BNodeFunctions.MasterQueuePushTree(bd.masterStructure);
                bd.masterStructure = -1; 
            }
            MusicManager.Stop();
            LabelPool.StopAll();
        }

        public override void _Ready()
        {
            base._Ready();
            main = this;
        }
    }
}

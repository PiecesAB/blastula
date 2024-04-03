using Blastula.Input;
using Blastula.VirtualVariables;
using Godot;

namespace Blastula
{
    /// <summary>
    /// A pause menu that appears when an input named "Pause" occurs.
    /// </summary>
    public partial class DefaultPauseMenu : Control
    {
        public enum State
        {
            Unpaused, Pausing, Paused, Unpausing
        }
        public State state = State.Unpaused;
        public const int PAUSE_OPEN_CLOSE_DELAY = 5;
        private int animationTimer = 0;

        public void UnpausingAnimation()
        {
            int i = 9 - animationTimer;
            if (i > 0)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
            }
            else
            {
                Modulate = new Color(1, 1, 1, 0);
                Visible = false;
                Session.main.Unpause();
                state = State.Unpaused;
            }
            ++animationTimer;
        }

        public void PausingAnimation()
        {
            int i = 1 + animationTimer;
            if (i < 10)
            {
                Modulate = new Color(1, 1, 1, i / (float)(10));
                Visible = true;
            }
            else
            {
                Modulate = new Color(1, 1, 1, 1);
                state = State.Paused;
            }
            ++animationTimer;
        }

        public override void _Ready()
        {
            Visible = false;
            ProcessPriority = Persistent.Priorities.PAUSE;
        }

        public override void _Process(double delta)
        {
            if (Session.main == null) { return; }
            bool pausePressed = InputManager.ButtonPressedThisFrame("Pause");
            if (pausePressed)
            {
                if (state == State.Paused && Session.main.paused)
                {
                    state = State.Unpausing;
                    animationTimer = 0;
                    UnpausingAnimation();
                }
                else if (state == State.Unpaused && !Session.main.paused)
                {
                    state = State.Pausing;
                    animationTimer = 0;
                    Session.main.Pause();
                    PausingAnimation();
                }
            }

            // No async used because that will easily cause the game state to change when it's paused.
            // We want to ensure all pauses/unpauses are solved after all game action is processed.
            if (state == State.Unpausing) { UnpausingAnimation(); }
            else if (state == State.Pausing) { PausingAnimation(); }
        }
    }
}

using Blastula.VirtualVariables;
using Godot;
using System;

namespace Blastula
{
    /// <summary>
    /// A class that enemies use to move around. These are meant to be created by the movement schedule of an enemy.
    /// </summary>
    /// <remarks>
    /// The reason this is separate from the Enemy is to increase the possible complexity of enemy movement.
    /// Also, any position interpolation will take precedence over all movement from all other EnemyMovers.
    /// </remarks>
    /// <example>
    /// If we want an Enemy to move in a circle while also moving downward, two EnemyMovers together can create that complex movement.
    /// The circular movement can be created by updating velocity in a cyclic movement schedule.
    /// </example>
    public partial class EnemyMover : Node
    {
        [Export] public Tween.TransitionType easingTransition = Tween.TransitionType.Quad;
        [Export] public Tween.EaseType easingType = Tween.EaseType.InOut;
        [Export] public Vector2 startVelocity = Vector2.Zero;
        [Export] public float tweenDuration = 0.5f;
        [Export] public bool radialVelocityInterpolation = false;

        public Enemy enemy;
        private bool velocityIsRadial = false;
        private Vector2 velocity = Vector2.Zero;
        private Tween velTween = null;
        private Tween posTween = null;

        public static Vector2 RadialToCartesian(Vector2 v)
        {
            return v.X * Vector2.FromAngle(v.Y);
        }

        public static Vector2 CartesianToRadial(Vector2 v)
        {
            return new Vector2(v.Length(), v.Angle());
        }

        public Vector2 GetVelocity()
        {
            if (velocityIsRadial) { return RadialToCartesian(velocity); }
            else { return velocity; }
        }

        public void SetTargetVelocity(Vector2 newVelocity)
        {
            if (velTween != null && velTween.IsRunning()) { velTween.Kill(); }
            if (radialVelocityInterpolation && !velocityIsRadial)
            {
                velocity = CartesianToRadial(velocity);
                velocityIsRadial = true;
            }
            else if (!radialVelocityInterpolation && velocityIsRadial)
            {
                velocity = RadialToCartesian(velocity);
                velocityIsRadial = false;
            }
            if (tweenDuration <= 0f)
            {
                velocity = newVelocity;
                return;
            }
            velTween = CreateTween();
            velTween.Pause();
            velTween.SetTrans(easingTransition).SetEase(easingType);
            velTween.TweenProperty(this, "velocity", newVelocity, tweenDuration);
        }

        public void SetTargetPosition(Vector2 newPosition)
        {
            if (posTween != null && posTween.IsRunning()) { posTween.Kill(); }
            if (tweenDuration <= 0f)
            {
                enemy.GlobalPosition = newPosition;
                return;
            }
            posTween = enemy.CreateTween();
            posTween.Pause();
            posTween.SetTrans(easingTransition).SetEase(easingType);
            posTween.TweenProperty(enemy, "global_position", newPosition, tweenDuration);
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (enemy == null) { return; }
            if (Session.IsPaused() || Debug.GameFlow.frozen) { return; }

            if (posTween != null)
            {
                bool incomplete = posTween.CustomStep(Engine.TimeScale / Persistent.SIMULATED_FPS);
                if (!incomplete) { posTween = null; }
            }

            if (velTween != null)
            {
                bool incomplete = velTween.CustomStep(Engine.TimeScale / Persistent.SIMULATED_FPS);
                if (!incomplete) { velTween = null; }
            }

            if (velocityIsRadial)
            {
                enemy.GlobalPosition += (float)Engine.TimeScale * RadialToCartesian(velocity) / Persistent.SIMULATED_FPS;
            }
            else
            {
                enemy.GlobalPosition += (float)Engine.TimeScale * velocity / Persistent.SIMULATED_FPS;
            }
        }
    }
}


using System;
using static BulletSharp.UnsafeNativeMethods;

namespace BulletSharp
{
	public class MultiBodyJointMotor : MultiBodyConstraint
	{
		public MultiBodyJointMotor(MultiBody body, int link, float desiredVelocity,
			float maxMotorImpulse)
		{
			IntPtr native = btMultiBodyJointMotor_new(body.Native, link, desiredVelocity,
				maxMotorImpulse);
			InitializeUserOwned(native);
			InitializeMembers(body, body);
		}

		public MultiBodyJointMotor(MultiBody body, int link, int linkDoF, float desiredVelocity,
			float maxMotorImpulse)
		{
			IntPtr native = btMultiBodyJointMotor_new2(body.Native, link, linkDoF, desiredVelocity,
				maxMotorImpulse);
			InitializeUserOwned(native);
			InitializeMembers(body, body);
		}

		public void SetVelocityTarget(float velTarget)
		{
			btMultiBodyJointMotor_setVelocityTarget(Native, velTarget);
		}
        public void SetVelocityTarget(float velTarget, float kd)
        {
            btMultiBodyJointMotor_setVelocityTarget2(Native, velTarget, kd);
        }

        public void SetPositionTarget(float velTarget)
        {
            btMultiBodyJointMotor_setPositionTarget(Native, velTarget);
        }

        public void SetPositionTarget(float velTarget, float kp)
        {
            btMultiBodyJointMotor_setPositionTarget2(Native, velTarget, kp);
        }
    }
}
